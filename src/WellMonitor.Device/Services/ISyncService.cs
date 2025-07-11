using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    public interface ISyncService
    {
        Task SyncTelemetryAsync();
        Task SyncSummariesAsync();
    }
}
