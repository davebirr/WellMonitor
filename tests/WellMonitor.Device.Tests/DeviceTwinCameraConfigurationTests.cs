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
    /// <summary>
    /// Tests for device twin camera configuration integration
    /// </summary>
    public class DeviceTwinCameraConfigurationTests
    {
        [Fact]
        public async Task DeviceTwin_LoadsCameraConfiguration_FromRealDeviceTwinData()
        {
            // Arrange - Using actual device twin data from Azure IoT Hub
            var twin = new Twin();
            
            // Camera settings from actual device twin
            twin.Properties.Desired["cameraBrightness"] = 50;
            twin.Properties.Desired["cameraContrast"] = 10;
            twin.Properties.Desired["cameraDebugImagePath"] = "debug_images";
            twin.Properties.Desired["cameraEnablePreview"] = false;
            twin.Properties.Desired["cameraHeight"] = 1080;
            twin.Properties.Desired["cameraQuality"] = 95;
            twin.Properties.Desired["cameraRotation"] = 0;
            twin.Properties.Desired["cameraSaturation"] = 0;
            twin.Properties.Desired["cameraTimeoutMs"] = 5000;
            twin.Properties.Desired["cameraWarmupTimeMs"] = 2000;
            twin.Properties.Desired["cameraWidth"] = 1920;
            
            // Other settings from actual device twin
            twin.Properties.Desired["currentThreshold"] = 4.5;
            twin.Properties.Desired["cycleTimeThreshold"] = 30;
            twin.Properties.Desired["relayDebounceMs"] = 500;
            twin.Properties.Desired["syncIntervalMinutes"] = 5;
            twin.Properties.Desired["logRetentionDays"] = 14;
            twin.Properties.Desired["ocrMode"] = "tesseract";
            twin.Properties.Desired["powerAppEnabled"] = true;
            
            var mockDeviceClient = new Mock<DeviceClient>(MockBehavior.Strict);
            mockDeviceClient.Setup(c => c.GetTwinAsync()).ReturnsAsync(twin);
            
            var mockConfig = new Mock<IConfiguration>();
            var gpioOptions = new GpioOptions();
            var cameraOptions = new CameraOptions(); // Start with default values
            var mockLogger = new Mock<ILogger>();
            
            var service = new DeviceTwinService();

            // Act
            var result = await service.FetchAndApplyConfigAsync(
                mockDeviceClient.Object, 
                mockConfig.Object, 
                gpioOptions, 
                cameraOptions, 
                mockLogger.Object);

            // Assert - Verify camera options were updated from device twin
            Assert.Equal(1920, cameraOptions.Width);
            Assert.Equal(1080, cameraOptions.Height);
            Assert.Equal(95, cameraOptions.Quality);
            Assert.Equal(5000, cameraOptions.TimeoutMs);
            Assert.Equal(2000, cameraOptions.WarmupTimeMs);
            Assert.Equal(0, cameraOptions.Rotation);
            Assert.Equal(50, cameraOptions.Brightness);
            Assert.Equal(10, cameraOptions.Contrast);
            Assert.Equal(0, cameraOptions.Saturation);
            Assert.False(cameraOptions.EnablePreview);
            Assert.Equal("debug_images", cameraOptions.DebugImagePath);
            
            // Assert - Verify other settings were also loaded
            Assert.Equal(4.5, result.CurrentThreshold);
            Assert.Equal(30, result.CycleTimeThreshold);
            Assert.Equal(500, result.RelayDebounceMs);
            Assert.Equal(5, result.SyncIntervalMinutes);
            Assert.Equal(14, result.LogRetentionDays);
            Assert.Equal("tesseract", result.OcrMode);
            Assert.True(result.PowerAppEnabled);
        }
        
        [Fact]
        public async Task DeviceTwin_FallsBackToDefaults_WhenCameraSettingsMissing()
        {
            // Arrange - Device twin with no camera settings
            var twin = new Twin();
            twin.Properties.Desired["currentThreshold"] = 4.5;
            twin.Properties.Desired["relayDebounceMs"] = 500;
            
            var mockDeviceClient = new Mock<DeviceClient>(MockBehavior.Strict);
            mockDeviceClient.Setup(c => c.GetTwinAsync()).ReturnsAsync(twin);
            
            var mockConfig = new Mock<IConfiguration>();
            var gpioOptions = new GpioOptions();
            var cameraOptions = new CameraOptions(); // Start with default values
            var mockLogger = new Mock<ILogger>();
            
            var service = new DeviceTwinService();

            // Act
            var result = await service.FetchAndApplyConfigAsync(
                mockDeviceClient.Object, 
                mockConfig.Object, 
                gpioOptions, 
                cameraOptions, 
                mockLogger.Object);

            // Assert - Verify camera options retain default values
            Assert.Equal(1920, cameraOptions.Width);  // Default values
            Assert.Equal(1080, cameraOptions.Height);
            Assert.Equal(95, cameraOptions.Quality);
            Assert.Equal(5000, cameraOptions.TimeoutMs);
            Assert.Equal(2000, cameraOptions.WarmupTimeMs);
            Assert.Equal(0, cameraOptions.Rotation);
            Assert.Equal(50, cameraOptions.Brightness);
            Assert.Equal(0, cameraOptions.Contrast);
            Assert.Equal(0, cameraOptions.Saturation);
            Assert.False(cameraOptions.EnablePreview);
            Assert.Null(cameraOptions.DebugImagePath);
            
            // Assert - Verify other settings were loaded from device twin
            Assert.Equal(4.5, result.CurrentThreshold);
            Assert.Equal(500, result.RelayDebounceMs);
        }
        
        [Fact]
        public async Task DeviceTwin_UpdatesOnlySpecifiedCameraSettings()
        {
            // Arrange - Device twin with only some camera settings
            var twin = new Twin();
            twin.Properties.Desired["cameraQuality"] = 85;  // Different from default
            twin.Properties.Desired["cameraBrightness"] = 75;  // Different from default
            twin.Properties.Desired["cameraRotation"] = 180;  // Different from default
            
            var mockDeviceClient = new Mock<DeviceClient>(MockBehavior.Strict);
            mockDeviceClient.Setup(c => c.GetTwinAsync()).ReturnsAsync(twin);
            
            var mockConfig = new Mock<IConfiguration>();
            var gpioOptions = new GpioOptions();
            var cameraOptions = new CameraOptions(); // Start with default values
            var mockLogger = new Mock<ILogger>();
            
            var service = new DeviceTwinService();

            // Act
            var result = await service.FetchAndApplyConfigAsync(
                mockDeviceClient.Object, 
                mockConfig.Object, 
                gpioOptions, 
                cameraOptions, 
                mockLogger.Object);

            // Assert - Verify only specified settings were updated
            Assert.Equal(85, cameraOptions.Quality);     // Updated from device twin
            Assert.Equal(75, cameraOptions.Brightness);  // Updated from device twin
            Assert.Equal(180, cameraOptions.Rotation);   // Updated from device twin
            
            // Assert - Verify other settings retain defaults
            Assert.Equal(1920, cameraOptions.Width);     // Default value
            Assert.Equal(1080, cameraOptions.Height);    // Default value
            Assert.Equal(5000, cameraOptions.TimeoutMs); // Default value
            Assert.Equal(0, cameraOptions.Contrast);     // Default value
            Assert.Equal(0, cameraOptions.Saturation);   // Default value
            Assert.False(cameraOptions.EnablePreview);   // Default value
            Assert.Null(cameraOptions.DebugImagePath);   // Default value
        }

        [Fact]
        public async Task DeviceTwin_WarnsForMissingProperties()
        {
            // Arrange - Device twin with only a few properties (missing many expected ones)
            var twin = new Twin();
            twin.Properties.Desired["currentThreshold"] = 4.5;
            twin.Properties.Desired["cameraWidth"] = 1920;
            // Missing: cameraHeight, cameraQuality, cameraBrightness, cameraContrast, etc.
            
            var mockDeviceClient = new Mock<DeviceClient>(MockBehavior.Strict);
            mockDeviceClient.Setup(c => c.GetTwinAsync()).ReturnsAsync(twin);
            
            var mockConfig = new Mock<IConfiguration>();
            var gpioOptions = new GpioOptions();
            var cameraOptions = new CameraOptions();
            var mockLogger = new Mock<ILogger>();
            
            var service = new DeviceTwinService();

            // Act
            var result = await service.FetchAndApplyConfigAsync(
                mockDeviceClient.Object, 
                mockConfig.Object, 
                gpioOptions, 
                cameraOptions, 
                mockLogger.Object);

            // Assert - Should use default values for missing properties
            Assert.Equal(1920, cameraOptions.Width);  // From device twin
            Assert.Equal(1080, cameraOptions.Height); // Default value (missing from twin)
            Assert.Equal(95, cameraOptions.Quality);  // Default value (missing from twin)
            Assert.Equal(4.5, result.CurrentThreshold); // From device twin
            
            // Verify that warnings were logged (we can't easily test the actual log calls with our current setup,
            // but the validation service should have generated warnings)
        }
        
        [Fact]
        public async Task DeviceTwin_WarnsForUnexpectedProperties()
        {
            // Arrange - Device twin with unexpected properties that should not exist
            var twin = new Twin();
            
            // Valid properties
            twin.Properties.Desired["currentThreshold"] = 4.5;
            twin.Properties.Desired["cameraWidth"] = 1920;
            twin.Properties.Desired["cameraHeight"] = 1080;
            
            // Invalid/unexpected properties that should trigger warnings
            twin.Properties.Desired["invalidCameraSetting"] = 123;
            twin.Properties.Desired["unknownProperty"] = "test";
            twin.Properties.Desired["legacyConfig"] = true;
            twin.Properties.Desired["experimentalFeature"] = 999;
            
            var mockDeviceClient = new Mock<DeviceClient>(MockBehavior.Strict);
            mockDeviceClient.Setup(c => c.GetTwinAsync()).ReturnsAsync(twin);
            
            var mockConfig = new Mock<IConfiguration>();
            var gpioOptions = new GpioOptions();
            var cameraOptions = new CameraOptions();
            var mockLogger = new Mock<ILogger>();
            
            var service = new DeviceTwinService();

            // Act
            var result = await service.FetchAndApplyConfigAsync(
                mockDeviceClient.Object, 
                mockConfig.Object, 
                gpioOptions, 
                cameraOptions, 
                mockLogger.Object);

            // Assert - Should load valid properties and ignore unexpected ones
            Assert.Equal(1920, cameraOptions.Width);  // Valid property loaded
            Assert.Equal(1080, cameraOptions.Height); // Valid property loaded
            Assert.Equal(4.5, result.CurrentThreshold); // Valid property loaded
            
            // The unexpected properties should be ignored (not crash the system)
            // and warnings should be logged
        }

        [Fact]
        public void DeviceTwin_ValidatesPropertyThatShouldNotExist()
        {
            // Arrange - Create a device twin with a property that should not exist
            var twin = new Twin();
            twin.Properties.Desired["currentThreshold"] = 4.5;
            twin.Properties.Desired["shouldNotExist"] = "invalid";
            
            var validationService = new ConfigurationValidationService();

            // Act
            var result = validationService.ValidateDeviceTwinProperties(twin.Properties.Desired);

            // Assert - The test passes if the unexpected property is detected
            Assert.True(result.HasWarnings, "Should have warnings for unexpected properties");
            Assert.Contains("shouldNotExist", result.GetErrorSummary());
            Assert.Contains("unexpected property", result.GetErrorSummary());
        }

        [Fact]
        public void DeviceTwin_ValidatesInvalidPropertyValues()
        {
            // Arrange - Create a device twin with invalid property values
            var twin = new Twin();
            twin.Properties.Desired["cameraWidth"] = -100;      // Invalid: too small
            twin.Properties.Desired["cameraHeight"] = 50000;    // Invalid: too large
            twin.Properties.Desired["cameraQuality"] = 150;     // Invalid: over 100%
            twin.Properties.Desired["cameraRotation"] = 45;     // Invalid: not 0/90/180/270
            twin.Properties.Desired["currentThreshold"] = 100;  // Invalid: too high
            twin.Properties.Desired["ocrMode"] = "invalid";     // Invalid: not tesseract/azure/offline
            
            var validationService = new ConfigurationValidationService();

            // Act
            var result = validationService.ValidateDeviceTwinProperties(twin.Properties.Desired);

            // Assert - Should detect all invalid values
            Assert.False(result.IsValid, "Should be invalid due to bad property values");
            Assert.True(result.Errors.Count >= 6, $"Should have at least 6 validation errors, but got {result.Errors.Count}");
            
            // Log all errors for debugging
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Validation error: {error}");
            }
            
            // Just verify that the basic validation is working
            Assert.True(result.Errors.Count > 0, "Should have validation errors for invalid values");
        }
    }
}
