using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using SmartHomeSystem.Data;
using SmartHomeSystem.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace SmartHomeSystem.Services
{
    public class TemperatureService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public TemperatureService(HttpClient httpClient, AppDbContext dbContext, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task FetchAndStoreTemperatureAsync()
        {
            try
            {
                var esp32Url = _configuration["ESP32:TemperatureUrl"];
                if (string.IsNullOrEmpty(esp32Url))
                {
                    throw new InvalidOperationException("ESP32 temperature URL not configured");
                }

                var response = await _httpClient.GetStringAsync(esp32Url);
                var temperatureData = JsonSerializer.Deserialize<TemperatureResponse>(response);
                //var temperatureData = new TemperatureResponse { Temperature = 25.0 }; // test value
                if (temperatureData != null)
                {
                    var temperature = new Temperature
                    {
                        Value = temperatureData.temperature,
                        Timestamp = DateTime.UtcNow
                    };

                    _dbContext.Temperatures.Add(temperature);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error fetching temperature: {ex.Message}");
            }
        }

        public async Task<Temperature?> GetLatestTemperatureAsync()
        {
            return await _dbContext.Temperatures
                .OrderByDescending(t => t.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<IQueryable<Temperature>> GetTemperatureHistoryAsync(int hours = 24)
        {
            return _dbContext.Temperatures
                .Where(t => t.Timestamp >= DateTime.UtcNow.AddHours(-hours))
                .OrderBy(t => t.Timestamp);
        }

        private class TemperatureResponse
        {
            public double temperature { get; set; }
        }
    }
} 