using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SmartHomeSystem.Data;
using SmartHomeSystem.Data.Model;

namespace SmartHomeSystem.Services
{
    public class GoogleTasksService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public GoogleTasksService(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
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

            var newAccessToken = obj.GetProperty("access_token").GetString()!;
            var expiresIn = obj.GetProperty("expires_in").GetInt32();

            token.AccessToken = newAccessToken;
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            await _db.SaveChangesAsync();

            return newAccessToken;
        }


        public async Task<Dictionary<string, List<GoogleTask>>> GetAllTasksAsync(string userEmail)
        {
            var token = await _db.UserTokens.FirstOrDefaultAsync(t => t.UserEmail == userEmail); // Or filter by user if needed
            if (token == null)
                throw new UnauthorizedAccessException("No access token available");

            var accessToken = await GetValidAccessTokenAsync(token);

            var tasksByList = new Dictionary<string, List<GoogleTask>>();
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Get task lists
            var response = await client.GetAsync("https://tasks.googleapis.com/tasks/v1/users/@me/lists");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                var listTitle = item.GetProperty("title").GetString();
                var listId = item.GetProperty("id").GetString();

                var taskUrl = $"https://tasks.googleapis.com/tasks/v1/lists/{listId}/tasks";
                var tasksResponse = await client.GetAsync(taskUrl);
                tasksResponse.EnsureSuccessStatusCode();

                var tasksJson = await tasksResponse.Content.ReadAsStringAsync();
                using var tasksDoc = JsonDocument.Parse(tasksJson);

                var tasks = new List<GoogleTask>();
                if (tasksDoc.RootElement.TryGetProperty("items", out var taskItems))
                {
                    foreach (var taskItem in taskItems.EnumerateArray())
                    {
                        var task = new GoogleTask
                        {
                            Title = taskItem.GetProperty("title").GetString(),
                            Status = taskItem.GetProperty("status").GetString()
                        };

                        if (taskItem.TryGetProperty("notes", out var notes))
                            task.Notes = notes.GetString();

                        if (taskItem.TryGetProperty("due", out var due))
                            task.Due = DateTime.Parse(due.GetString());

                        tasks.Add(task);
                    }
                }
                tasksByList[listTitle] = tasks;
            }

            return tasksByList;
        }


        public string FormatTasksForSpeech(Dictionary<string, List<GoogleTask>> tasksByList)
        {
            if (!tasksByList.Any())
            {
                return "You have no tasks in your lists.";
            }

            var message = "Here are your tasks: ";
            foreach (var taskList in tasksByList)
            {
                if (taskList.Value.Any())
                {
                    message += $"In {taskList.Key}: ";
                    foreach (var task in taskList.Value)
                    {
                        message += $"{task.Title}. ";
                        if (!string.IsNullOrEmpty(task.Notes))
                        {
                            message += $"Notes: {task.Notes}. ";
                        }
                        if (task.Due.HasValue)
                        {
                            if (task.Due.HasValue)
                            {
                                message += $"Due {task.Due.Value.ToString("MMMM dd")}";
                            }
                        }
                    }
                }
                else
                {
                    message += "Jokes... There are no tasks Mr. Egbert!";
                }
            }

            return message;
        }
    }
}