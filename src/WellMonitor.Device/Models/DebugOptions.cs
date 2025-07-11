namespace WellMonitor.Device.Models;

/// <summary>
/// Configuration options for debug and logging behavior
/// </summary>
public class DebugOptions
{
    /// <summary>
    /// Enable debug mode for additional logging and diagnostics
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// Enable saving debug images to disk
    /// </summary>
    public bool ImageSaveEnabled { get; set; } = false;

    /// <summary>
    /// Number of days to retain debug images
    /// </summary>
    public int ImageRetentionDays { get; set; } = 7;

    /// <summary>
    /// Minimum log level (Debug, Information, Warning, Error)
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Enable verbose OCR logging for detailed diagnostics
    /// </summary>
    public bool EnableVerboseOcrLogging { get; set; } = false;
}
