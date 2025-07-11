using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services
{
    public interface IDeviceTwinService
    {
        Task<DeviceTwinConfig> FetchAndApplyConfigAsync(DeviceClient deviceClient, IConfiguration configuration, GpioOptions gpioOptions, CameraOptions cameraOptions, ILogger logger);
        Task<OcrOptions> FetchAndApplyOcrConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger);
        Task ReportOcrStatusAsync(DeviceClient deviceClient, IOcrService ocrService, ILogger logger);
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

        /// <summary>
        /// Fetch and apply OCR configuration from device twin
        /// </summary>
        public async Task<OcrOptions> FetchAndApplyOcrConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger)
        {
            try
            {
                // Fetch device twin properties
                Twin twin = await deviceClient.GetTwinAsync();
                var desired = twin.Properties.Desired;

                // Create OCR options from device twin with fallbacks
                var ocrOptions = new OcrOptions
                {
                    Provider = desired.Contains("ocrProvider") ? (string)desired["ocrProvider"] : configuration.GetValue("OCR:Provider", "Tesseract"),
                    MinimumConfidence = desired.Contains("ocrMinimumConfidence") ? (double)desired["ocrMinimumConfidence"] : configuration.GetValue("OCR:MinimumConfidence", 0.7),
                    MaxRetryAttempts = desired.Contains("ocrMaxRetryAttempts") ? (int)desired["ocrMaxRetryAttempts"] : configuration.GetValue("OCR:MaxRetryAttempts", 3),
                    TimeoutSeconds = desired.Contains("ocrTimeoutSeconds") ? (int)desired["ocrTimeoutSeconds"] : configuration.GetValue("OCR:TimeoutSeconds", 30),
                    EnablePreprocessing = desired.Contains("ocrEnablePreprocessing") ? (bool)desired["ocrEnablePreprocessing"] : configuration.GetValue("OCR:EnablePreprocessing", true)
                };

                // Tesseract configuration
                ocrOptions.Tesseract.Language = desired.Contains("ocrTesseractLanguage") ? (string)desired["ocrTesseractLanguage"] : configuration.GetValue("OCR:Tesseract:Language", "eng");
                ocrOptions.Tesseract.EngineMode = desired.Contains("ocrTesseractEngineMode") ? (int)desired["ocrTesseractEngineMode"] : configuration.GetValue("OCR:Tesseract:EngineMode", 3);
                ocrOptions.Tesseract.PageSegmentationMode = desired.Contains("ocrTesseractPageSegmentationMode") ? (int)desired["ocrTesseractPageSegmentationMode"] : configuration.GetValue("OCR:Tesseract:PageSegmentationMode", 7);
                
                if (desired.Contains("ocrTesseractCharWhitelist"))
                {
                    ocrOptions.Tesseract.CustomConfig["tessedit_char_whitelist"] = (string)desired["ocrTesseractCharWhitelist"];
                }

                // Azure Cognitive Services configuration
                ocrOptions.AzureCognitiveServices.Endpoint = desired.Contains("ocrAzureEndpoint") ? (string)desired["ocrAzureEndpoint"] : configuration.GetValue("OCR:AzureCognitiveServices:Endpoint", "");
                ocrOptions.AzureCognitiveServices.Region = desired.Contains("ocrAzureRegion") ? (string)desired["ocrAzureRegion"] : configuration.GetValue("OCR:AzureCognitiveServices:Region", "eastus");
                ocrOptions.AzureCognitiveServices.UseReadApi = desired.Contains("ocrAzureUseReadApi") ? (bool)desired["ocrAzureUseReadApi"] : configuration.GetValue("OCR:AzureCognitiveServices:UseReadApi", true);

                // Image preprocessing configuration - handle both nested and flat structure
                if (desired.Contains("ocrImagePreprocessing"))
                {
                    var preprocessing = desired["ocrImagePreprocessing"];
                    if (preprocessing is Microsoft.Azure.Devices.Shared.TwinCollection preprocessingCollection)
                    {
                        ocrOptions.ImagePreprocessing.EnableGrayscale = preprocessingCollection.Contains("enableGrayscale") ? (bool)preprocessingCollection["enableGrayscale"] : configuration.GetValue("OCR:ImagePreprocessing:EnableGrayscale", true);
                        ocrOptions.ImagePreprocessing.EnableContrastEnhancement = preprocessingCollection.Contains("enableContrastEnhancement") ? (bool)preprocessingCollection["enableContrastEnhancement"] : configuration.GetValue("OCR:ImagePreprocessing:EnableContrastEnhancement", true);
                        ocrOptions.ImagePreprocessing.ContrastFactor = preprocessingCollection.Contains("contrastFactor") ? (double)preprocessingCollection["contrastFactor"] : configuration.GetValue("OCR:ImagePreprocessing:ContrastFactor", 1.5);
                        ocrOptions.ImagePreprocessing.EnableBrightnessAdjustment = preprocessingCollection.Contains("enableBrightnessAdjustment") ? (bool)preprocessingCollection["enableBrightnessAdjustment"] : configuration.GetValue("OCR:ImagePreprocessing:EnableBrightnessAdjustment", true);
                        ocrOptions.ImagePreprocessing.BrightnessAdjustment = preprocessingCollection.Contains("brightnessAdjustment") ? (int)preprocessingCollection["brightnessAdjustment"] : configuration.GetValue("OCR:ImagePreprocessing:BrightnessAdjustment", 10);
                        ocrOptions.ImagePreprocessing.EnableNoiseReduction = preprocessingCollection.Contains("enableNoiseReduction") ? (bool)preprocessingCollection["enableNoiseReduction"] : configuration.GetValue("OCR:ImagePreprocessing:EnableNoiseReduction", true);
                        ocrOptions.ImagePreprocessing.EnableEdgeEnhancement = preprocessingCollection.Contains("enableEdgeEnhancement") ? (bool)preprocessingCollection["enableEdgeEnhancement"] : configuration.GetValue("OCR:ImagePreprocessing:EnableEdgeEnhancement", false);
                        ocrOptions.ImagePreprocessing.EnableScaling = preprocessingCollection.Contains("enableScaling") ? (bool)preprocessingCollection["enableScaling"] : configuration.GetValue("OCR:ImagePreprocessing:EnableScaling", true);
                        ocrOptions.ImagePreprocessing.ScaleFactor = preprocessingCollection.Contains("scaleFactor") ? (double)preprocessingCollection["scaleFactor"] : configuration.GetValue("OCR:ImagePreprocessing:ScaleFactor", 2.0);
                        ocrOptions.ImagePreprocessing.EnableBinaryThresholding = preprocessingCollection.Contains("enableBinaryThresholding") ? (bool)preprocessingCollection["enableBinaryThresholding"] : configuration.GetValue("OCR:ImagePreprocessing:EnableBinaryThresholding", true);
                        ocrOptions.ImagePreprocessing.BinaryThreshold = preprocessingCollection.Contains("binaryThreshold") ? (int)preprocessingCollection["binaryThreshold"] : configuration.GetValue("OCR:ImagePreprocessing:BinaryThreshold", 128);
                    }
                }
                else
                {
                    // Fall back to flat structure for backward compatibility
                    ocrOptions.ImagePreprocessing.EnableScaling = desired.Contains("ocrImageScaling") ? (bool)desired["ocrImageScaling"] : configuration.GetValue("OCR:ImagePreprocessing:EnableScaling", true);
                    ocrOptions.ImagePreprocessing.ScaleFactor = desired.Contains("ocrImageScaleFactor") ? (double)desired["ocrImageScaleFactor"] : configuration.GetValue("OCR:ImagePreprocessing:ScaleFactor", 2.0);
                    ocrOptions.ImagePreprocessing.BinaryThreshold = desired.Contains("ocrImageBinaryThreshold") ? (int)desired["ocrImageBinaryThreshold"] : configuration.GetValue("OCR:ImagePreprocessing:BinaryThreshold", 128);
                    ocrOptions.ImagePreprocessing.ContrastFactor = desired.Contains("ocrImageContrastFactor") ? (double)desired["ocrImageContrastFactor"] : configuration.GetValue("OCR:ImagePreprocessing:ContrastFactor", 1.5);
                    ocrOptions.ImagePreprocessing.BrightnessAdjustment = desired.Contains("ocrImageBrightnessAdjustment") ? (int)desired["ocrImageBrightnessAdjustment"] : configuration.GetValue("OCR:ImagePreprocessing:BrightnessAdjustment", 10);
                }

                logger.LogInformation("OCR configuration loaded from device twin: Provider={Provider}, MinConfidence={MinConfidence}, Preprocessing={Preprocessing}",
                    ocrOptions.Provider, ocrOptions.MinimumConfidence, ocrOptions.EnablePreprocessing);

                return ocrOptions;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch OCR configuration from device twin, using defaults");
                
                // Return default configuration from appsettings
                return new OcrOptions
                {
                    Provider = configuration.GetValue("OCR:Provider", "Tesseract"),
                    MinimumConfidence = configuration.GetValue("OCR:MinimumConfidence", 0.7),
                    MaxRetryAttempts = configuration.GetValue("OCR:MaxRetryAttempts", 3),
                    TimeoutSeconds = configuration.GetValue("OCR:TimeoutSeconds", 30),
                    EnablePreprocessing = configuration.GetValue("OCR:EnablePreprocessing", true)
                };
            }
        }

        /// <summary>
        /// Report OCR status and statistics to device twin
        /// </summary>
        public async Task ReportOcrStatusAsync(DeviceClient deviceClient, IOcrService ocrService, ILogger logger)
        {
            try
            {
                var stats = ocrService.GetStatistics();
                
                // Get provider availability
                var providerAvailability = new Dictionary<string, bool>();
                
                // Note: In a real implementation, you'd check each provider's availability
                // For now, we'll report based on basic configuration
                providerAvailability["Tesseract"] = true; // Assume Tesseract is available
                providerAvailability["AzureCognitiveServices"] = false; // Will be determined by configuration

                var reportedProperties = new TwinCollection
                {
                    ["ocrStatistics"] = new
                    {
                        totalOperations = stats.TotalOperations,
                        successfulOperations = stats.SuccessfulOperations,
                        failedOperations = stats.FailedOperations,
                        successRate = stats.SuccessRate,
                        averageProcessingTimeMs = stats.AverageProcessingTimeMs,
                        averageConfidence = stats.AverageConfidence,
                        lastResetAt = stats.LastResetAt
                    },
                    ["ocrProviderAvailable"] = providerAvailability,
                    ["ocrLastUpdateUtc"] = DateTime.UtcNow
                };

                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
                
                logger.LogDebug("OCR status reported to device twin: {TotalOps} operations, {SuccessRate:P2} success rate",
                    stats.TotalOperations, stats.SuccessRate);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to report OCR status to device twin");
            }
        }

        /// <summary>
        /// Fetch and apply monitoring configuration from device twin
        /// </summary>
        public async Task<MonitoringOptions> FetchAndApplyMonitoringConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger)
        {
            try
            {
                Twin twin = await deviceClient.GetTwinAsync();
                TwinCollection desired = twin.Properties.Desired;

                var monitoringOptions = new MonitoringOptions();

                // Load monitoring settings from device twin with fallbacks
                monitoringOptions.MonitoringIntervalSeconds = desired.Contains("monitoringIntervalSeconds") ? (int)desired["monitoringIntervalSeconds"] : configuration.GetValue("WellMonitor:MonitoringIntervalSeconds", 30);
                monitoringOptions.TelemetryIntervalMinutes = desired.Contains("telemetryIntervalMinutes") ? (int)desired["telemetryIntervalMinutes"] : configuration.GetValue("WellMonitor:TelemetryIntervalMinutes", 5);
                monitoringOptions.SyncIntervalHours = desired.Contains("syncIntervalHours") ? (int)desired["syncIntervalHours"] : configuration.GetValue("WellMonitor:SyncIntervalHours", 1);
                monitoringOptions.DataRetentionDays = desired.Contains("dataRetentionDays") ? (int)desired["dataRetentionDays"] : configuration.GetValue("WellMonitor:DataRetentionDays", 30);

                logger.LogInformation("Monitoring configuration loaded from device twin: Monitoring={MonitoringInterval}s, Telemetry={TelemetryInterval}m, Sync={SyncInterval}h, Retention={DataRetention}d",
                    monitoringOptions.MonitoringIntervalSeconds, monitoringOptions.TelemetryIntervalMinutes, monitoringOptions.SyncIntervalHours, monitoringOptions.DataRetentionDays);

                return monitoringOptions;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch monitoring configuration from device twin, using defaults");
                return new MonitoringOptions();
            }
        }

        /// <summary>
        /// Fetch and apply image quality configuration from device twin
        /// </summary>
        public async Task<ImageQualityOptions> FetchAndApplyImageQualityConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger)
        {
            try
            {
                Twin twin = await deviceClient.GetTwinAsync();
                TwinCollection desired = twin.Properties.Desired;

                var imageQualityOptions = new ImageQualityOptions();

                // Load image quality settings from device twin with fallbacks
                imageQualityOptions.MinThreshold = desired.Contains("imageQualityMinThreshold") ? (double)desired["imageQualityMinThreshold"] : configuration.GetValue("ImageQuality:MinThreshold", 0.7);
                imageQualityOptions.BrightnessMin = desired.Contains("imageQualityBrightnessMin") ? (int)desired["imageQualityBrightnessMin"] : configuration.GetValue("ImageQuality:BrightnessMin", 50);
                imageQualityOptions.BrightnessMax = desired.Contains("imageQualityBrightnessMax") ? (int)desired["imageQualityBrightnessMax"] : configuration.GetValue("ImageQuality:BrightnessMax", 200);
                imageQualityOptions.ContrastMin = desired.Contains("imageQualityContrastMin") ? (double)desired["imageQualityContrastMin"] : configuration.GetValue("ImageQuality:ContrastMin", 0.3);
                imageQualityOptions.NoiseMax = desired.Contains("imageQualityNoiseMax") ? (double)desired["imageQualityNoiseMax"] : configuration.GetValue("ImageQuality:NoiseMax", 0.5);

                logger.LogInformation("Image quality configuration loaded from device twin: MinThreshold={MinThreshold}, Brightness={BrightnessMin}-{BrightnessMax}, Contrast={ContrastMin}, Noise={NoiseMax}",
                    imageQualityOptions.MinThreshold, imageQualityOptions.BrightnessMin, imageQualityOptions.BrightnessMax, imageQualityOptions.ContrastMin, imageQualityOptions.NoiseMax);

                return imageQualityOptions;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch image quality configuration from device twin, using defaults");
                return new ImageQualityOptions();
            }
        }

        /// <summary>
        /// Fetch and apply alert configuration from device twin
        /// </summary>
        public async Task<AlertOptions> FetchAndApplyAlertConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger)
        {
            try
            {
                Twin twin = await deviceClient.GetTwinAsync();
                TwinCollection desired = twin.Properties.Desired;

                var alertOptions = new AlertOptions();

                // Load alert settings from device twin with fallbacks
                alertOptions.DryCountThreshold = desired.Contains("alertDryCountThreshold") ? (int)desired["alertDryCountThreshold"] : configuration.GetValue("Alert:DryCountThreshold", 3);
                alertOptions.RcycCountThreshold = desired.Contains("alertRcycCountThreshold") ? (int)desired["alertRcycCountThreshold"] : configuration.GetValue("Alert:RcycCountThreshold", 2);
                alertOptions.MaxRetryAttempts = desired.Contains("alertMaxRetryAttempts") ? (int)desired["alertMaxRetryAttempts"] : configuration.GetValue("Alert:MaxRetryAttempts", 5);
                alertOptions.CooldownMinutes = desired.Contains("alertCooldownMinutes") ? (int)desired["alertCooldownMinutes"] : configuration.GetValue("Alert:CooldownMinutes", 15);

                logger.LogInformation("Alert configuration loaded from device twin: DryCount={DryCount}, RcycCount={RcycCount}, MaxRetries={MaxRetries}, Cooldown={Cooldown}m",
                    alertOptions.DryCountThreshold, alertOptions.RcycCountThreshold, alertOptions.MaxRetryAttempts, alertOptions.CooldownMinutes);

                return alertOptions;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch alert configuration from device twin, using defaults");
                return new AlertOptions();
            }
        }

        /// <summary>
        /// Fetch and apply debug configuration from device twin
        /// </summary>
        public async Task<DebugOptions> FetchAndApplyDebugConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger)
        {
            try
            {
                Twin twin = await deviceClient.GetTwinAsync();
                TwinCollection desired = twin.Properties.Desired;

                var debugOptions = new DebugOptions();

                // Load debug settings from device twin with fallbacks
                debugOptions.DebugMode = desired.Contains("debugMode") ? (bool)desired["debugMode"] : configuration.GetValue("Debug:DebugMode", false);
                debugOptions.ImageSaveEnabled = desired.Contains("debugImageSaveEnabled") ? (bool)desired["debugImageSaveEnabled"] : configuration.GetValue("Debug:ImageSaveEnabled", false);
                debugOptions.ImageRetentionDays = desired.Contains("debugImageRetentionDays") ? (int)desired["debugImageRetentionDays"] : configuration.GetValue("Debug:ImageRetentionDays", 7);
                debugOptions.LogLevel = desired.Contains("logLevel") ? (string)desired["logLevel"] : configuration.GetValue("Debug:LogLevel", "Information");
                debugOptions.EnableVerboseOcrLogging = desired.Contains("enableVerboseOcrLogging") ? (bool)desired["enableVerboseOcrLogging"] : configuration.GetValue("Debug:EnableVerboseOcrLogging", false);

                logger.LogInformation("Debug configuration loaded from device twin: DebugMode={DebugMode}, ImageSave={ImageSave}, LogLevel={LogLevel}, VerboseOCR={VerboseOCR}",
                    debugOptions.DebugMode, debugOptions.ImageSaveEnabled, debugOptions.LogLevel, debugOptions.EnableVerboseOcrLogging);

                return debugOptions;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch debug configuration from device twin, using defaults");
                return new DebugOptions();
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
        public OcrOptions? OcrOptions { get; set; }
        
        // New configuration options
        public MonitoringOptions MonitoringOptions { get; set; } = new();
        public ImageQualityOptions ImageQualityOptions { get; set; } = new();
        public AlertOptions AlertOptions { get; set; } = new();
        public DebugOptions DebugOptions { get; set; } = new();
    }
}
