using System.Text.Json.Serialization;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Models;

/// <summary>
/// Enhanced OCR result with comprehensive metadata and confidence scoring
/// Designed for industrial monitoring applications requiring high reliability
/// </summary>
public class OcrResult
{
    /// <summary>
    /// Whether OCR processing was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Raw text extracted from the image
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Processed/cleaned text after filtering
    /// </summary>
    public string ProcessedText { get; set; } = string.Empty;

    /// <summary>
    /// Overall confidence score (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// OCR provider that generated this result
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Processing timestamp
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Processing duration in milliseconds
    /// </summary>
    public long ProcessingDurationMs { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Individual character recognition results
    /// </summary>
    public List<CharacterResult> Characters { get; set; } = new();

    /// <summary>
    /// Bounding boxes for detected text regions
    /// </summary>
    public List<TextRegion> TextRegions { get; set; } = new();

    /// <summary>
    /// Image preprocessing operations applied
    /// </summary>
    public List<string> PreprocessingSteps { get; set; } = new();

    /// <summary>
    /// Whether the image passed quality validation
    /// </summary>
    public bool PassedQualityValidation { get; set; }

    /// <summary>
    /// Quality metrics for the processed image
    /// </summary>
    public ImageQualityMetrics? QualityMetrics { get; set; }
}

/// <summary>
/// Individual character recognition result
/// </summary>
public class CharacterResult
{
    /// <summary>
    /// Recognized character
    /// </summary>
    public char Character { get; set; }

    /// <summary>
    /// Confidence score for this character (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Bounding box coordinates
    /// </summary>
    public BoundingBox BoundingBox { get; set; } = new();
}

/// <summary>
/// Text region with bounding box information
/// </summary>
public class TextRegion
{
    /// <summary>
    /// Text content in this region
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score for this region (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Bounding box coordinates
    /// </summary>
    public BoundingBox BoundingBox { get; set; } = new();
}

/// <summary>
/// Bounding box coordinates
/// </summary>
public class BoundingBox
{
    /// <summary>
    /// X coordinate of top-left corner
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y coordinate of top-left corner
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Width of the bounding box
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height of the bounding box
    /// </summary>
    public int Height { get; set; }
}

/// <summary>
/// Image quality metrics for OCR validation
/// </summary>
public class ImageQualityMetrics
{
    /// <summary>
    /// Overall image quality score (0.0 - 1.0)
    /// </summary>
    public double OverallQuality { get; set; }

    /// <summary>
    /// Image brightness level (0-255)
    /// </summary>
    public double Brightness { get; set; }

    /// <summary>
    /// Image contrast level (0.0 - 1.0)
    /// </summary>
    public double Contrast { get; set; }

    /// <summary>
    /// Image sharpness/focus quality (0.0 - 1.0)
    /// </summary>
    public double Sharpness { get; set; }

    /// <summary>
    /// Noise level in the image (0.0 - 1.0, lower is better)
    /// </summary>
    public double NoiseLevel { get; set; }

    /// <summary>
    /// Image resolution (width x height)
    /// </summary>
    public Size Resolution { get; set; } = new();

    /// <summary>
    /// Whether the image meets minimum quality standards
    /// </summary>
    public bool MeetsMinimumStandards { get; set; }
}

/// <summary>
/// Image size dimensions
/// </summary>
public class Size
{
    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; set; }
}

/// <summary>
/// Parsed pump reading with status and current value
/// </summary>
public class PumpReading
{
    /// <summary>
    /// Pump status determined from OCR
    /// </summary>
    public PumpStatus Status { get; set; } = PumpStatus.Unknown;

    /// <summary>
    /// Current draw in amperes (null if status message)
    /// </summary>
    public double? CurrentAmps { get; set; }

    /// <summary>
    /// Raw text that was parsed
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Confidence in the parsing result (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Timestamp when reading was taken
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this reading is valid for telemetry
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Additional metadata from OCR processing
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// OCR processing statistics for monitoring and diagnostics
/// </summary>
public class OcrStatistics
{
    /// <summary>
    /// Total number of OCR operations performed
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Number of successful OCR operations
    /// </summary>
    public long SuccessfulOperations { get; set; }

    /// <summary>
    /// Number of failed OCR operations
    /// </summary>
    public long FailedOperations { get; set; }

    /// <summary>
    /// Average processing time in milliseconds
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Average confidence score across all operations
    /// </summary>
    public double AverageConfidence { get; set; }

    /// <summary>
    /// Success rate (0.0 - 1.0)
    /// </summary>
    [JsonIgnore]
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations : 0.0;

    /// <summary>
    /// Last reset timestamp
    /// </summary>
    public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
}
