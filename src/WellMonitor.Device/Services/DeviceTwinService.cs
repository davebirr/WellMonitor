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
        Task<DeviceTwinConfig> FetchAndApplyConfigAsync(DeviceClient deviceClient, IConfiguration configuration, GpioOptions gpioOptions, CameraOptions cameraOptions, ILogger logger, IRuntimeConfigurationService? runtimeConfigService = null);
        Task<OcrOptions> FetchAndApplyOcrConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger, IRuntimeConfigurationService? runtimeConfigService = null);
        Task<DebugOptions> FetchAndApplyDebugConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger, IRuntimeConfigurationService? runtimeConfigService = null);
        Task<WebOptions> FetchAndApplyWebConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger, IRuntimeConfigurationService? runtimeConfigService = null);
        Task<PumpAnalysisOptions> FetchPumpAnalysisConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger);
        Task<PowerManagementOptions> FetchPowerManagementConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger);
        Task<StatusDetectionOptions> FetchStatusDetectionConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger);
        Task ReportOcrStatusAsync(DeviceClient deviceClient, IOcrService ocrService, ILogger logger);
        Task LogPeriodicConfigurationSummaryAsync(DeviceClient deviceClient, CameraOptions cameraOptions, ILogger logger);
    }

    public class DeviceTwinService : IDeviceTwinService
    {
        public async Task<DeviceTwinConfig> FetchAndApplyConfigAsync(DeviceClient deviceClient, IConfiguration configuration, GpioOptions gpioOptions, CameraOptions cameraOptions, ILogger logger, IRuntimeConfigurationService? runtimeConfigService = null)
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

            logger.LogInformation("🔧 Loading well monitor configuration from device twin...");
            
            var configFromDeviceTwin = new List<string>();
            var configFromDefaults = new List<string>();
            
            // Read configuration from device twin desired properties (with fallback to config file)
            double currentThreshold = LoadConfigValue(desired, "currentThreshold", () => configuration.GetValue("CurrentThreshold", 4.5), configFromDeviceTwin, configFromDefaults);
            int cycleTimeThreshold = LoadConfigValue(desired, "cycleTimeThreshold", () => configuration.GetValue("CycleTimeThreshold", 30), configFromDeviceTwin, configFromDefaults);
            int relayDebounceMs = LoadConfigValue(desired, "relayDebounceMs", () => configuration.GetValue("RelayDebounceMs", 500), configFromDeviceTwin, configFromDefaults);
            gpioOptions.RelayDebounceMs = relayDebounceMs;
            int syncIntervalMinutes = LoadConfigValue(desired, "syncIntervalMinutes", () => configuration.GetValue("SyncIntervalMinutes", 5), configFromDeviceTwin, configFromDefaults);
            int logRetentionDays = LoadConfigValue(desired, "logRetentionDays", () => configuration.GetValue("LogRetentionDays", 14), configFromDefaults, configFromDefaults);
            string ocrMode = LoadConfigValue(desired, "ocrMode", () => configuration.GetValue("OcrMode", "tesseract"), configFromDeviceTwin, configFromDefaults);
            bool powerAppEnabled = LoadConfigValue(desired, "powerAppEnabled", () => configuration.GetValue("PowerAppEnabled", true), configFromDeviceTwin, configFromDefaults);

            // Log configuration source summary
            if (configFromDeviceTwin.Any())
            {
                logger.LogInformation("✅ Well monitor settings loaded from device twin: {ConfigFromDeviceTwin}", 
                    string.Join(", ", configFromDeviceTwin));
            }
            
            if (configFromDefaults.Any())
            {
                logger.LogWarning("🔸 Well monitor settings using default values (not found in device twin): {ConfigFromDefaults}", 
                    string.Join(", ", configFromDefaults));
            }

            // Camera configuration from device twin (with fallback to defaults)
            await UpdateCameraOptionsFromDeviceTwin(desired, configuration, cameraOptions, logger, runtimeConfigService);

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
                logger.LogInformation("✅ Well monitor configuration validated successfully from device twin");
            }

            // Log comprehensive configuration summary
            LogWellMonitorConfiguration(config, logger);

            return config;
        }

        private async Task UpdateCameraOptionsFromDeviceTwin(TwinCollection desired, IConfiguration configuration, CameraOptions cameraOptions, ILogger logger, IRuntimeConfigurationService? runtimeConfigService = null)
        {
            logger.LogInformation("🔧 Updating camera configuration from device twin...");
            
            var settingsFromDeviceTwin = new List<string>();
            var settingsFromDefaults = new List<string>();
            
            // Check for new nested Camera structure first (preferred)
            if (desired.Contains("Camera") && desired["Camera"] is TwinCollection cameraConfig)
            {
                logger.LogInformation("📡 Found Camera configuration in device twin (nested structure)");
                UpdateCameraFromNestedConfig(cameraConfig, cameraOptions, settingsFromDeviceTwin, settingsFromDefaults, logger);
            }
            else
            {
                logger.LogWarning("⚠️ Camera configuration not found in nested structure, checking legacy flat properties");
                UpdateCameraFromLegacyConfig(desired, configuration, cameraOptions, settingsFromDeviceTwin, settingsFromDefaults, logger);
            }
            
            // Log configuration source summary
            if (settingsFromDeviceTwin.Any())
            {
                logger.LogInformation("✅ Camera settings loaded from device twin: {SettingsFromDeviceTwin}", 
                    string.Join(", ", settingsFromDeviceTwin));
            }
            
            if (settingsFromDefaults.Any())
            {
                logger.LogWarning("🔸 Camera settings using default values (not found in device twin): {SettingsFromDefaults}", 
                    string.Join(", ", settingsFromDefaults));
            }
            
            // Log final camera configuration
            LogCameraConfiguration(cameraOptions, logger);

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

            // **CRITICAL FIX**: Update runtime configuration service so IOptionsMonitor gets updated values
            if (runtimeConfigService != null)
            {
                logger.LogInformation("🔄 Updating runtime camera configuration with device twin values...");
                await runtimeConfigService.UpdateCameraOptionsAsync(cameraOptions);
                logger.LogInformation("✅ Runtime camera configuration updated successfully");
            }
            else
            {
                logger.LogWarning("⚠️ Runtime configuration service not provided - IOptionsMonitor will not be updated");
            }
        }

        /// <summary>
        /// Fetch and apply OCR configuration from device twin
        /// </summary>
        public async Task<OcrOptions> FetchAndApplyOcrConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger, IRuntimeConfigurationService? runtimeConfigService = null)
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

                // **CRITICAL FIX**: Update runtime configuration service so IOptionsMonitor gets updated values
                if (runtimeConfigService != null)
                {
                    logger.LogInformation("🔄 Updating runtime OCR configuration with device twin values...");
                    await runtimeConfigService.UpdateOcrOptionsAsync(ocrOptions);
                    logger.LogInformation("✅ Runtime OCR configuration updated successfully");
                }
                else
                {
                    logger.LogWarning("⚠️ Runtime configuration service not provided - IOptionsMonitor will not be updated");
                }

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
        public async Task<DebugOptions> FetchAndApplyDebugConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger, IRuntimeConfigurationService? runtimeConfigService = null)
        {
            try
            {
                Twin twin = await deviceClient.GetTwinAsync();
                TwinCollection desired = twin.Properties.Desired;

                var debugOptions = new DebugOptions();
                var fromDeviceTwin = new List<string>();
                var fromDefaults = new List<string>();

                // Load debug settings from device twin with fallbacks
                if (desired.Contains("debugMode"))
                {
                    debugOptions.DebugMode = (bool)desired["debugMode"];
                    fromDeviceTwin.Add($"debugMode={debugOptions.DebugMode}");
                }
                else
                {
                    debugOptions.DebugMode = configuration.GetValue("Debug:DebugMode", false);
                    fromDefaults.Add($"debugMode={debugOptions.DebugMode}");
                }

                if (desired.Contains("debugImageSaveEnabled"))
                {
                    debugOptions.ImageSaveEnabled = (bool)desired["debugImageSaveEnabled"];
                    fromDeviceTwin.Add($"debugImageSaveEnabled={debugOptions.ImageSaveEnabled}");
                }
                else
                {
                    debugOptions.ImageSaveEnabled = configuration.GetValue("Debug:ImageSaveEnabled", false);
                    fromDefaults.Add($"debugImageSaveEnabled={debugOptions.ImageSaveEnabled}");
                }

                if (desired.Contains("debugImageRetentionDays"))
                {
                    debugOptions.ImageRetentionDays = (int)desired["debugImageRetentionDays"];
                    fromDeviceTwin.Add($"debugImageRetentionDays={debugOptions.ImageRetentionDays}");
                }
                else
                {
                    debugOptions.ImageRetentionDays = configuration.GetValue("Debug:ImageRetentionDays", 7);
                    fromDefaults.Add($"debugImageRetentionDays={debugOptions.ImageRetentionDays}");
                }

                if (desired.Contains("logLevel"))
                {
                    debugOptions.LogLevel = (string)desired["logLevel"];
                    fromDeviceTwin.Add($"logLevel={debugOptions.LogLevel}");
                }
                else
                {
                    debugOptions.LogLevel = configuration.GetValue("Debug:LogLevel", "Information");
                    fromDefaults.Add($"logLevel={debugOptions.LogLevel}");
                }

                if (desired.Contains("enableVerboseOcrLogging"))
                {
                    debugOptions.EnableVerboseOcrLogging = (bool)desired["enableVerboseOcrLogging"];
                    fromDeviceTwin.Add($"enableVerboseOcrLogging={debugOptions.EnableVerboseOcrLogging}");
                }
                else
                {
                    debugOptions.EnableVerboseOcrLogging = configuration.GetValue("Debug:EnableVerboseOcrLogging", false);
                    fromDefaults.Add($"enableVerboseOcrLogging={debugOptions.EnableVerboseOcrLogging}");
                }

                logger.LogInformation("📡 Debug configuration sources:");
                if (fromDeviceTwin.Any())
                {
                    logger.LogInformation("  ✅ From Device Twin: {DeviceTwinSettings}", string.Join(", ", fromDeviceTwin));
                }
                if (fromDefaults.Any())
                {
                    logger.LogInformation("  ⚠️  From Defaults/Config: {DefaultSettings}", string.Join(", ", fromDefaults));
                }

                logger.LogInformation("🔧 Final Debug Configuration: DebugMode={DebugMode}, ImageSaveEnabled={ImageSave}, LogLevel={LogLevel}, VerboseOCR={VerboseOCR}",
                    debugOptions.DebugMode, debugOptions.ImageSaveEnabled, debugOptions.LogLevel, debugOptions.EnableVerboseOcrLogging);

                // **CRITICAL FIX**: Update runtime configuration service so IOptionsMonitor gets updated values
                if (runtimeConfigService != null)
                {
                    logger.LogInformation("🔄 Updating runtime debug configuration with device twin values...");
                    await runtimeConfigService.UpdateDebugOptionsAsync(debugOptions);
                    logger.LogInformation("✅ Runtime debug configuration updated successfully");
                }
                else
                {
                    logger.LogWarning("⚠️ Runtime configuration service not provided - IOptionsMonitor will not be updated");
                }

                return debugOptions;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch debug configuration from device twin, using defaults");
                return new DebugOptions();
            }
        }

        /// <summary>
        /// Fetch and apply web dashboard configuration from device twin
        /// </summary>
        public async Task<WebOptions> FetchAndApplyWebConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger, IRuntimeConfigurationService? runtimeConfigService = null)
        {
            try
            {
                Twin twin = await deviceClient.GetTwinAsync();
                TwinCollection desired = twin.Properties.Desired;

                var webOptions = new WebOptions();

                // Load web settings from device twin with fallbacks
                webOptions.Port = desired.Contains("webPort") ? (int)desired["webPort"] : configuration.GetValue("Web:Port", 5000);
                webOptions.AllowNetworkAccess = desired.Contains("webAllowNetworkAccess") ? (bool)desired["webAllowNetworkAccess"] : configuration.GetValue("Web:AllowNetworkAccess", false);
                webOptions.BindAddress = desired.Contains("webBindAddress") ? (string)desired["webBindAddress"] : configuration.GetValue("Web:BindAddress", "127.0.0.1");
                webOptions.EnableHttps = desired.Contains("webEnableHttps") ? (bool)desired["webEnableHttps"] : configuration.GetValue("Web:EnableHttps", false);
                webOptions.HttpsPort = desired.Contains("webHttpsPort") ? (int)desired["webHttpsPort"] : configuration.GetValue("Web:HttpsPort", 5001);
                webOptions.CorsOrigins = desired.Contains("webCorsOrigins") ? (string)desired["webCorsOrigins"] : configuration.GetValue("Web:CorsOrigins", "");
                webOptions.EnableAuthentication = desired.Contains("webEnableAuthentication") ? (bool)desired["webEnableAuthentication"] : configuration.GetValue("Web:EnableAuthentication", false);
                webOptions.AuthUsername = desired.Contains("webAuthUsername") ? (string)desired["webAuthUsername"] : configuration.GetValue("Web:AuthUsername", "admin");

                // Note: We don't load AuthPassword from device twin for security reasons

                logger.LogInformation("Web configuration loaded from device twin: Port={Port}, NetworkAccess={NetworkAccess}, BindAddress={BindAddress}, HTTPS={HTTPS}",
                    webOptions.Port, webOptions.AllowNetworkAccess, webOptions.BindAddress, webOptions.EnableHttps);

                // **CRITICAL FIX**: Update runtime configuration service so IOptionsMonitor gets updated values
                if (runtimeConfigService != null)
                {
                    logger.LogInformation("🔄 Updating runtime web configuration with device twin values...");
                    await runtimeConfigService.UpdateWebOptionsAsync(webOptions);
                    logger.LogInformation("✅ Runtime web configuration updated successfully");
                }
                else
                {
                    logger.LogWarning("⚠️ Runtime configuration service not provided - IOptionsMonitor will not be updated");
                }

                return webOptions;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch web configuration from device twin, using defaults");
                return new WebOptions();
            }
        }

        /// <summary>
        /// Fetch and apply pump analysis configuration from device twin
        /// </summary>
        public async Task<PumpAnalysisOptions> FetchPumpAnalysisConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger)
        {
            try
            {
                var twin = await deviceClient.GetTwinAsync();
                var desired = twin.Properties.Desired;
                var options = new PumpAnalysisOptions();

                if (desired.Contains("pumpCurrentThresholds"))
                {
                    var thresholds = desired["pumpCurrentThresholds"];
                    
                    if (thresholds.Contains("offCurrentThreshold"))
                        options.OffCurrentThreshold = (double)thresholds["offCurrentThreshold"];
                    
                    if (thresholds.Contains("idleCurrentThreshold"))
                        options.IdleCurrentThreshold = (double)thresholds["idleCurrentThreshold"];
                    
                    if (thresholds.Contains("normalCurrentMin"))
                        options.NormalCurrentMin = (double)thresholds["normalCurrentMin"];
                    
                    if (thresholds.Contains("normalCurrentMax"))
                        options.NormalCurrentMax = (double)thresholds["normalCurrentMax"];
                    
                    if (thresholds.Contains("maxValidCurrent"))
                        options.MaxValidCurrent = (double)thresholds["maxValidCurrent"];
                    
                    if (thresholds.Contains("highCurrentThreshold"))
                        options.HighCurrentThreshold = (double)thresholds["highCurrentThreshold"];

                    logger.LogInformation("Pump analysis configuration updated from device twin: Off={Off}A, Idle={Idle}A, Normal={Min}-{Max}A", 
                        options.OffCurrentThreshold, options.IdleCurrentThreshold, options.NormalCurrentMin, options.NormalCurrentMax);
                }
                else
                {
                    logger.LogDebug("No pump current thresholds found in device twin, using defaults");
                }

                return options;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching pump analysis configuration from device twin");
                return new PumpAnalysisOptions(); // Return defaults on error
            }
        }

        /// <summary>
        /// Fetch and apply power management configuration from device twin
        /// </summary>
        public async Task<PowerManagementOptions> FetchPowerManagementConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger)
        {
            try
            {
                var twin = await deviceClient.GetTwinAsync();
                var desired = twin.Properties.Desired;
                var options = new PowerManagementOptions();

                if (desired.Contains("powerManagement"))
                {
                    var powerMgmt = desired["powerManagement"];
                    
                    if (powerMgmt.Contains("enableAutoActions"))
                        options.EnableAutoActions = (bool)powerMgmt["enableAutoActions"];
                    
                    if (powerMgmt.Contains("powerCycleDelaySeconds"))
                        options.PowerCycleDelaySeconds = (int)powerMgmt["powerCycleDelaySeconds"];
                    
                    if (powerMgmt.Contains("minimumCycleIntervalMinutes"))
                        options.MinimumCycleIntervalMinutes = (int)powerMgmt["minimumCycleIntervalMinutes"];
                    
                    if (powerMgmt.Contains("maxDailyCycles"))
                        options.MaxDailyCycles = (int)powerMgmt["maxDailyCycles"];
                    
                    if (powerMgmt.Contains("enableDryConditionCycling"))
                        options.EnableDryConditionCycling = (bool)powerMgmt["enableDryConditionCycling"];

                    logger.LogInformation("Power management configuration updated from device twin: AutoActions={Auto}, CycleDelay={Delay}s, MinInterval={Interval}min", 
                        options.EnableAutoActions, options.PowerCycleDelaySeconds, options.MinimumCycleIntervalMinutes);
                }
                else
                {
                    logger.LogDebug("No power management configuration found in device twin, using defaults");
                }

                return options;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching power management configuration from device twin");
                return new PowerManagementOptions(); // Return defaults on error
            }
        }

        /// <summary>
        /// Fetch and apply status detection configuration from device twin
        /// </summary>
        public async Task<StatusDetectionOptions> FetchStatusDetectionConfigAsync(DeviceClient deviceClient, IConfiguration configuration, ILogger logger)
        {
            try
            {
                var twin = await deviceClient.GetTwinAsync();
                var desired = twin.Properties.Desired;
                var options = new StatusDetectionOptions();

                if (desired.Contains("statusDetection"))
                {
                    var statusDetection = desired["statusDetection"];
                    
                    if (statusDetection.Contains("dryKeywords"))
                    {
                        var dryKeywords = statusDetection["dryKeywords"] as object[];
                        if (dryKeywords != null)
                            options.DryKeywords = dryKeywords.Cast<string>().ToArray();
                    }
                    
                    if (statusDetection.Contains("rapidCycleKeywords"))
                    {
                        var rcycKeywords = statusDetection["rapidCycleKeywords"] as object[];
                        if (rcycKeywords != null)
                            options.RapidCycleKeywords = rcycKeywords.Cast<string>().ToArray();
                    }
                    
                    if (statusDetection.Contains("statusMessageCaseSensitive"))
                        options.StatusMessageCaseSensitive = (bool)statusDetection["statusMessageCaseSensitive"];

                    logger.LogInformation("Status detection configuration updated from device twin: DryKeywords={DryCount}, RcycKeywords={RcycCount}, CaseSensitive={CaseSensitive}", 
                        options.DryKeywords.Length, options.RapidCycleKeywords.Length, options.StatusMessageCaseSensitive);
                }
                else
                {
                    logger.LogDebug("No status detection configuration found in device twin, using defaults");
                }

                return options;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching status detection configuration from device twin");
                return new StatusDetectionOptions(); // Return defaults on error
            }
        }

        private void UpdateCameraFromNestedConfig(TwinCollection cameraConfig, CameraOptions cameraOptions, 
            List<string> settingsFromDeviceTwin, List<string> settingsFromDefaults, ILogger logger)
        {
            // Update camera options from nested Camera configuration
            UpdateCameraSetting(cameraConfig, "Width", val => cameraOptions.Width = (int)val, 
                () => cameraOptions.Width, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "Height", val => cameraOptions.Height = (int)val, 
                () => cameraOptions.Height, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "Quality", val => cameraOptions.Quality = (int)val, 
                () => cameraOptions.Quality, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "TimeoutMs", val => cameraOptions.TimeoutMs = (int)val, 
                () => cameraOptions.TimeoutMs, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "WarmupTimeMs", val => cameraOptions.WarmupTimeMs = (int)val, 
                () => cameraOptions.WarmupTimeMs, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "Rotation", val => cameraOptions.Rotation = (int)val, 
                () => cameraOptions.Rotation, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "Brightness", val => cameraOptions.Brightness = (int)val, 
                () => cameraOptions.Brightness, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "Contrast", val => cameraOptions.Contrast = (int)val, 
                () => cameraOptions.Contrast, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "Saturation", val => cameraOptions.Saturation = (int)val, 
                () => cameraOptions.Saturation, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "Gain", val => cameraOptions.Gain = (double)val, 
                () => cameraOptions.Gain, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "ShutterSpeedMicroseconds", val => cameraOptions.ShutterSpeedMicroseconds = (int)val, 
                () => cameraOptions.ShutterSpeedMicroseconds, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "AutoExposure", val => cameraOptions.AutoExposure = (bool)val, 
                () => cameraOptions.AutoExposure, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "AutoWhiteBalance", val => cameraOptions.AutoWhiteBalance = (bool)val, 
                () => cameraOptions.AutoWhiteBalance, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "EnablePreview", val => cameraOptions.EnablePreview = (bool)val, 
                () => cameraOptions.EnablePreview, settingsFromDeviceTwin, settingsFromDefaults);
            
            UpdateCameraSetting(cameraConfig, "DebugImagePath", val => cameraOptions.DebugImagePath = (string)val, 
                () => cameraOptions.DebugImagePath ?? "not set", settingsFromDeviceTwin, settingsFromDefaults);
        }
        
        private void UpdateCameraFromLegacyConfig(TwinCollection desired, IConfiguration configuration, 
            CameraOptions cameraOptions, List<string> settingsFromDeviceTwin, List<string> settingsFromDefaults, ILogger logger)
        {
            // Handle legacy flat camera properties for backward compatibility
            logger.LogInformation("🔄 Processing legacy camera configuration properties");
            
            if (desired.Contains("cameraWidth"))
            {
                cameraOptions.Width = (int)desired["cameraWidth"];
                settingsFromDeviceTwin.Add($"Width={cameraOptions.Width}");
            }
            else
            {
                settingsFromDefaults.Add($"Width={cameraOptions.Width} (default)");
            }
            
            if (desired.Contains("cameraHeight"))
            {
                cameraOptions.Height = (int)desired["cameraHeight"];
                settingsFromDeviceTwin.Add($"Height={cameraOptions.Height}");
            }
            else
            {
                settingsFromDefaults.Add($"Height={cameraOptions.Height} (default)");
            }
            
            if (desired.Contains("cameraQuality"))
            {
                cameraOptions.Quality = (int)desired["cameraQuality"];
                settingsFromDeviceTwin.Add($"Quality={cameraOptions.Quality}");
            }
            else
            {
                settingsFromDefaults.Add($"Quality={cameraOptions.Quality} (default)");
            }
            
            if (desired.Contains("cameraTimeoutMs"))
            {
                cameraOptions.TimeoutMs = (int)desired["cameraTimeoutMs"];
                settingsFromDeviceTwin.Add($"TimeoutMs={cameraOptions.TimeoutMs}");
            }
            else
            {
                settingsFromDefaults.Add($"TimeoutMs={cameraOptions.TimeoutMs} (default)");
            }
            
            if (desired.Contains("cameraWarmupTimeMs"))
            {
                cameraOptions.WarmupTimeMs = (int)desired["cameraWarmupTimeMs"];
                settingsFromDeviceTwin.Add($"WarmupTimeMs={cameraOptions.WarmupTimeMs}");
            }
            else
            {
                settingsFromDefaults.Add($"WarmupTimeMs={cameraOptions.WarmupTimeMs} (default)");
            }
            
            if (desired.Contains("cameraRotation"))
            {
                cameraOptions.Rotation = (int)desired["cameraRotation"];
                settingsFromDeviceTwin.Add($"Rotation={cameraOptions.Rotation}");
            }
            else
            {
                settingsFromDefaults.Add($"Rotation={cameraOptions.Rotation} (default)");
            }
            
            if (desired.Contains("cameraBrightness"))
            {
                cameraOptions.Brightness = (int)desired["cameraBrightness"];
                settingsFromDeviceTwin.Add($"Brightness={cameraOptions.Brightness}");
            }
            else
            {
                settingsFromDefaults.Add($"Brightness={cameraOptions.Brightness} (default)");
            }
            
            if (desired.Contains("cameraContrast"))
            {
                cameraOptions.Contrast = (int)desired["cameraContrast"];
                settingsFromDeviceTwin.Add($"Contrast={cameraOptions.Contrast}");
            }
            else
            {
                settingsFromDefaults.Add($"Contrast={cameraOptions.Contrast} (default)");
            }
            
            if (desired.Contains("cameraSaturation"))
            {
                cameraOptions.Saturation = (int)desired["cameraSaturation"];
                settingsFromDeviceTwin.Add($"Saturation={cameraOptions.Saturation}");
            }
            else
            {
                settingsFromDefaults.Add($"Saturation={cameraOptions.Saturation} (default)");
            }
            
            if (desired.Contains("cameraGain"))
            {
                cameraOptions.Gain = (double)desired["cameraGain"];
                settingsFromDeviceTwin.Add($"Gain={cameraOptions.Gain}");
            }
            else
            {
                settingsFromDefaults.Add($"Gain={cameraOptions.Gain} (default)");
            }
            
            if (desired.Contains("cameraShutterSpeedMicroseconds"))
            {
                cameraOptions.ShutterSpeedMicroseconds = (int)desired["cameraShutterSpeedMicroseconds"];
                settingsFromDeviceTwin.Add($"ShutterSpeedMicroseconds={cameraOptions.ShutterSpeedMicroseconds}");
            }
            else
            {
                settingsFromDefaults.Add($"ShutterSpeedMicroseconds={cameraOptions.ShutterSpeedMicroseconds} (default)");
            }
            
            if (desired.Contains("cameraAutoExposure"))
            {
                cameraOptions.AutoExposure = (bool)desired["cameraAutoExposure"];
                settingsFromDeviceTwin.Add($"AutoExposure={cameraOptions.AutoExposure}");
            }
            else
            {
                settingsFromDefaults.Add($"AutoExposure={cameraOptions.AutoExposure} (default)");
            }
            
            if (desired.Contains("cameraAutoWhiteBalance"))
            {
                cameraOptions.AutoWhiteBalance = (bool)desired["cameraAutoWhiteBalance"];
                settingsFromDeviceTwin.Add($"AutoWhiteBalance={cameraOptions.AutoWhiteBalance}");
            }
            else
            {
                settingsFromDefaults.Add($"AutoWhiteBalance={cameraOptions.AutoWhiteBalance} (default)");
            }
            
            if (desired.Contains("cameraEnablePreview"))
            {
                cameraOptions.EnablePreview = (bool)desired["cameraEnablePreview"];
                settingsFromDeviceTwin.Add($"EnablePreview={cameraOptions.EnablePreview}");
            }
            else
            {
                settingsFromDefaults.Add($"EnablePreview={cameraOptions.EnablePreview} (default)");
            }
            
            if (desired.Contains("cameraDebugImagePath"))
            {
                cameraOptions.DebugImagePath = (string)desired["cameraDebugImagePath"];
                settingsFromDeviceTwin.Add($"DebugImagePath={cameraOptions.DebugImagePath}");
            }
            else
            {
                settingsFromDefaults.Add($"DebugImagePath={cameraOptions.DebugImagePath ?? "not set"} (default)");
            }
        }
        
        private void UpdateCameraSetting<T>(TwinCollection config, string propertyName, Action<object> setter, 
            Func<T> getter, List<string> settingsFromDeviceTwin, List<string> settingsFromDefaults)
        {
            if (config.Contains(propertyName))
            {
                setter(config[propertyName]);
                settingsFromDeviceTwin.Add($"{propertyName}={getter()}");
            }
            else
            {
                settingsFromDefaults.Add($"{propertyName}={getter()} (default)");
            }
        }
        
        private void LogCameraConfiguration(CameraOptions cameraOptions, ILogger logger)
        {
            logger.LogInformation("📸 Final Camera Configuration:");
            logger.LogInformation("   Image: {Width}x{Height}, Quality: {Quality}%, Rotation: {Rotation}°",
                cameraOptions.Width, cameraOptions.Height, cameraOptions.Quality, cameraOptions.Rotation);
            logger.LogInformation("   Timing: Timeout: {TimeoutMs}ms, Warmup: {WarmupTimeMs}ms",
                cameraOptions.TimeoutMs, cameraOptions.WarmupTimeMs);
            logger.LogInformation("   Visual: Brightness: {Brightness}, Contrast: {Contrast}, Saturation: {Saturation}",
                cameraOptions.Brightness, cameraOptions.Contrast, cameraOptions.Saturation);
            logger.LogInformation("   Exposure: Gain: {Gain}, Shutter: {ShutterSpeed}μs, AutoExposure: {AutoExposure}, AutoWhiteBalance: {AutoWhiteBalance}",
                cameraOptions.Gain, cameraOptions.ShutterSpeedMicroseconds, cameraOptions.AutoExposure, cameraOptions.AutoWhiteBalance);
            logger.LogInformation("   Debug: Preview: {EnablePreview}, DebugImagePath: '{DebugImagePath}'",
                cameraOptions.EnablePreview, cameraOptions.DebugImagePath ?? "not set");
                
            // Highlight critical settings for LED environment
            if (cameraOptions.Gain > 2.0)
            {
                logger.LogWarning("⚠️ High camera gain ({Gain}) detected - may cause overexposure with bright LEDs", cameraOptions.Gain);
            }
            
            if (cameraOptions.ShutterSpeedMicroseconds > 20000)
            {
                logger.LogWarning("⚠️ Long shutter speed ({ShutterSpeed}μs) detected - may cause motion blur or overexposure", cameraOptions.ShutterSpeedMicroseconds);
            }
            
            if (cameraOptions.AutoExposure)
            {
                logger.LogWarning("⚠️ Auto-exposure enabled - may cause inconsistent exposure with LED displays");
            }
        }
        
        private T LoadConfigValue<T>(TwinCollection desired, string propertyName, Func<T> defaultValueProvider, 
            List<string> configFromDeviceTwin, List<string> configFromDefaults)
        {
            if (desired.Contains(propertyName))
            {
                var value = (T)desired[propertyName];
                configFromDeviceTwin.Add($"{propertyName}={value}");
                return value;
            }
            else
            {
                var defaultValue = defaultValueProvider();
                configFromDefaults.Add($"{propertyName}={defaultValue} (default)");
                return defaultValue;
            }
        }
        
        private void LogWellMonitorConfiguration(DeviceTwinConfig config, ILogger logger)
        {
            logger.LogInformation("⚙️ Final Well Monitor Configuration:");
            logger.LogInformation("   Pump Monitoring: CurrentThreshold: {CurrentThreshold}A, CycleTimeThreshold: {CycleTimeThreshold}s",
                config.CurrentThreshold, config.CycleTimeThreshold);
            logger.LogInformation("   System: RelayDebounce: {RelayDebounceMs}ms, SyncInterval: {SyncIntervalMinutes}min, LogRetention: {LogRetentionDays}d",
                config.RelayDebounceMs, config.SyncIntervalMinutes, config.LogRetentionDays);
            logger.LogInformation("   OCR: Mode: {OcrMode}, PowerApp: {PowerAppEnabled}",
                config.OcrMode, config.PowerAppEnabled);
                
            // Add warnings for potentially problematic values
            if (config.CurrentThreshold < 1.0 || config.CurrentThreshold > 20.0)
            {
                logger.LogWarning("⚠️ Unusual current threshold ({CurrentThreshold}A) - typical range is 1.0-20.0A", config.CurrentThreshold);
            }
            
            if (config.CycleTimeThreshold < 10 || config.CycleTimeThreshold > 300)
            {
                logger.LogWarning("⚠️ Unusual cycle time threshold ({CycleTimeThreshold}s) - typical range is 10-300s", config.CycleTimeThreshold);
            }
        }
        
        /// <summary>
        /// Log periodic configuration summary for monitoring and troubleshooting
        /// </summary>
        public async Task LogPeriodicConfigurationSummaryAsync(DeviceClient deviceClient, CameraOptions cameraOptions, ILogger logger)
        {
            try
            {
                logger.LogInformation("📊 Periodic Configuration Summary Report:");
                
                // Get current device twin
                Twin twin = await deviceClient.GetTwinAsync();
                var desired = twin.Properties.Desired;
                var reported = twin.Properties.Reported;
                
                logger.LogInformation("🔗 Device Twin Status:");
                logger.LogInformation("   Desired Properties Version: {DesiredVersion}", desired.Version);
                logger.LogInformation("   Reported Properties Version: {ReportedVersion}", reported.Version);
                logger.LogInformation("   Last Updated: {LastUpdated}", desired.GetLastUpdated());
                
                // Check for configuration drift
                var cameraFromTwin = desired.Contains("Camera") ? "nested structure" : 
                                   (HasLegacyCameraProperties(desired) ? "legacy flat properties" : "not found");
                logger.LogInformation("   Camera Configuration Source: {CameraSource}", cameraFromTwin);
                
                // Log current active camera settings
                LogCameraConfiguration(cameraOptions, logger);
                
                // Check for common misconfigurations
                if (cameraFromTwin == "not found")
                {
                    logger.LogWarning("🚨 No camera configuration found in device twin - using all default values!");
                }
                
                if (desired.Contains("Camera") && HasLegacyCameraProperties(desired))
                {
                    logger.LogWarning("🔄 Both nested Camera and legacy camera properties found - nested takes precedence");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to log periodic configuration summary");
            }
        }
        
        private bool HasLegacyCameraProperties(TwinCollection desired)
        {
            var legacyProperties = new[] { "cameraWidth", "cameraHeight", "cameraGain", "cameraShutterSpeedMicroseconds" };
            return legacyProperties.Any(prop => desired.Contains(prop));
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
