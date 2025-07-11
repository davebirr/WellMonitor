namespace WellMonitor.Device.Models;

/// <summary>
/// Configuration options for image quality validation
/// </summary>
public class ImageQualityOptions
{
    /// <summary>
    /// Minimum overall image quality threshold (0.0 - 1.0)
    /// </summary>
    public double MinThreshold { get; set; } = 0.7;

    /// <summary>
    /// Minimum acceptable brightness level (0-255)
    /// </summary>
    public int BrightnessMin { get; set; } = 50;

    /// <summary>
    /// Maximum acceptable brightness level (0-255)
    /// </summary>
    public int BrightnessMax { get; set; } = 200;

    /// <summary>
    /// Minimum acceptable contrast level (0.0 - 1.0)
    /// </summary>
    public double ContrastMin { get; set; } = 0.3;

    /// <summary>
    /// Maximum acceptable noise level (0.0 - 1.0, lower is better)
    /// </summary>
    public double NoiseMax { get; set; } = 0.5;

    /// <summary>
    /// Minimum acceptable sharpness level (0.0 - 1.0)
    /// </summary>
    public double SharpnessMin { get; set; } = 0.4;
}
