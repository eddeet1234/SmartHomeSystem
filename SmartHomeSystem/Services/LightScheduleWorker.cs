using Microsoft.Extensions.Hosting;
using SmartHomeSystem.Data;
using SmartHomeSystem.Services;
using Microsoft.EntityFrameworkCore;

public class LightScheduleWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LightScheduleWorker> _logger;

    public LightScheduleWorker(IServiceProvider serviceProvider, ILogger<LightScheduleWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var espService = scope.ServiceProvider.GetRequiredService<EspLightService>();

                var now = DateTime.Now.TimeOfDay;
                var windowStart = now.Add(TimeSpan.FromSeconds(-30));
                var windowEnd = now.Add(TimeSpan.FromSeconds(30));

                // Turn ON lights
                var toTurnOn = await context.LightSchedules
                    .Where(s => s.OnTime > windowStart && s.OnTime <= windowEnd)
                    .ToListAsync();

                foreach (var schedule in toTurnOn)
                {
                    try
                    {
                        _logger.LogInformation($"Turning ON light for schedule {schedule.Id}...");
                        await espService.TurnOnAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to turn ON light for schedule {schedule.Id}");
                    }
                }

                // Turn OFF lights
                var toTurnOff = await context.LightSchedules
                    .Where(s => s.OffTime != null && s.OffTime > windowStart && s.OffTime <= windowEnd)
                    .ToListAsync();

                foreach (var schedule in toTurnOff)
                {
                    try
                    {
                        _logger.LogInformation($"Turning OFF light for schedule {schedule.Id}...");
                        await espService.TurnOffAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to turn OFF light for schedule {schedule.Id}");
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
