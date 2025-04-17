using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using SmartHomeSystem.Services;

public class TaskAnnouncementWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskAnnouncementWorker> _logger;
    private readonly HomeStateService _homeState;
    private readonly TextToSpeechService _tts;
    private Process? _activeAudioProcess;

    public TaskAnnouncementWorker(
        IServiceProvider serviceProvider,
        ILogger<TaskAnnouncementWorker> logger,
        HomeStateService homeState,
        TextToSpeechService tts)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _homeState = homeState;
        _tts = tts;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_homeState.IsHome)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var googleTasksService = scope.ServiceProvider.GetRequiredService<GoogleTasksService>();
                        
                        // Get and format tasks
                        var tasksByList = await googleTasksService.GetAllTasksAsync();
                        var message = googleTasksService.FormatTasksForSpeech(tasksByList);
                        
                        // Convert to speech and get audio file path
                        var audioPath = await _tts.SynthesizeSpeechAsync(message);
                        
                        // Play the audio using mpg123 (needs to be installed on the Raspberry Pi)
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", audioPath.TrimStart('/'));
                        
                        try
                        {
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
                _logger.LogInformation("Not home, skipping task announcements...");
            }

            // Wait for 1 hour before next announcement
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
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