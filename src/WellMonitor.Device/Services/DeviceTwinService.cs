using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services
{
    public interface IDeviceTwinService
    {
        Task<DeviceTwinConfig> FetchAndApplyConfigAsync(DeviceClient deviceClient, IConfiguration configuration, GpioOptions gpioOptions, ILogger logger);
    }

    public class DeviceTwinService : IDeviceTwinService
    {
        public async Task<DeviceTwinConfig> FetchAndApplyConfigAsync(DeviceClient deviceClient, IConfiguration configuration, GpioOptions gpioOptions, ILogger logger)
        {
            // Fetch device twin properties
            Twin twin = await deviceClient.GetTwinAsync();
            var desired = twin.Properties.Desired;

            // Read configuration from device twin desired properties (with fallback to config file)
            double currentThreshold = desired.Contains("currentThreshold") ? (double)desired["currentThreshold"] : configuration.GetValue("CurrentThreshold", 4.5);
            int cycleTimeThreshold = desired.Contains("cycleTimeThreshold") ? (int)desired["cycleTimeThreshold"] : configuration.GetValue("CycleTimeThreshold", 30);
            int relayDebounceMs = desired.Contains("relayDebounceMs") ? (int)desired["relayDebounceMs"] : configuration.GetValue("RelayDebounceMs", 500);
            gpioOptions.RelayDebounceMs = relayDebounceMs;
            int syncIntervalMinutes = desired.Contains("syncIntervalMinutes") ? (int)desired["syncIntervalMinutes"] : configuration.GetValue("SyncIntervalMinutes", 5);
            int logRetentionDays = desired.Contains("logRetentionDays") ? (int)desired["logRetentionDays"] : configuration.GetValue("LogRetentionDays", 14);
            string ocrMode = desired.Contains("ocrMode") ? (string)desired["ocrMode"] : configuration.GetValue("OcrMode", "tesseract");
            bool powerAppEnabled = desired.Contains("powerAppEnabled") ? (bool)desired["powerAppEnabled"] : configuration.GetValue("PowerAppEnabled", true);

            logger.LogInformation($"Loaded config: currentThreshold={currentThreshold}, cycleTimeThreshold={cycleTimeThreshold}, relayDebounceMs={relayDebounceMs}, syncIntervalMinutes={syncIntervalMinutes}, logRetentionDays={logRetentionDays}, ocrMode={ocrMode}, powerAppEnabled={powerAppEnabled}");

            return new DeviceTwinConfig
            {
                CurrentThreshold = currentThreshold,
                CycleTimeThreshold = cycleTimeThreshold,
                RelayDebounceMs = relayDebounceMs,
                SyncIntervalMinutes = syncIntervalMinutes,
                LogRetentionDays = logRetentionDays,
                OcrMode = ocrMode,
                PowerAppEnabled = powerAppEnabled
            };
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
