using System.Threading.Tasks;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Services
{
    public interface ITelemetryService
    {
        Task SendTelemetryAsync(Reading reading);
        Task SendRelayActionLogAsync(RelayActionLog log);
    }
}
