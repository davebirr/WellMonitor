using WellMonitor.Device.Services;
using Xunit;

namespace WellMonitor.Device.Tests
{
    public class SyncServiceTests
    {
        [Fact]
        public void SyncService_CanBeConstructed()
        {
            var service = new SyncService();
            Assert.NotNull(service);
        }
        // Add more tests for sync logic, queuing, retry, etc.
    }
}
