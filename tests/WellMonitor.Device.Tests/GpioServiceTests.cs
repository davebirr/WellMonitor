using System.Threading.Tasks;
using Moq;
using WellMonitor.Device.Services;
using WellMonitor.Device.Models;
using Xunit;

namespace WellMonitor.Device.Tests
{
    public class GpioServiceTests
    {
        [Fact]
        public void GpioService_InitializesWithOptions()
        {
            var options = new GpioOptions { RelayDebounceMs = 123 };
            var service = new GpioService(options);
            Assert.Equal(123, options.RelayDebounceMs);
        }
        // Add more tests for relay control, debounce, etc. with mocks as needed
    }
}
