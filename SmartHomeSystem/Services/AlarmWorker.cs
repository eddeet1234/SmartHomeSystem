using Microsoft.Extensions.Hosting;
using SmartHomeSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

public class AlarmWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlarmWorker> _logger;
    public static Process? ActiveAlarmProcess { get; private set; }

    public AlarmWorker(IServiceProvider serviceProvider, ILogger<AlarmWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            var dueAlarms = await context.Alarms
                .Where(a => a.Time <= now)
                .ToListAsync();

            foreach (var alarm in dueAlarms)
            {
                _logger.LogInformation($"Alarm triggered: {alarm.Label}");

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "mpg123",
                        ArgumentList = { "/home/eddeet2001/alarm.mp3" },
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    ActiveAlarmProcess = Process.Start(psi);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to play alarm sound");
                }

                if (alarm.RepeatDaily)
                {
                    alarm.Time = alarm.Time.AddDays(1);
                }
                else
                {
                    context.Alarms.Remove(alarm);
                }
            }

            await context.SaveChangesAsync();
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    public static void StopAlarm()
    {
        if (ActiveAlarmProcess != null && !ActiveAlarmProcess.HasExited)
        {
            ActiveAlarmProcess.Kill(true);
            ActiveAlarmProcess.Dispose();
            ActiveAlarmProcess = null;
        }
    }
}
