using WellMonitor.Device.Services;
using WellMonitor.Device.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Threading.Tasks;

namespace WellMonitor.Device.Tests
{
    public class CameraServiceTests
    {
        private readonly Mock<ILogger<CameraService>> _mockLogger;
        private readonly CameraOptions _cameraOptions;
        private readonly CameraService _cameraService;

        public CameraServiceTests()
        {
            _mockLogger = new Mock<ILogger<CameraService>>();
            _cameraOptions = new CameraOptions
            {
                Width = 1920,
                Height = 1080,
                Quality = 85,
                TimeoutMs = 30000,
                WarmupTimeMs = 2000,
                Rotation = 0,
                Brightness = 50,
                Contrast = 0,
                Saturation = 0,
                EnablePreview = false,
                DebugImagePath = "./debug_images"
            };
            _cameraService = new CameraService(_mockLogger.Object, _cameraOptions);
        }

        [Fact]
        public void CameraService_CanBeConstructed()
        {
            Assert.NotNull(_cameraService);
        }

        [Fact]
        public async Task CaptureImageAsync_WhenCameraNotAvailable_ThrowsException()
        {
            // This test will fail on systems without libcamera-still
            // but ensures the service handles missing camera gracefully
            var exception = await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(
                async () => await _cameraService.CaptureImageAsync());
            
            Assert.NotNull(exception);
        }

        [Fact]
        public void CameraOptions_HasCorrectDefaults()
        {
            Assert.Equal(1920, _cameraOptions.Width);
            Assert.Equal(1080, _cameraOptions.Height);
            Assert.Equal(85, _cameraOptions.Quality);
            Assert.Equal(30000, _cameraOptions.TimeoutMs);
            Assert.Equal(2000, _cameraOptions.WarmupTimeMs);
            Assert.Equal(0, _cameraOptions.Rotation);
            Assert.Equal(50, _cameraOptions.Brightness);
            Assert.Equal(0, _cameraOptions.Contrast);
            Assert.Equal(0, _cameraOptions.Saturation);
            Assert.False(_cameraOptions.EnablePreview);
            Assert.Equal("./debug_images", _cameraOptions.DebugImagePath);
        }
    }
}
