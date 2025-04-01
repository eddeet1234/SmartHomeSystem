using System.Diagnostics;

namespace SmartHomeSystem.Services
{
    public class CeilingLightService
    {
        private readonly ILogger<CeilingLightService> _logger;

        public CeilingLightService(ILogger<CeilingLightService> logger)
        {
            _logger = logger;
        }

        public async Task ToggleAsync()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python3", // Or "python" if that's what your system uses
                    ArgumentList = { "/home/eddeet2001/rf_transfer.py" }, // Use full path!
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                _logger.LogInformation($"rf_transfer.py output: {output}");
                if (!string.IsNullOrWhiteSpace(error))
                    _logger.LogWarning($"rf_transfer.py error: {error}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling ceiling light.");
                throw;
            }
        }
    }
}
