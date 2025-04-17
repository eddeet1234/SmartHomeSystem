using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartHomeSystem.Services;
using SmartHomeSystem.Data;
using SmartHomeSystem.Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;

public class IndexModel : PageModel
{
    private readonly EspLightService _lightService;
    private readonly AppDbContext _context;
    private readonly CeilingLightService _ceilingLightService;
    private readonly AlarmService _alarmService;
    private readonly TextToSpeechService _tts;
    private readonly GoogleTasksService _googleTasksService;
    private readonly HomeStateService _homeState;
    public string AudioUrl { get; private set; }

    public IndexModel(EspLightService lightService, AppDbContext context, CeilingLightService ceilingLightService, AlarmService alarmService, TextToSpeechService tts, GoogleTasksService googleTasksService, HomeStateService homeState)
    {
        _lightService = lightService;
        _context = context;
        _ceilingLightService = ceilingLightService;
        _alarmService = alarmService;
        _tts = tts;
        _googleTasksService = googleTasksService;
        _homeState = homeState;
        // Set default times to current time
        OnTime = DateTime.Now.TimeOfDay;
        OffTime = DateTime.Now.TimeOfDay;
    }

    [BindProperty]
    public TimeSpan OnTime { get; set; }

    [BindProperty]
    public TimeSpan? OffTime { get; set; }

    [BindProperty]
    public DateTime NewAlarmTime { get; set; }

    [BindProperty]
    public string NewAlarmLabel { get; set; }

    [BindProperty]
    public bool NewAlarmRepeat { get; set; }

    public bool IsHome
    {
        get => _homeState.IsHome;
        set => _homeState.IsHome = value;
    }

    public List<Alarm> Alarms { get; set; }

    public List<LightSchedule> LightSchedules { get; set; }

    public string CurrentState { get; set; }
    public string UserEmail { get; set; } = "Not signed in";
    public Dictionary<string, List<GoogleTask>> TasksByList { get; set; } = new();
    public string TasksErrorMessage { get; set; }

    public async Task<IActionResult> OnPostTurnOnAsync()
    {
        CurrentState = await _lightService.TurnOnAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTurnOffAsync()
    {
        CurrentState = await _lightService.TurnOffAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostScheduleAsync()
    {
        var schedule = new LightSchedule
        {
            OnTime = OnTime,
            OffTime = OffTime
        };
        await _lightService.AddScheduleAsync(schedule);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteScheduleAsync(int id)
    {
        await _lightService.DeleteScheduleAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleCeilingLightAsync()
    {
        await _ceilingLightService.ToggleAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddAlarmAsync()
    {
        var alarm = new Alarm
        {
            Time = DateTime.SpecifyKind(NewAlarmTime, DateTimeKind.Local).ToUniversalTime(),
            Label = NewAlarmLabel,
            RepeatDaily = NewAlarmRepeat
        };

        _alarmService.AddAlarm(alarm);
        return RedirectToPage();
    }

    public async Task OnGetAsync()
    {
        // Get alarms
        Alarms = _alarmService.GetAllAlarms()
            .Where(a => a.Time > DateTime.UtcNow)
            .OrderBy(a => a.Time)
            .ToList();

        // Get light schedules
        LightSchedules = await _lightService.GetAllSchedulesAsync();

        string message = "";

        if (User.Identity?.IsAuthenticated == true)
        {
            UserEmail = User.Identity.Name ?? "Signed in";
              _homeState.UserEmail = UserEmail;  // Add this line to store the email
            try
            {
                // 🔐 Save tokens
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
                var expiresAtStr = await HttpContext.GetTokenAsync("expires_at");

                if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(expiresAtStr))
                {
                    var expiresAt = DateTime.Parse(
                        expiresAtStr,
                        null,
                        System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal
                    );
                    var existing = await _context.UserTokens.FirstOrDefaultAsync(t => t.UserEmail == UserEmail);

                    if (existing != null)
                    {
                        existing.AccessToken = accessToken!;
                        existing.ExpiresAt = expiresAt;
                        if (!string.IsNullOrEmpty(refreshToken))
                        {
                            existing.RefreshToken = refreshToken!;
                        }
                    }
                    else if (!string.IsNullOrEmpty(refreshToken)) // Refresh token is only sent once
                    {
                        _context.UserTokens.Add(new UserToken
                        {
                            UserEmail = UserEmail,
                            AccessToken = accessToken!,
                            RefreshToken = refreshToken!,
                            ExpiresAt = expiresAt
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                //Get all the user's tasks and format them for TTS
                TasksByList = await _googleTasksService.GetAllTasksAsync(_homeState.UserEmail);
                message = _googleTasksService.FormatTasksForSpeech(TasksByList);
            }
            catch (UnauthorizedAccessException)
            {
                TasksErrorMessage = "You are not authenticated. Please sign in to access your tasks.";
            }
            catch (Exception ex)
            {
                TasksErrorMessage = "An unexpected error occurred while fetching tasks.";
            }
        }

        //Synthesize Speech
        if (!string.IsNullOrEmpty(message))
        {
            AudioUrl = await _tts.SynthesizeSpeechAsync(message);
        }
    }

    public IActionResult OnPostStopAlarm()
    {
        //Only allow user to stop alarm after 30s
        if (_alarmService.GetAlarmPlayDuration().TotalSeconds >= 30)
        {
            _alarmService.StopAlarm();
            return RedirectToPage();
        }

        return RedirectToPage();
    }

    public JsonResult OnGetAlarmStatus()
    {
        bool isPlaying = _alarmService.IsAlarmPlaying();
        return new JsonResult(isPlaying);
    }

    public IActionResult OnPostDeleteAlarm(int id)
    {
        _alarmService.DeleteAlarm(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostThirtyMinTimerAsync()
    {
        try
        {
            // Turn the light on
            await _lightService.TurnOnAsync();

            // Schedule turn off after 30 minutes
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(30));
                await _lightService.TurnOffAsync();
            });

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            return RedirectToPage();
        }
    }

    //function to get the duration of the alarm playing 
    public JsonResult OnGetAlarmDuration()
    {
        var duration = _alarmService.GetAlarmPlayDuration();
        return new JsonResult(Math.Round(duration.TotalSeconds));
    }

    //function to sign in with google
    public IActionResult OnGetSignin()
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, GoogleDefaults.AuthenticationScheme);
    }

    //function to sign out
    public IActionResult OnGetSignOut()
    {
        return SignOut(new AuthenticationProperties
        {
            RedirectUri = "/"
        },
        CookieAuthenticationDefaults.AuthenticationScheme);
    }

    //function to toggle the status
    public IActionResult OnPostToggleStatus()
    {
        _homeState.IsHome = !_homeState.IsHome;
        return RedirectToPage();
    }
}
