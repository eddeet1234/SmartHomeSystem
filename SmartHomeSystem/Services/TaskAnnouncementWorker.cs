using System.Diagnostics;
using SmartHomeSystem.Services;
public class TaskAnnouncementWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskAnnouncementWorker> _logger;
    private readonly HomeStateService _homeState;
    private readonly TextToSpeechService _tts;
    private Process? _activeAudioProcess;
    private readonly IWebHostEnvironment _env;

    public TaskAnnouncementWorker(
        IServiceProvider serviceProvider,
        ILogger<TaskAnnouncementWorker> logger,
        HomeStateService homeState,
        TextToSpeechService tts, IWebHostEnvironment env)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _homeState = homeState;
        _tts = tts;
        _env = env;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_homeState.IsHome && !string.IsNullOrEmpty(_homeState.UserEmail))
            //if (_homeState.IsHome)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var googleTasksService = scope.ServiceProvider.GetRequiredService<GoogleTasksService>();

                        // Get and format tasks with the user's email
                        var tasksByList = await googleTasksService.GetAllTasksAsync(_homeState.UserEmail);
                        var message = googleTasksService.FormatTasksForSpeech(tasksByList);

                        // Convert to speech and get audio file path
                        var audioPath = await _tts.SynthesizeSpeechAsync(message);

                        // Play the audio using mpg123 (needs to be installed on the Raspberry Pi)
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", audioPath.TrimStart('/'));

                        try
                        {
                            if (_env.IsDevelopment())
                            {
                                _logger.LogInformation($"Playing audio file: {fullPath}");
                                var psi = new ProcessStartInfo
                                {
                                    FileName = @"C:\Program Files\VideoLAN\VLC\vlc.exe", // Adjust if using 32-bit VLC
                                    Arguments = $"\"{fullPath}\" --intf dummy --play-and-exit",
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true
                                };

                                _logger.LogInformation("Playing MP3 with VLC: " + fullPath);
                                var process = Process.Start(psi);
                                if (process != null)
                                {
                                    await process.WaitForExitAsync();
                                }


                            }
                            else
                            {
                                _logger.LogInformation($"Playing audio file: {fullPath}");
                                // 🐧 Raspberry Pi PROD - use mpg123
                                var psi = new ProcessStartInfo
                                {
                                    FileName = "mpg123",
                                    ArgumentList = { fullPath },
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };



                                _activeAudioProcess = Process.Start(psi);
                                if (_activeAudioProcess != null)
                                {
                                    await _activeAudioProcess.WaitForExitAsync(stoppingToken);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to play audio file");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in task announcement worker");
                }
            }
            else
            {
                _logger.LogInformation(!_homeState.IsHome ? 
                    "Not home, skipping task announcements..." : 
                    "No user email available, skipping task announcements...");
            }

            // Calculate delay until the next hour
            var now = DateTime.Now;
            var nextHour = now.AddHours(1).Date.AddHours(now.AddHours(1).Hour);
            var delay = nextHour - now;
            
            // Wait until the next hour
            await Task.Delay(delay, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_activeAudioProcess != null && !_activeAudioProcess.HasExited)
        {
            _activeAudioProcess.Kill(true);
            _activeAudioProcess.Dispose();
            _activeAudioProcess = null;
        }

        await base.StopAsync(cancellationToken);
    }
}