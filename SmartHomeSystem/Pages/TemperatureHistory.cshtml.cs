using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartHomeSystem.Services;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartHomeSystem.Data.Model;

namespace SmartHomeSystem.Pages
{
    public class TemperatureHistoryModel : PageModel
    {
        private readonly TemperatureService _temperatureService;

        public TemperatureHistoryModel(TemperatureService temperatureService)
        {
            _temperatureService = temperatureService;
        }

        public List<Temperature> Temperatures { get; set; }

        public async Task OnGetAsync()
        {
            Temperatures = await _temperatureService.GetTemperatureHistoryAsync();
        }
    }
}