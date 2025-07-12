using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Devices.Shared;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Service for validating device twin configuration values
    /// </summary>
    public class ConfigurationValidationService
    {
        // Define expected device twin properties
        private readonly HashSet<string> _expectedCameraProperties = new()
        {
            "cameraWidth", "cameraHeight", "cameraQuality", "cameraBrightness",
            "cameraContrast", "cameraSaturation", "cameraRotation", "cameraTimeoutMs",
            "cameraWarmupTimeMs", "cameraEnablePreview", "cameraDebugImagePath"
        };

        private readonly HashSet<string> _expectedWellMonitorProperties = new()
        {
            "currentThreshold", "cycleTimeThreshold", "relayDebounceMs",
            "syncIntervalMinutes", "logRetentionDays", "ocrMode", "powerAppEnabled"
        };

        // OCR configuration properties
        private readonly HashSet<string> _expectedOcrProperties = new()
        {
            "ocrProvider", "ocrMinimumConfidence", "ocrMaxRetryAttempts", "ocrTimeoutSeconds",
            "ocrEnablePreprocessing", "ocrTesseractLanguage", "ocrTesseractEngineMode",
            "ocrTesseractPageSegmentationMode", "ocrTesseractCharWhitelist",
            "ocrAzureEndpoint", "ocrAzureKey", "ocrAzureRegion", "ocrAzureUseReadApi",
            "ocrImagePreprocessing", "ocrImageScaling", "ocrImageScaleFactor",
            "ocrImageBinaryThreshold", "ocrImageBrightnessAdjustment", "ocrImageContrastFactor",
            "ocrRetrySettings"
        };

        // Debug and monitoring properties
        private readonly HashSet<string> _expectedDebugProperties = new()
        {
            "debugMode", "debugImageSaveEnabled", "debugImageRetentionDays",
            "enableVerboseOcrLogging", "logLevel"
        };

        // Monitoring and alert properties
        private readonly HashSet<string> _expectedMonitoringProperties = new()
        {
            "monitoringIntervalSeconds", "telemetryIntervalMinutes", "syncIntervalHours",
            "dataRetentionDays", "displayUnits"
        };

        // Alert configuration properties
        private readonly HashSet<string> _expectedAlertProperties = new()
        {
            "alertConfig", "alertCooldownMinutes", "alertDryCountThreshold",
            "alertRcycCountThreshold", "alertMaxRetryAttempts"
        };

        // Image quality properties
        private readonly HashSet<string> _expectedImageQualityProperties = new()
        {
            "imageQualityMinThreshold", "imageQualityBrightnessMin", "imageQualityBrightnessMax",
            "imageQualityContrastMin", "imageQualityNoiseMax"
        };

        // System properties
        private readonly HashSet<string> _expectedSystemProperties = new()
        {
            "firmwareUpdateUrl", "$version"
        };

        private readonly HashSet<string> _allExpectedProperties;

        public ConfigurationValidationService()
        {
            _allExpectedProperties = new HashSet<string>(_expectedCameraProperties);
            _allExpectedProperties.UnionWith(_expectedWellMonitorProperties);
            _allExpectedProperties.UnionWith(_expectedOcrProperties);
            _allExpectedProperties.UnionWith(_expectedDebugProperties);
            _allExpectedProperties.UnionWith(_expectedMonitoringProperties);
            _allExpectedProperties.UnionWith(_expectedAlertProperties);
            _allExpectedProperties.UnionWith(_expectedImageQualityProperties);
            _allExpectedProperties.UnionWith(_expectedSystemProperties);
        }

        /// <summary>
        /// Validates device twin desired properties for missing, unexpected, or invalid values
        /// </summary>
        public ValidationResult ValidateDeviceTwinProperties(TwinCollection desired)
        {
            var result = new ValidationResult();
            
            // Get property names from TwinCollection using Contains method
            var actualProperties = new HashSet<string>();
            foreach (var property in _allExpectedProperties)
            {
                if (desired.Contains(property))
                {
                    actualProperties.Add(property);
                }
            }
            
            // Add any other properties that might exist
            var allPropertiesInTwin = new HashSet<string>();
            try
            {
                // Use reflection to get all properties from TwinCollection
                var jsonString = desired.ToJson();
                var jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
                if (jsonObject != null)
                {
                    foreach (var key in jsonObject.Keys)
                    {
                        allPropertiesInTwin.Add(key);
                    }
                }
            }
            catch
            {
                // If we can't parse as JSON, just check the expected properties
                foreach (var property in _allExpectedProperties)
                {
                    if (desired.Contains(property))
                    {
                        allPropertiesInTwin.Add(property);
                    }
                }
            }
            
            // Check for missing properties (warnings)
            var missingProperties = _allExpectedProperties.Except(allPropertiesInTwin).ToList();
            foreach (var missing in missingProperties)
            {
                result.AddWarning($"Device twin property '{missing}' is missing - will use default value");
            }

            // Check for unexpected properties (warnings)
            var unexpectedProperties = allPropertiesInTwin.Except(_allExpectedProperties).ToList();
            foreach (var unexpected in unexpectedProperties)
            {
                result.AddWarning($"Device twin contains unexpected property '{unexpected}' - will be ignored");
            }

            // Validate individual property values
            foreach (var propertyName in allPropertiesInTwin)
            {
                if (_allExpectedProperties.Contains(propertyName) && desired.Contains(propertyName))
                {
                    ValidatePropertyValue(propertyName, desired[propertyName], result);
                }
            }

            return result;
        }

        /// <summary>
        /// Validates a specific device twin property value
        /// </summary>
        private void ValidatePropertyValue(string propertyName, object value, ValidationResult result)
        {
            try
            {
                switch (propertyName)
                {
                    // Camera properties
                    case "cameraWidth":
                        if (!IsValidWidth(Convert.ToInt32(value)))
                            result.AddError($"Camera width {value} is outside valid range (320-4096)");
                        break;
                    case "cameraHeight":
                        if (!IsValidHeight(Convert.ToInt32(value)))
                            result.AddError($"Camera height {value} is outside valid range (240-2160)");
                        break;
                    case "cameraQuality":
                        if (!IsValidQuality(Convert.ToInt32(value)))
                            result.AddError($"Camera quality {value} is outside valid range (1-100)");
                        break;
                    case "cameraBrightness":
                        if (!IsValidBrightness(Convert.ToInt32(value)))
                            result.AddError($"Camera brightness {value} is outside valid range (0-100)");
                        break;
                    case "cameraContrast":
                        if (!IsValidContrast(Convert.ToInt32(value)))
                            result.AddError($"Camera contrast {value} is outside valid range (-100 to 100)");
                        break;
                    case "cameraSaturation":
                        if (!IsValidSaturation(Convert.ToInt32(value)))
                            result.AddError($"Camera saturation {value} is outside valid range (-100 to 100)");
                        break;
                    case "cameraRotation":
                        if (!IsValidRotation(Convert.ToInt32(value)))
                            result.AddError($"Camera rotation {value} is not a valid value (0, 90, 180, 270)");
                        break;
                    case "cameraTimeoutMs":
                        if (!IsValidTimeout(Convert.ToInt32(value)))
                            result.AddError($"Camera timeout {value}ms is outside valid range (1000-30000)");
                        break;
                    case "cameraWarmupTimeMs":
                        if (!IsValidWarmupTime(Convert.ToInt32(value)))
                            result.AddError($"Camera warmup time {value}ms is outside valid range (500-8000)");
                        break;
                    case "cameraEnablePreview":
                        // Boolean validation - just ensure it's convertible
                        Convert.ToBoolean(value);
                        break;
                    case "cameraDebugImagePath":
                        if (value != null && !IsValidDebugPath(value.ToString()!))
                            result.AddError($"Camera debug path '{value}' is not a valid path");
                        break;
                    
                    // Well monitor properties
                    case "currentThreshold":
                        if (!IsValidCurrentThreshold(Convert.ToDouble(value)))
                            result.AddError($"Current threshold {value} is outside valid range (0.1-25.0)");
                        break;
                    case "cycleTimeThreshold":
                        if (!IsValidCycleTimeThreshold(Convert.ToInt32(value)))
                            result.AddError($"Cycle time threshold {value} is outside valid range (10-120)");
                        break;
                    case "relayDebounceMs":
                        if (!IsValidRelayDebounce(Convert.ToInt32(value)))
                            result.AddError($"Relay debounce {value}ms is outside valid range (100-2000)");
                        break;
                    case "syncIntervalMinutes":
                        if (!IsValidSyncInterval(Convert.ToInt32(value)))
                            result.AddError($"Sync interval {value} is outside valid range (1-60)");
                        break;
                    case "logRetentionDays":
                        if (!IsValidLogRetention(Convert.ToInt32(value)))
                            result.AddError($"Log retention {value} is outside valid range (3-90)");
                        break;
                    case "ocrMode":
                        if (!IsValidOcrMode(value?.ToString() ?? ""))
                            result.AddError($"OCR mode '{value}' is not a valid option (tesseract, azure, offline)");
                        break;
                    case "powerAppEnabled":
                        // Boolean validation - just ensure it's convertible
                        Convert.ToBoolean(value);
                        break;
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Device twin property '{propertyName}' has invalid value '{value}': {ex.Message}");
            }
        }

        /// <summary>
        /// Validates camera configuration and returns validation results
        /// </summary>
        public ValidationResult ValidateCameraConfiguration(CameraOptions cameraOptions)
        {
            var result = new ValidationResult();

            // Validate camera width
            if (cameraOptions.Width < 320 || cameraOptions.Width > 4096)
            {
                result.AddError($"Camera width {cameraOptions.Width} is outside valid range (320-4096)");
            }

            // Validate camera height
            if (cameraOptions.Height < 240 || cameraOptions.Height > 2160)
            {
                result.AddError($"Camera height {cameraOptions.Height} is outside valid range (240-2160)");
            }

            // Validate quality
            if (cameraOptions.Quality < 1 || cameraOptions.Quality > 100)
            {
                result.AddError($"Camera quality {cameraOptions.Quality} is outside valid range (1-100)");
            }

            // Validate brightness
            if (cameraOptions.Brightness < 0 || cameraOptions.Brightness > 100)
            {
                result.AddError($"Camera brightness {cameraOptions.Brightness} is outside valid range (0-100)");
            }

            // Validate contrast
            if (cameraOptions.Contrast < -100 || cameraOptions.Contrast > 100)
            {
                result.AddError($"Camera contrast {cameraOptions.Contrast} is outside valid range (-100 to 100)");
            }

            // Validate saturation
            if (cameraOptions.Saturation < -100 || cameraOptions.Saturation > 100)
            {
                result.AddError($"Camera saturation {cameraOptions.Saturation} is outside valid range (-100 to 100)");
            }

            // Validate rotation
            if (cameraOptions.Rotation != 0 && cameraOptions.Rotation != 90 && 
                cameraOptions.Rotation != 180 && cameraOptions.Rotation != 270)
            {
                result.AddError($"Camera rotation {cameraOptions.Rotation} is not a valid value (0, 90, 180, 270)");
            }

            // Validate timeout
            if (cameraOptions.TimeoutMs < 1000 || cameraOptions.TimeoutMs > 30000)
            {
                result.AddError($"Camera timeout {cameraOptions.TimeoutMs}ms is outside valid range (1000-30000)");
            }

            // Validate warmup time
            if (cameraOptions.WarmupTimeMs < 500 || cameraOptions.WarmupTimeMs > 8000)
            {
                result.AddError($"Camera warmup time {cameraOptions.WarmupTimeMs}ms is outside valid range (500-8000)");
            }

            // Validate debug path if specified
            if (!string.IsNullOrEmpty(cameraOptions.DebugImagePath))
            {
                if (string.IsNullOrWhiteSpace(cameraOptions.DebugImagePath) || cameraOptions.DebugImagePath.Length > 260)
                {
                    result.AddError($"Camera debug path '{cameraOptions.DebugImagePath}' is not a valid path");
                }
            }

            return result;
        }

        /// <summary>
        /// Validates well monitor configuration and returns validation results
        /// </summary>
        public ValidationResult ValidateWellMonitorConfiguration(DeviceTwinConfig config)
        {
            var result = new ValidationResult();

            // Validate current threshold
            if (config.CurrentThreshold < 0.1 || config.CurrentThreshold > 25.0)
            {
                result.AddError($"Current threshold {config.CurrentThreshold} is outside valid range (0.1-25.0)");
            }

            // Validate cycle time threshold
            if (config.CycleTimeThreshold < 10 || config.CycleTimeThreshold > 120)
            {
                result.AddError($"Cycle time threshold {config.CycleTimeThreshold} is outside valid range (10-120)");
            }

            // Validate relay debounce
            if (config.RelayDebounceMs < 100 || config.RelayDebounceMs > 2000)
            {
                result.AddError($"Relay debounce {config.RelayDebounceMs}ms is outside valid range (100-2000)");
            }

            // Validate sync interval
            if (config.SyncIntervalMinutes < 1 || config.SyncIntervalMinutes > 60)
            {
                result.AddError($"Sync interval {config.SyncIntervalMinutes} is outside valid range (1-60)");
            }

            // Validate log retention
            if (config.LogRetentionDays < 3 || config.LogRetentionDays > 90)
            {
                result.AddError($"Log retention {config.LogRetentionDays} is outside valid range (3-90)");
            }

            // Validate OCR mode
            if (config.OcrMode != "tesseract" && config.OcrMode != "azure" && config.OcrMode != "offline")
            {
                result.AddError($"OCR mode '{config.OcrMode}' is not a valid option (tesseract, azure, offline)");
            }

            return result;
        }

        /// <summary>
        /// Applies safe fallback values for invalid camera configuration
        /// </summary>
        public void ApplyCameraFallbacks(CameraOptions cameraOptions)
        {
            // Apply safe fallback values for invalid settings
            if (cameraOptions.Width < 320 || cameraOptions.Width > 4096)
            {
                cameraOptions.Width = 1920; // Default safe value
            }

            if (cameraOptions.Height < 240 || cameraOptions.Height > 2160)
            {
                cameraOptions.Height = 1080; // Default safe value
            }

            if (cameraOptions.Quality < 1 || cameraOptions.Quality > 100)
            {
                cameraOptions.Quality = 95; // Default safe value
            }

            if (cameraOptions.Brightness < 0 || cameraOptions.Brightness > 100)
            {
                cameraOptions.Brightness = 50; // Default safe value
            }

            if (cameraOptions.Contrast < -100 || cameraOptions.Contrast > 100)
            {
                cameraOptions.Contrast = 0; // Default safe value
            }

            if (cameraOptions.Saturation < -100 || cameraOptions.Saturation > 100)
            {
                cameraOptions.Saturation = 0; // Default safe value
            }

            if (cameraOptions.Rotation != 0 && cameraOptions.Rotation != 90 && 
                cameraOptions.Rotation != 180 && cameraOptions.Rotation != 270)
            {
                cameraOptions.Rotation = 0; // Default safe value
            }

            if (cameraOptions.TimeoutMs < 1000 || cameraOptions.TimeoutMs > 30000)
            {
                cameraOptions.TimeoutMs = 5000; // Default safe value
            }

            if (cameraOptions.WarmupTimeMs < 500 || cameraOptions.WarmupTimeMs > 8000)
            {
                cameraOptions.WarmupTimeMs = 2000; // Default safe value
            }
        }

        /// <summary>
        /// Applies safe fallback values for invalid well monitor configuration
        /// </summary>
        public DeviceTwinConfig ApplyWellMonitorFallbacks(DeviceTwinConfig config)
        {
            // Apply safe fallback values for invalid settings
            if (config.CurrentThreshold < 0.1 || config.CurrentThreshold > 25.0)
            {
                config.CurrentThreshold = 4.5; // Default safe value
            }

            if (config.CycleTimeThreshold < 10 || config.CycleTimeThreshold > 120)
            {
                config.CycleTimeThreshold = 30; // Default safe value
            }

            if (config.RelayDebounceMs < 100 || config.RelayDebounceMs > 2000)
            {
                config.RelayDebounceMs = 500; // Default safe value
            }

            if (config.SyncIntervalMinutes < 1 || config.SyncIntervalMinutes > 60)
            {
                config.SyncIntervalMinutes = 5; // Default safe value
            }

            if (config.LogRetentionDays < 3 || config.LogRetentionDays > 90)
            {
                config.LogRetentionDays = 14; // Default safe value
            }

            if (config.OcrMode != "tesseract" && config.OcrMode != "azure" && config.OcrMode != "offline")
            {
                config.OcrMode = "tesseract"; // Default safe value
            }

            return config;
        }

        // Validation helper methods
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
        private bool IsValidCurrentThreshold(double threshold) => threshold >= 0.1 && threshold <= 25.0;
        private bool IsValidCycleTimeThreshold(int threshold) => threshold >= 10 && threshold <= 120;
        private bool IsValidRelayDebounce(int debounceMs) => debounceMs >= 100 && debounceMs <= 2000;
        private bool IsValidSyncInterval(int intervalMin) => intervalMin >= 1 && intervalMin <= 60;
        private bool IsValidLogRetention(int retentionDays) => retentionDays >= 3 && retentionDays <= 90;
        private bool IsValidOcrMode(string mode) => mode == "tesseract" || mode == "azure" || mode == "offline";
    }

    /// <summary>
    /// Validation result containing any errors and warnings found during configuration validation
    /// </summary>
    public class ValidationResult
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public bool IsValid => _errors.Count == 0;
        public bool HasWarnings => _warnings.Count > 0;
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();
        public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        public void AddError(string error)
        {
            _errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }

        public string GetErrorSummary()
        {
            if (IsValid && !HasWarnings)
                return "Configuration is valid";

            var summary = "";
            if (!IsValid)
            {
                summary += $"Configuration has {_errors.Count} error(s):\n" + string.Join("\n", _errors);
            }
            
            if (HasWarnings)
            {
                if (!string.IsNullOrEmpty(summary))
                    summary += "\n\n";
                summary += $"Configuration has {_warnings.Count} warning(s):\n" + string.Join("\n", _warnings);
            }

            return summary;
        }
    }
}
