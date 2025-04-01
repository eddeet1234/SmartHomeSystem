using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartHomeSystem.Data.Model;
using SmartHomeSystem.Services;

namespace SmartHomeSystem.Pages
{
    public class EditAlarmModel : PageModel
    {
        private readonly AlarmService _alarmService;

        [BindProperty]
        public Alarm Alarm { get; set; }

        public EditAlarmModel(AlarmService alarmService)
        {
            _alarmService = alarmService;
        }

        public IActionResult OnGet(int id)
        {
            Alarm = _alarmService.GetAlarm(id);
            
            if (Alarm == null)
            {
                return NotFound();
            }

            // Convert UTC time from database to local time for the form
            Alarm.Time = Alarm.Time.ToLocalTime();
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Convert local time back to UTC for storage
            Alarm.Time = Alarm.Time.ToUniversalTime();
            _alarmService.UpdateAlarm(Alarm);
            
            return RedirectToPage("Index");
        }
    }
}
