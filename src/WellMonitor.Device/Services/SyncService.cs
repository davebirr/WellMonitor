using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    public class SyncService : ISyncService
    {
        public Task SyncTelemetryAsync()
        {
            // TODO: Implement sync logic
            return Task.CompletedTask;
        }

        public Task SyncSummariesAsync()
        {
            // TODO: Implement sync logic
            return Task.CompletedTask;
        }
    }
}
