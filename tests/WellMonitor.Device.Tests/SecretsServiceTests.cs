using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WellMonitor.Device.Services;
using Xunit;

namespace WellMonitor.Device.Tests
{
    public class SecretsServiceTests
    {
        [Fact]
        public async Task SimplifiedSecretsService_ReturnsExpectedValues()
        {
            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<SimplifiedSecretsService>>();
            
            mockConfig.Setup(c => c["WELLMONITOR_IOTHUB_CONNECTION_STRING"]).Returns("iot-conn");
            mockConfig.Setup(c => c["WELLMONITOR_STORAGE_CONNECTION_STRING"]).Returns("storage-conn");
            mockConfig.Setup(c => c["WELLMONITOR_LOCAL_ENCRYPTION_KEY"]).Returns("key");

            var service = new SimplifiedSecretsService(mockConfig.Object, mockLogger.Object);
            Assert.Equal("iot-conn", await service.GetIotHubConnectionStringAsync());
            Assert.Equal("storage-conn", await service.GetStorageConnectionStringAsync());
            Assert.Equal("key", await service.GetLocalEncryptionKeyAsync());
        }
    }
}
