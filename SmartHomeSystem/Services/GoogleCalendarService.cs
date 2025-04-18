using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using SmartHomeSystem.Data;
using SmartHomeSystem.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace SmartHomeSystem.Services
{

    public class GoogleCalendarService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<GoogleCalendarService> _logger;


        public GoogleCalendarService(IHttpClientFactory factory, AppDbContext db, IConfiguration config, ILogger<GoogleCalendarService> logger)
        {
            _httpClientFactory = factory;
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task<string> GetValidAccessTokenAsync(UserToken token)
        {

            if (token.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
                return token.AccessToken;

            var client = _httpClientFactory.CreateClient();
            var dict = new Dictionary<string, string>
            {
                ["client_id"] = _config["Authentication:Google:ClientId"],
                ["client_secret"] = _config["Authentication:Google:ClientSecret"],
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = token.RefreshToken
            };

            var response = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(dict));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonDocument.Parse(json).RootElement;

            token.AccessToken = obj.GetProperty("access_token").GetString()!;
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(obj.GetProperty("expires_in").GetInt32());
            await _db.SaveChangesAsync();

            return token.AccessToken;
        }

        public async Task<List<GoogleCalendarEvent>> GetUpcomingEventsAsync(string userEmail)
        {
            var token = await _db.UserTokens.FirstOrDefaultAsync(t => t.UserEmail == userEmail); // Or filter by user if needed
            if (token == null)
                throw new UnauthorizedAccessException("No access token available");
            
            var accessToken = await GetValidAccessTokenAsync(token);
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var now = DateTime.UtcNow.ToString("o");
            var response = await client.GetAsync($"https://www.googleapis.com/calendar/v3/calendars/primary/events?timeMin={now}&singleEvents=true&orderBy=startTime&maxResults=10");
            response.EnsureSuccessStatusCode();

            var events = new List<GoogleCalendarEvent>();
            var json = await response.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;

            if (root.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var summary = item.GetProperty("summary").GetString();
                    var description = item.TryGetProperty("description", out var desc) ? desc.GetString() : null;
                    var location = item.TryGetProperty("location", out var loc) ? loc.GetString() : null;
                    var status = item.TryGetProperty("status", out var stat) ? stat.GetString() : "confirmed";
                    var htmlLink = item.TryGetProperty("htmlLink", out var link) ? link.GetString() : null;
                    
                    // Handle start time
                    var startObj = item.GetProperty("start");
                    var isAllDay = startObj.TryGetProperty("date", out var startDate);
                    var start = isAllDay 
                        ? DateTime.Parse(startDate.GetString()) 
                        : DateTime.Parse(startObj.GetProperty("dateTime").GetString());
                    var timeZone = startObj.TryGetProperty("timeZone", out var tz) ? tz.GetString() : "UTC";

                    // Handle end time
                    var endObj = item.GetProperty("end");
                    var end = isAllDay 
                        ? DateTime.Parse(endObj.GetProperty("date").GetString()) 
                        : DateTime.Parse(endObj.GetProperty("dateTime").GetString());

                    // Handle created/updated times
                    var created = DateTime.Parse(item.GetProperty("created").GetString());
                    var updated = DateTime.Parse(item.GetProperty("updated").GetString());

                    events.Add(new GoogleCalendarEvent
                    {
                        Title = summary,
                        Description = description,
                        Location = location,
                        Start = start,
                        End = end,
                        Status = status,
                        Created = created,
                        Updated = updated,
                        HtmlLink = htmlLink,
                        IsAllDay = isAllDay,
                        TimeZone = timeZone
                    });
                }
            }

            return events;
        }

        public string FormatEventsForSpeech(List<GoogleCalendarEvent> events)
        {
            if (!events.Any())
                return "You have no upcoming events in your calendar.";

            var sb = new StringBuilder("Here are your upcoming events: ");
            foreach (var e in events)
            {
                sb.Append($"{e.Title}");
                
                if (!string.IsNullOrEmpty(e.Location))
                    sb.Append($" at {e.Location}");

                if (e.IsAllDay)
                {
                    sb.Append($" all day on {e.Start.ToLocalTime():dddd, MMMM d}");
                }
                else
                {
                    sb.Append($" at {e.Start.ToLocalTime():h:mm tt} on {e.Start.ToLocalTime():dddd, MMMM d}");
                }

                if (e.Status != "confirmed")
                    sb.Append($" ({e.Status})");

                sb.Append(". ");
            }

            return sb.ToString();
        }
    }

}

