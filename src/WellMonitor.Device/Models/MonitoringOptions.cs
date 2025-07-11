namespace WellMonitor.Device.Models;

/// <summary>
/// Configuration options for monitoring intervals and behavior
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// How often to capture images and check pump status (in seconds)
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// How often to send telemetry to Azure IoT Hub (in minutes)
    /// </summary>
    public int TelemetryIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// How often to sync summary data to Azure (in hours)
    /// </summary>
    public int SyncIntervalHours { get; set; } = 1;

    /// <summary>
    /// How long to retain high-frequency readings in local database (in days)
    /// </summary>
    public int DataRetentionDays { get; set; } = 30;
}
