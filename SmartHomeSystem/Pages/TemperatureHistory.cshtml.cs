using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartHomeSystem.Services;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SmartHomeSystem.Pages
{
    public class TemperatureHistoryModel : PageModel
    {
        private readonly TemperatureService _temperatureService;

        public TemperatureHistoryModel(TemperatureService temperatureService)
        {
            _temperatureService = temperatureService;
        }

        public List<SmartHomeSystem.Data.Model.Temperature> Temperatures { get; set; }

        public async Task OnGetAsync()
        {
            var query = await _temperatureService.GetTemperatureHistoryAsync();
            Temperatures = await query.ToListAsync();
        }
    }
}