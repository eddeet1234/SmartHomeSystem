using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartHomeSystem.Data.Model;
using SmartHomeSystem.Services;

namespace SmartHomeSystem.Pages
{
    public class EditLightScheduleModel : PageModel
    {
        private readonly EspLightService _lightService;

        [BindProperty]
        public LightSchedule Schedule { get; set; }

        public EditLightScheduleModel(EspLightService lightService)
        {
            _lightService = lightService;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Schedule = await _lightService.GetScheduleAsync(id);

            if (Schedule == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await _lightService.UpdateScheduleAsync(Schedule);
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to update schedule. Please try again.");
                return Page();
            }
        }
    }
}
