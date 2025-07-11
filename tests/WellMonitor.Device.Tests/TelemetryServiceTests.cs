using WellMonitor.Device.Services;
using Xunit;

namespace WellMonitor.Device.Tests
{
    public class TelemetryServiceTests
    {
        [Fact]
        public void TelemetryService_CanBeConstructed()
        {
            var service = new TelemetryService();
            Assert.NotNull(service);
        }
        // Add more tests for telemetry logic, message formatting, etc.
    }
}
