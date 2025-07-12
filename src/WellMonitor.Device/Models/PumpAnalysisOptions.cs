namespace WellMonitor.Device.Models;

/// <summary>
/// Configuration options for pump current thresholds and analysis
/// </summary>
public class PumpAnalysisOptions
{
    /// <summary>
    /// Current threshold below which pump is considered OFF (default: 0.1A)
    /// </summary>
    public double OffCurrentThreshold { get; set; } = 0.1;

    /// <summary>
    /// Current threshold below which pump is considered IDLE (default: 0.5A)
    /// </summary>
    public double IdleCurrentThreshold { get; set; } = 0.5;

    /// <summary>
    /// Minimum current for normal pump operation (default: 3.0A)
    /// </summary>
    public double NormalCurrentMin { get; set; } = 3.0;

    /// <summary>
    /// Maximum current for normal pump operation (default: 8.0A)
    /// </summary>
    public double NormalCurrentMax { get; set; } = 8.0;

    /// <summary>
    /// Maximum valid current reading - readings above this are rejected (default: 25.0A)
    /// </summary>
    public double MaxValidCurrent { get; set; } = 25.0;

    /// <summary>
    /// Current threshold above which pump is considered high/overload (default: 20.0A)
    /// </summary>
    public double HighCurrentThreshold { get; set; } = 20.0;
}

/// <summary>
/// Configuration options for automated power management and safety controls
/// </summary>
public class PowerManagementOptions
{
    /// <summary>
    /// Enable automatic power cycling for rapid cycling conditions (default: true)
    /// </summary>
    public bool EnableAutoActions { get; set; } = true;

    /// <summary>
    /// Duration in seconds to keep power off during cycle (default: 5 seconds)
    /// </summary>
    public int PowerCycleDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Minimum interval in minutes between power cycles (default: 30 minutes)
    /// </summary>
    public int MinimumCycleIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum number of automatic power cycles per day (default: 10)
    /// </summary>
    public int MaxDailyCycles { get; set; } = 10;

    /// <summary>
    /// Enable power cycling for dry conditions (default: false - safety feature)
    /// </summary>
    public bool EnableDryConditionCycling { get; set; } = false;
}

/// <summary>
/// Configuration options for pump status detection from OCR text
/// </summary>
public class StatusDetectionOptions
{
    /// <summary>
    /// Keywords that indicate dry/no water condition
    /// </summary>
    public string[] DryKeywords { get; set; } = ["Dry", "No Water", "Empty", "Well Dry"];

    /// <summary>
    /// Keywords that indicate rapid cycling condition
    /// </summary>
    public string[] RapidCycleKeywords { get; set; } = ["rcyc", "Rapid Cycle", "Cycling", "Fault", "Error"];

    /// <summary>
    /// Whether status message matching is case sensitive (default: false)
    /// </summary>
    public bool StatusMessageCaseSensitive { get; set; } = false;
}
