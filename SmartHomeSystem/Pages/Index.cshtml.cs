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
    public string AudioUrl { get; private set; }

    public IndexModel(EspLightService lightService, AppDbContext context, CeilingLightService ceilingLightService, AlarmService alarmService, TextToSpeechService tts)
    {
        _lightService = lightService;
        _context = context;
        _ceilingLightService = ceilingLightService;
        _alarmService = alarmService;
        _tts = tts;

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
        if (User.Identity?.IsAuthenticated == true)
        {
            UserEmail = User.Identity.Name ?? "Signed in";

            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");

                if (string.IsNullOrEmpty(accessToken))
                {
                    TasksErrorMessage = "You are not authenticated. Please sign in to access your tasks.";
                    return;
                }

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.GetAsync("https://tasks.googleapis.com/tasks/v1/users/@me/lists");

                if (!response.IsSuccessStatusCode)
                {
                    TasksErrorMessage = $"Unable to fetch task lists. Status: {response.StatusCode}";
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var items = doc.RootElement.GetProperty("items");

                foreach (var item in items.EnumerateArray())
                {
                    var listTitle = item.GetProperty("title").GetString();
                    var listId = item.GetProperty("id").GetString();

                    //var taskUrl = $"https://tasks.googleapis.com/tasks/v1/lists/{listId}/tasks?showCompleted=true&showDeleted=false&showHidden=true";
                    var taskUrl = $"https://tasks.googleapis.com/tasks/v1/lists/{listId}/tasks";
                    var tasksResponse = await httpClient.GetAsync(taskUrl);

                    if (tasksResponse.IsSuccessStatusCode)
                    {
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
                        TasksByList[listTitle] = tasks;
                    }
                }
            }
            catch (Exception ex)
            {
                TasksErrorMessage = "An unexpected error occurred while fetching tasks.";
            }

        }
        string message = "Ohh wow Mr Egbert you dance so good, much better than that Stephan fellow!";
        AudioUrl = await _tts.SynthesizeSpeechAsync(message);
    }

    public IActionResult OnPostStopAlarm()
    {
        if (_alarmService.GetAlarmPlayDuration().TotalSeconds >= 30)
        {
            _alarmService.StopAlarm();
            return RedirectToPage();
        }

        // If alarm hasn't played for 30s yet, don't stop it
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

    public JsonResult OnGetAlarmDuration()
    {
        var duration = _alarmService.GetAlarmPlayDuration();
        return new JsonResult(Math.Round(duration.TotalSeconds));
    }

    public IActionResult OnGetSignin()
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, GoogleDefaults.AuthenticationScheme);
    }

    public IActionResult OnGetSignOut()
    {
        return SignOut(new AuthenticationProperties
        {
            RedirectUri = "/"
        },
        CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
