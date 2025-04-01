using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartHomeSystem.Data;
using SmartHomeSystem.Data.Model;

namespace SmartHomeSystem.Services
{
    public class EspLightService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly string _baseUrl = "http://10.0.0.103";

        public EspLightService(HttpClient httpClient, AppDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        // Create
        public async Task AddScheduleAsync(LightSchedule schedule)
        {
            _context.LightSchedules.Add(schedule);
            await _context.SaveChangesAsync();
        }

        // Read
        public async Task<List<LightSchedule>> GetAllSchedulesAsync()
        {
            return await _context.LightSchedules
                .OrderBy(s => s.OnTime)
                .ToListAsync();
        }

        public async Task<LightSchedule> GetScheduleAsync(int id)
        {
            return await _context.LightSchedules.FindAsync(id);
        }

        // Update
        public async Task UpdateScheduleAsync(LightSchedule schedule)
        {
            var existing = await _context.LightSchedules.FindAsync(schedule.Id);
            if (existing != null)
            {
                existing.OnTime = schedule.OnTime;
                existing.OffTime = schedule.OffTime;
                await _context.SaveChangesAsync();
            }
        }

        // Delete
        public async Task DeleteScheduleAsync(int id)
        {
            var schedule = await _context.LightSchedules.FindAsync(id);
            if (schedule != null)
            {
                _context.LightSchedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> TurnOnAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{_baseUrl}/on");
                return JsonSerializer.Deserialize<JsonElement>(response).GetProperty("state").GetString();
            }
            catch (HttpRequestException)
            {
                return "Error: ESP32 is offline";
            }
            catch (JsonException)
            {
                return "Error: Invalid response from ESP32";
            }
        }

        public async Task<string> TurnOffAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{_baseUrl}/off");
                return JsonSerializer.Deserialize<JsonElement>(response).GetProperty("state").GetString();
            }
            catch (HttpRequestException)
            {
                return "Error: ESP32 is offline";
            }
            catch (JsonException)
            {
                return "Error: Invalid response from ESP32";
            }
        }
    }
}
