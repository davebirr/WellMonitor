using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WellMonitor.Device.Models;
using WellMonitor.Device.Services;
using Xunit;

namespace WellMonitor.Device.Tests
{
    /// <summary>
    /// Integration tests for device twin camera configuration with real Azure IoT Hub connection
    /// </summary>
    public class DeviceTwinCameraConfigurationIntegrationTests
    {
        private readonly string? _iotHubConnectionString;
        private readonly ISecretsService _secretsService;
        private readonly ILogger<DeviceTwinService> _logger;

        public DeviceTwinCameraConfigurationIntegrationTests()
        {
            // Build configuration to get connection string from environment variables or .env file
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .Build();

            var mockLogger = new Mock<ILogger<SimplifiedSecretsService>>();
            _secretsService = new SimplifiedSecretsService(configuration, mockLogger.Object);
            _iotHubConnectionString = _secretsService.GetIotHubConnectionStringAsync().GetAwaiter().GetResult();
            
            var mockDeviceTwinLogger = new Mock<ILogger<DeviceTwinService>>();
            _logger = mockDeviceTwinLogger.Object;
        }

        [Fact]
        public async Task DeviceTwin_LoadsRealConfigurationFromAzureIoTHub()
        {
            // Skip test if no connection string available
            if (string.IsNullOrEmpty(_iotHubConnectionString) || _iotHubConnectionString.Contains("test-"))
            {
                // Use xUnit.Skip to skip the test
                return;
            }

            // Arrange
            var deviceClient = DeviceClient.CreateFromConnectionString(_iotHubConnectionString);
            var mockConfig = new Mock<IConfiguration>();
            var gpioOptions = new GpioOptions();
            var cameraOptions = new CameraOptions();
            var service = new DeviceTwinService();

            try
            {
                // Act - Load real device twin configuration
                var result = await service.FetchAndApplyConfigAsync(
                    deviceClient, 
                    mockConfig.Object, 
                    gpioOptions, 
                    cameraOptions, 
                    _logger);

                // Assert - Verify all expected camera properties are loaded and valid
                ValidateCameraConfiguration(cameraOptions);
                ValidateWellMonitorConfiguration(result);
            }
            finally
            {
                await deviceClient.CloseAsync();
            }
        }

        [Fact]
        public void CameraOptions_ValidateInputRanges()
        {
            // Arrange
            var cameraOptions = new CameraOptions();

            // Test valid ranges
            Assert.True(IsValidWidth(1920));
            Assert.True(IsValidWidth(1280));
            Assert.True(IsValidWidth(640));
            Assert.False(IsValidWidth(100));   // Too small
            Assert.False(IsValidWidth(5000));  // Too large

            Assert.True(IsValidHeight(1080));
            Assert.True(IsValidHeight(720));
            Assert.True(IsValidHeight(480));
            Assert.False(IsValidHeight(100));  // Too small
            Assert.False(IsValidHeight(3000)); // Too large

            Assert.True(IsValidQuality(95));
            Assert.True(IsValidQuality(85));
            Assert.True(IsValidQuality(75));
            Assert.False(IsValidQuality(0));   // Too low
            Assert.False(IsValidQuality(101)); // Too high

            Assert.True(IsValidBrightness(50));
            Assert.True(IsValidBrightness(25));
            Assert.True(IsValidBrightness(75));
            Assert.False(IsValidBrightness(-1));  // Too low
            Assert.False(IsValidBrightness(101)); // Too high

            Assert.True(IsValidContrast(10));
            Assert.True(IsValidContrast(0));
            Assert.True(IsValidContrast(-10));
            Assert.False(IsValidContrast(-101)); // Too low
            Assert.False(IsValidContrast(101));  // Too high

            Assert.True(IsValidRotation(0));
            Assert.True(IsValidRotation(90));
            Assert.True(IsValidRotation(180));
            Assert.True(IsValidRotation(270));
            Assert.False(IsValidRotation(45));   // Invalid rotation
            Assert.False(IsValidRotation(360));  // Invalid rotation

            Assert.True(IsValidTimeout(5000));
            Assert.True(IsValidTimeout(3000));
            Assert.True(IsValidTimeout(10000));
            Assert.False(IsValidTimeout(500));   // Too short
            Assert.False(IsValidTimeout(60000)); // Too long

            Assert.True(IsValidWarmupTime(2000));
            Assert.True(IsValidWarmupTime(1000));
            Assert.True(IsValidWarmupTime(5000));
            Assert.False(IsValidWarmupTime(100));  // Too short
            Assert.False(IsValidWarmupTime(10000)); // Too long
        }

        [Fact]
        public void WellMonitorConfiguration_ValidateInputRanges()
        {
            // Test current threshold validation
            Assert.True(IsValidCurrentThreshold(4.5));
            Assert.True(IsValidCurrentThreshold(3.0));
            Assert.True(IsValidCurrentThreshold(10.0));
            Assert.False(IsValidCurrentThreshold(0.0));   // Too low
            Assert.False(IsValidCurrentThreshold(50.0));  // Too high

            // Test cycle time threshold validation
            Assert.True(IsValidCycleTimeThreshold(30));
            Assert.True(IsValidCycleTimeThreshold(15));
            Assert.True(IsValidCycleTimeThreshold(60));
            Assert.False(IsValidCycleTimeThreshold(5));   // Too short
            Assert.False(IsValidCycleTimeThreshold(300)); // Too long

            // Test relay debounce validation
            Assert.True(IsValidRelayDebounce(500));
            Assert.True(IsValidRelayDebounce(250));
            Assert.True(IsValidRelayDebounce(1000));
            Assert.False(IsValidRelayDebounce(50));   // Too short
            Assert.False(IsValidRelayDebounce(5000)); // Too long

            // Test sync interval validation
            Assert.True(IsValidSyncInterval(5));
            Assert.True(IsValidSyncInterval(1));
            Assert.True(IsValidSyncInterval(30));
            Assert.False(IsValidSyncInterval(0));   // Too short
            Assert.False(IsValidSyncInterval(120)); // Too long

            // Test log retention validation
            Assert.True(IsValidLogRetention(14));
            Assert.True(IsValidLogRetention(7));
            Assert.True(IsValidLogRetention(30));
            Assert.False(IsValidLogRetention(1));   // Too short
            Assert.False(IsValidLogRetention(365)); // Too long
        }

        private void ValidateCameraConfiguration(CameraOptions cameraOptions)
        {
            // Validate all camera properties are within expected ranges
            Assert.True(IsValidWidth(cameraOptions.Width), 
                $"Camera width {cameraOptions.Width} is outside valid range (320-4096)");
            
            Assert.True(IsValidHeight(cameraOptions.Height), 
                $"Camera height {cameraOptions.Height} is outside valid range (240-2160)");
            
            Assert.True(IsValidQuality(cameraOptions.Quality), 
                $"Camera quality {cameraOptions.Quality} is outside valid range (1-100)");
            
            Assert.True(IsValidBrightness(cameraOptions.Brightness), 
                $"Camera brightness {cameraOptions.Brightness} is outside valid range (0-100)");
            
            Assert.True(IsValidContrast(cameraOptions.Contrast), 
                $"Camera contrast {cameraOptions.Contrast} is outside valid range (-100 to 100)");
            
            Assert.True(IsValidSaturation(cameraOptions.Saturation), 
                $"Camera saturation {cameraOptions.Saturation} is outside valid range (-100 to 100)");
            
            Assert.True(IsValidRotation(cameraOptions.Rotation), 
                $"Camera rotation {cameraOptions.Rotation} is not a valid value (0, 90, 180, 270)");
            
            Assert.True(IsValidTimeout(cameraOptions.TimeoutMs), 
                $"Camera timeout {cameraOptions.TimeoutMs}ms is outside valid range (1000-30000)");
            
            Assert.True(IsValidWarmupTime(cameraOptions.WarmupTimeMs), 
                $"Camera warmup time {cameraOptions.WarmupTimeMs}ms is outside valid range (500-8000)");

            // Validate debug path if specified
            if (!string.IsNullOrEmpty(cameraOptions.DebugImagePath))
            {
                Assert.True(IsValidDebugPath(cameraOptions.DebugImagePath), 
                    $"Camera debug path '{cameraOptions.DebugImagePath}' is not a valid path");
            }

            // Log the loaded configuration for verification
            Console.WriteLine($"Camera Configuration Loaded:");
            Console.WriteLine($"  Resolution: {cameraOptions.Width}x{cameraOptions.Height}");
            Console.WriteLine($"  Quality: {cameraOptions.Quality}%");
            Console.WriteLine($"  Brightness: {cameraOptions.Brightness}");
            Console.WriteLine($"  Contrast: {cameraOptions.Contrast}");
            Console.WriteLine($"  Saturation: {cameraOptions.Saturation}");
            Console.WriteLine($"  Rotation: {cameraOptions.Rotation}°");
            Console.WriteLine($"  Timeout: {cameraOptions.TimeoutMs}ms");
            Console.WriteLine($"  Warmup: {cameraOptions.WarmupTimeMs}ms");
            Console.WriteLine($"  Debug Path: {cameraOptions.DebugImagePath ?? "Not set"}");
        }

        private void ValidateWellMonitorConfiguration(DeviceTwinConfig config)
        {
            // Validate all well monitor properties are within expected ranges
            Assert.True(IsValidCurrentThreshold(config.CurrentThreshold), 
                $"Current threshold {config.CurrentThreshold} is outside valid range (0.1-25.0)");
            
            Assert.True(IsValidCycleTimeThreshold(config.CycleTimeThreshold), 
                $"Cycle time threshold {config.CycleTimeThreshold} is outside valid range (10-120)");
            
            Assert.True(IsValidRelayDebounce(config.RelayDebounceMs), 
                $"Relay debounce {config.RelayDebounceMs}ms is outside valid range (100-2000)");
            
            Assert.True(IsValidSyncInterval(config.SyncIntervalMinutes), 
                $"Sync interval {config.SyncIntervalMinutes} is outside valid range (1-60)");
            
            Assert.True(IsValidLogRetention(config.LogRetentionDays), 
                $"Log retention {config.LogRetentionDays} is outside valid range (3-90)");

            // Validate OCR mode
            Assert.True(IsValidOcrMode(config.OcrMode), 
                $"OCR mode '{config.OcrMode}' is not a valid option");

            // Log the loaded configuration for verification
            Console.WriteLine($"Well Monitor Configuration Loaded:");
            Console.WriteLine($"  Current Threshold: {config.CurrentThreshold}A");
            Console.WriteLine($"  Cycle Time Threshold: {config.CycleTimeThreshold}s");
            Console.WriteLine($"  Relay Debounce: {config.RelayDebounceMs}ms");
            Console.WriteLine($"  Sync Interval: {config.SyncIntervalMinutes}min");
            Console.WriteLine($"  Log Retention: {config.LogRetentionDays} days");
            Console.WriteLine($"  OCR Mode: {config.OcrMode}");
            Console.WriteLine($"  PowerApp Enabled: {config.PowerAppEnabled}");
        }

        // Camera validation methods
        private bool IsValidWidth(int width) => width >= 320 && width <= 4096;
        private bool IsValidHeight(int height) => height >= 240 && height <= 2160;
        private bool IsValidQuality(int quality) => quality >= 1 && quality <= 100;
        private bool IsValidBrightness(int brightness) => brightness >= 0 && brightness <= 100;
        private bool IsValidContrast(int contrast) => contrast >= -100 && contrast <= 100;
        private bool IsValidSaturation(int saturation) => saturation >= -100 && saturation <= 100;
        private bool IsValidRotation(int rotation) => rotation == 0 || rotation == 90 || rotation == 180 || rotation == 270;
        private bool IsValidTimeout(int timeoutMs) => timeoutMs >= 1000 && timeoutMs <= 30000;
        private bool IsValidWarmupTime(int warmupMs) => warmupMs >= 500 && warmupMs <= 8000;
        private bool IsValidDebugPath(string path) => !string.IsNullOrWhiteSpace(path) && path.Length <= 260;

        // Well monitor validation methods
        private bool IsValidCurrentThreshold(double threshold) => threshold >= 0.1 && threshold <= 25.0;
        private bool IsValidCycleTimeThreshold(int threshold) => threshold >= 10 && threshold <= 120;
        private bool IsValidRelayDebounce(int debounceMs) => debounceMs >= 100 && debounceMs <= 2000;
        private bool IsValidSyncInterval(int intervalMin) => intervalMin >= 1 && intervalMin <= 60;
        private bool IsValidLogRetention(int retentionDays) => retentionDays >= 3 && retentionDays <= 90;
        private bool IsValidOcrMode(string mode) => mode == "tesseract" || mode == "azure" || mode == "offline";
    }
}
