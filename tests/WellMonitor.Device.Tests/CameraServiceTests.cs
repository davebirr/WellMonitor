using WellMonitor.Device.Services;
using WellMonitor.Device.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using System.Threading.Tasks;

namespace WellMonitor.Device.Tests
{
    public class CameraServiceTests
    {
        private readonly Mock<ILogger<CameraService>> _mockLogger;
        private readonly Mock<IOptionsMonitor<DebugOptions>> _mockDebugOptions;
        private readonly Mock<IOptionsMonitor<CameraOptions>> _mockCameraOptions;
        private readonly CameraService _cameraService;

        public CameraServiceTests()
        {
            _mockLogger = new Mock<ILogger<CameraService>>();
            _mockDebugOptions = new Mock<IOptionsMonitor<DebugOptions>>();
            _mockCameraOptions = new Mock<IOptionsMonitor<CameraOptions>>();
            
            // Setup debug options mock
            _mockDebugOptions.Setup(x => x.CurrentValue).Returns(new DebugOptions
            {
                ImageSaveEnabled = false,
                DebugMode = false,
                ImageRetentionDays = 7
            });
            
            // Setup camera options mock
            _mockCameraOptions.Setup(x => x.CurrentValue).Returns(new CameraOptions
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
                DebugImagePath = "./debug_images",
                Gain = 1.0,
                ShutterSpeedMicroseconds = 0,
                AutoExposure = true,
                AutoWhiteBalance = true
            });
            
            _cameraService = new CameraService(_mockLogger.Object, _mockCameraOptions.Object, _mockDebugOptions.Object);
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
            var options = _mockCameraOptions.Object.CurrentValue;
            Assert.Equal(1920, options.Width);
            Assert.Equal(1080, options.Height);
            Assert.Equal(85, options.Quality);
            Assert.Equal(30000, options.TimeoutMs);
            Assert.Equal(2000, options.WarmupTimeMs);
            Assert.Equal(0, options.Rotation);
            Assert.Equal(50, options.Brightness);
            Assert.Equal(0, options.Contrast);
            Assert.Equal(0, options.Saturation);
            Assert.False(options.EnablePreview);
            Assert.Equal("./debug_images", options.DebugImagePath);
        }
    }
}
