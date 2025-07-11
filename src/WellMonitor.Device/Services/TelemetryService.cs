using System.Threading.Tasks;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Services
{
    public class TelemetryService : ITelemetryService
    {
        public Task SendTelemetryAsync(Reading reading)
        {
            // TODO: Implement Azure IoT telemetry send
            return Task.CompletedTask;
        }

        public Task SendRelayActionLogAsync(RelayActionLog log)
        {
            // TODO: Implement Azure IoT log send
            return Task.CompletedTask;
        }
    }
}
