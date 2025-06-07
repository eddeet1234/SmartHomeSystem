using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartHomeSystem.Services;
using SmartHomeSystem.Data;
using SmartHomeSystem.Data.Model;

public class IndexModel : PageModel
{
    private readonly EspLightService _lightService;
    private readonly AppDbContext _context;
    private readonly CeilingLightService _ceilingLightService;
    private readonly AlarmService _alarmService;
    private readonly HomeStateService _homeState;
    private readonly TemperatureService _temperatureService;

    public string AudioUrlTasks { get; private set; }
    public string AudioUrlCalendar { get; private set; }

    public IndexModel(
        EspLightService lightService, 
        AppDbContext context, 
        CeilingLightService ceilingLightService, 
        AlarmService alarmService, 
        HomeStateService homeState, 
        TemperatureService temperatureService)
    {
        _lightService = lightService;
        _context = context;
        _ceilingLightService = ceilingLightService;
        _alarmService = alarmService;
        _homeState = homeState;
        _temperatureService = temperatureService;

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

    public Temperature? LatestTemperature { get; private set; }
    public List<Temperature>? TemperatureHistory { get; private set; }

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
        // Get temperature data
        LatestTemperature = await _temperatureService.GetLatestTemperatureAsync();
        TemperatureHistory = await _temperatureService.GetTemperatureHistoryAsync();

        // Get alarms
        Alarms = _alarmService.GetAllAlarms()
            .Where(a => a.Time > DateTime.UtcNow)
            .OrderBy(a => a.Time)
            .ToList();

        // Get light schedules
        LightSchedules = await _lightService.GetAllSchedulesAsync();
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

    //function to toggle the status
    public IActionResult OnPostToggleStatus()
    {
        _homeState.IsHome = !_homeState.IsHome;
        return RedirectToPage();
    }
}
