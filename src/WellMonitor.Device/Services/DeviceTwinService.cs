using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services
{
    public interface IDeviceTwinService
    {
        Task<DeviceTwinConfig> FetchAndApplyConfigAsync(DeviceClient deviceClient, IConfiguration configuration, GpioOptions gpioOptions, CameraOptions cameraOptions, ILogger logger);
    }

    public class DeviceTwinService : IDeviceTwinService
    {
        public async Task<DeviceTwinConfig> FetchAndApplyConfigAsync(DeviceClient deviceClient, IConfiguration configuration, GpioOptions gpioOptions, CameraOptions cameraOptions, ILogger logger)
        {
            // Fetch device twin properties
            Twin twin = await deviceClient.GetTwinAsync();
            var desired = twin.Properties.Desired;

            // Validate device twin properties first
            var validationService = new ConfigurationValidationService();
            var deviceTwinValidation = validationService.ValidateDeviceTwinProperties(desired);
            
            if (!deviceTwinValidation.IsValid)
            {
                logger.LogWarning("Device twin validation errors: {Errors}", deviceTwinValidation.GetErrorSummary());
            }
            
            if (deviceTwinValidation.HasWarnings)
            {
                logger.LogWarning("Device twin validation warnings: {Warnings}", deviceTwinValidation.GetErrorSummary());
            }

            // Read configuration from device twin desired properties (with fallback to config file)
            double currentThreshold = desired.Contains("currentThreshold") ? (double)desired["currentThreshold"] : configuration.GetValue("CurrentThreshold", 4.5);
            int cycleTimeThreshold = desired.Contains("cycleTimeThreshold") ? (int)desired["cycleTimeThreshold"] : configuration.GetValue("CycleTimeThreshold", 30);
            int relayDebounceMs = desired.Contains("relayDebounceMs") ? (int)desired["relayDebounceMs"] : configuration.GetValue("RelayDebounceMs", 500);
            gpioOptions.RelayDebounceMs = relayDebounceMs;
            int syncIntervalMinutes = desired.Contains("syncIntervalMinutes") ? (int)desired["syncIntervalMinutes"] : configuration.GetValue("SyncIntervalMinutes", 5);
            int logRetentionDays = desired.Contains("logRetentionDays") ? (int)desired["logRetentionDays"] : configuration.GetValue("LogRetentionDays", 14);
            string ocrMode = desired.Contains("ocrMode") ? (string)desired["ocrMode"] : configuration.GetValue("OcrMode", "tesseract");
            bool powerAppEnabled = desired.Contains("powerAppEnabled") ? (bool)desired["powerAppEnabled"] : configuration.GetValue("PowerAppEnabled", true);

            // Camera configuration from device twin (with fallback to defaults)
            UpdateCameraOptionsFromDeviceTwin(desired, configuration, cameraOptions, logger);

            // Create config object for validation
            var config = new DeviceTwinConfig
            {
                CurrentThreshold = currentThreshold,
                CycleTimeThreshold = cycleTimeThreshold,
                RelayDebounceMs = relayDebounceMs,
                SyncIntervalMinutes = syncIntervalMinutes,
                LogRetentionDays = logRetentionDays,
                OcrMode = ocrMode,
                PowerAppEnabled = powerAppEnabled
            };

            // Validate well monitor configuration
            var validationResult = validationService.ValidateWellMonitorConfiguration(config);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Invalid well monitor configuration detected from device twin: {Errors}", 
                    validationResult.GetErrorSummary());
                
                // Apply safe fallbacks for invalid values
                config = validationService.ApplyWellMonitorFallbacks(config);
                
                logger.LogInformation("Applied safe fallback values for invalid well monitor configuration");
            }
            else
            {
                logger.LogInformation("Well monitor configuration validated successfully from device twin");
            }

            logger.LogInformation($"Loaded config: currentThreshold={config.CurrentThreshold}, cycleTimeThreshold={config.CycleTimeThreshold}, relayDebounceMs={config.RelayDebounceMs}, syncIntervalMinutes={config.SyncIntervalMinutes}, logRetentionDays={config.LogRetentionDays}, ocrMode={config.OcrMode}, powerAppEnabled={config.PowerAppEnabled}");
            logger.LogInformation($"Camera config: width={cameraOptions.Width}, height={cameraOptions.Height}, quality={cameraOptions.Quality}, brightness={cameraOptions.Brightness}, contrast={cameraOptions.Contrast}, rotation={cameraOptions.Rotation}");

            return config;
        }

        private void UpdateCameraOptionsFromDeviceTwin(TwinCollection desired, IConfiguration configuration, CameraOptions cameraOptions, ILogger logger)
        {
            // Update camera options from device twin with fallback to current values
            if (desired.Contains("cameraWidth"))
                cameraOptions.Width = (int)desired["cameraWidth"];
            
            if (desired.Contains("cameraHeight"))
                cameraOptions.Height = (int)desired["cameraHeight"];
            
            if (desired.Contains("cameraQuality"))
                cameraOptions.Quality = (int)desired["cameraQuality"];
            
            if (desired.Contains("cameraTimeoutMs"))
                cameraOptions.TimeoutMs = (int)desired["cameraTimeoutMs"];
            
            if (desired.Contains("cameraWarmupTimeMs"))
                cameraOptions.WarmupTimeMs = (int)desired["cameraWarmupTimeMs"];
            
            if (desired.Contains("cameraRotation"))
                cameraOptions.Rotation = (int)desired["cameraRotation"];
            
            if (desired.Contains("cameraBrightness"))
                cameraOptions.Brightness = (int)desired["cameraBrightness"];
            
            if (desired.Contains("cameraContrast"))
                cameraOptions.Contrast = (int)desired["cameraContrast"];
            
            if (desired.Contains("cameraSaturation"))
                cameraOptions.Saturation = (int)desired["cameraSaturation"];
            
            if (desired.Contains("cameraEnablePreview"))
                cameraOptions.EnablePreview = (bool)desired["cameraEnablePreview"];
            
            if (desired.Contains("cameraDebugImagePath"))
                cameraOptions.DebugImagePath = (string)desired["cameraDebugImagePath"];

            // Validate camera configuration
            var validationService = new ConfigurationValidationService();
            var validationResult = validationService.ValidateCameraConfiguration(cameraOptions);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Invalid camera configuration detected from device twin: {Errors}", 
                    validationResult.GetErrorSummary());
                
                // Apply safe fallbacks for invalid values
                validationService.ApplyCameraFallbacks(cameraOptions);
                
                logger.LogInformation("Applied safe fallback values for invalid camera configuration");
            }
            else
            {
                logger.LogInformation("Camera configuration validated successfully from device twin");
            }
        }
    }

    public class DeviceTwinConfig
    {
        public double CurrentThreshold { get; set; }
        public int CycleTimeThreshold { get; set; }
        public int RelayDebounceMs { get; set; }
        public int SyncIntervalMinutes { get; set; }
        public int LogRetentionDays { get; set; }
        public string OcrMode { get; set; } = "tesseract";
        public bool PowerAppEnabled { get; set; }
    }
}
