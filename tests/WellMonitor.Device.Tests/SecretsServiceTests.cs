using Microsoft.Extensions.Configuration;
using Moq;
using WellMonitor.Device.Services;
using Xunit;

namespace WellMonitor.Device.Tests
{
    public class SecretsServiceTests
    {
        [Fact]
        public void SecretsService_ReturnsExpectedValues()
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["IotHubConnectionString"]).Returns("iot-conn");
            mockConfig.Setup(c => c["AzureStorageConnectionString"]).Returns("storage-conn");
            mockConfig.Setup(c => c["LocalEncryptionKey"]).Returns("key");

            var service = new SecretsService(mockConfig.Object);
            Assert.Equal("iot-conn", service.GetIotHubConnectionString());
            Assert.Equal("storage-conn", service.GetStorageConnectionString());
            Assert.Equal("key", service.GetLocalEncryptionKey());
        }
    }
}
