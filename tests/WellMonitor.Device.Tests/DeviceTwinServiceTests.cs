using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WellMonitor.Device.Models;
using WellMonitor.Device.Services;
using Xunit;

namespace WellMonitor.Device.Tests
{
    public class DeviceTwinServiceTests
    {
        [Fact]
        public async Task FetchAndApplyConfigAsync_AppliesDeviceTwinAndConfigValues()
        {
            // Arrange
            var twin = new Twin();
            twin.Properties.Desired["currentThreshold"] = 7.5;
            twin.Properties.Desired["relayDebounceMs"] = 111;
            var mockDeviceClient = new Mock<DeviceClient>(MockBehavior.Strict);
            mockDeviceClient.Setup(c => c.GetTwinAsync()).ReturnsAsync(twin);
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c.GetValue("CurrentThreshold", 4.5)).Returns(4.5);
            mockConfig.Setup(c => c.GetValue("RelayDebounceMs", 500)).Returns(500);
            var gpioOptions = new GpioOptions();
            var cameraOptions = new CameraOptions();
            var mockLogger = new Mock<ILogger>();
            var service = new DeviceTwinService();

            // Act
            var result = await service.FetchAndApplyConfigAsync(mockDeviceClient.Object, mockConfig.Object, gpioOptions, cameraOptions, mockLogger.Object);

            // Assert
            Assert.Equal(7.5, result.CurrentThreshold);
            Assert.Equal(111, result.RelayDebounceMs);
            Assert.Equal(111, gpioOptions.RelayDebounceMs);
        }
    }
}
