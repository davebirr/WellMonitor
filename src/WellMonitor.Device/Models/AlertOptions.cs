namespace WellMonitor.Device.Models;

/// <summary>
/// Configuration options for alert behavior and thresholds
/// </summary>
public class AlertOptions
{
    /// <summary>
    /// Number of consecutive 'Dry' readings before triggering an alert
    /// </summary>
    public int DryCountThreshold { get; set; } = 3;

    /// <summary>
    /// Number of 'rcyc' readings before triggering relay action
    /// </summary>
    public int RcycCountThreshold { get; set; } = 2;

    /// <summary>
    /// Maximum number of retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Cooldown period in minutes to prevent alert spam
    /// </summary>
    public int CooldownMinutes { get; set; } = 15;
}
