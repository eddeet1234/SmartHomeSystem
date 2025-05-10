using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SmartHomeSystem.Services
{
    public class TemperatureWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TemperatureWorker> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

        public TemperatureWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<TemperatureWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var temperatureService = scope.ServiceProvider.GetRequiredService<TemperatureService>();
                        await temperatureService.FetchAndStoreTemperatureAsync();
                    }
                    _logger.LogInformation("Temperature data fetched and stored successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching temperature data");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
} 