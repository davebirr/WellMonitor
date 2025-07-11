using System.ComponentModel.DataAnnotations;

namespace WellMonitor.Device.Models;

/// <summary>
/// Configuration options for OCR processing
/// Supports both Tesseract and Azure Cognitive Services
/// </summary>
public class OcrOptions
{
    /// <summary>
    /// OCR provider to use (Tesseract, AzureCognitiveServices, Hybrid)
    /// </summary>
    [Required]
    public string Provider { get; set; } = "Tesseract";

    /// <summary>
    /// Minimum confidence threshold for accepting OCR results (0.0 - 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public double MinimumConfidence { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of retry attempts for failed OCR operations
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Timeout for OCR operations in seconds
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable image preprocessing for better OCR accuracy
    /// </summary>
    public bool EnablePreprocessing { get; set; } = true;

    /// <summary>
    /// Tesseract-specific configuration
    /// </summary>
    public TesseractOptions Tesseract { get; set; } = new();

    /// <summary>
    /// Azure Cognitive Services configuration
    /// </summary>
    public AzureCognitiveServicesOptions AzureCognitiveServices { get; set; } = new();

    /// <summary>
    /// Image preprocessing options
    /// </summary>
    public ImagePreprocessingOptions ImagePreprocessing { get; set; } = new();
}

/// <summary>
/// Tesseract OCR configuration options
/// </summary>
public class TesseractOptions
{
    /// <summary>
    /// Tesseract language data (e.g., "eng", "eng+fra")
    /// </summary>
    public string Language { get; set; } = "eng";

    /// <summary>
    /// Tesseract engine mode (0-3)
    /// 0: Legacy engine only
    /// 1: Neural nets LSTM engine only
    /// 2: Legacy + LSTM engines
    /// 3: Default, based on what is available
    /// </summary>
    [Range(0, 3)]
    public int EngineMode { get; set; } = 3;

    /// <summary>
    /// Page segmentation mode (0-13)
    /// 6: Uniform block of text (default)
    /// 7: Single text line
    /// 8: Single word
    /// 13: Raw line. Treat the image as a single text line
    /// </summary>
    [Range(0, 13)]
    public int PageSegmentationMode { get; set; } = 7; // Single text line for LED displays

    /// <summary>
    /// Custom Tesseract configuration variables
    /// </summary>
    public Dictionary<string, string> CustomConfig { get; set; } = new()
    {
        ["tessedit_char_whitelist"] = "0123456789.DryAMPSrcyc ", // Only allow expected characters
        ["tessedit_unrej_any_wd"] = "1" // Don't reject suspect words
    };

    /// <summary>
    /// Path to Tesseract data directory (tessdata)
    /// </summary>
    public string? DataPath { get; set; }
}

/// <summary>
/// Azure Cognitive Services OCR configuration
/// </summary>
public class AzureCognitiveServicesOptions
{
    /// <summary>
    /// Azure Cognitive Services endpoint URL
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Azure region for the Cognitive Services resource
    /// </summary>
    public string Region { get; set; } = "eastus";

    /// <summary>
    /// Use Read API for better accuracy on printed text
    /// </summary>
    public bool UseReadApi { get; set; } = true;

    /// <summary>
    /// Maximum polling attempts for async Read operations
    /// </summary>
    [Range(1, 20)]
    public int MaxPollingAttempts { get; set; } = 10;

    /// <summary>
    /// Polling interval in milliseconds for Read operations
    /// </summary>
    [Range(100, 5000)]
    public int PollingIntervalMs { get; set; } = 500;
}

/// <summary>
/// Image preprocessing configuration options
/// </summary>
public class ImagePreprocessingOptions
{
    /// <summary>
    /// Enable grayscale conversion
    /// </summary>
    public bool EnableGrayscale { get; set; } = true;

    /// <summary>
    /// Enable contrast enhancement
    /// </summary>
    public bool EnableContrastEnhancement { get; set; } = true;

    /// <summary>
    /// Contrast enhancement factor (1.0 = no change)
    /// </summary>
    [Range(0.1, 5.0)]
    public double ContrastFactor { get; set; } = 1.5;

    /// <summary>
    /// Enable brightness adjustment
    /// </summary>
    public bool EnableBrightnessAdjustment { get; set; } = true;

    /// <summary>
    /// Brightness adjustment value (-100 to 100)
    /// </summary>
    [Range(-100, 100)]
    public int BrightnessAdjustment { get; set; } = 10;

    /// <summary>
    /// Enable noise reduction
    /// </summary>
    public bool EnableNoiseReduction { get; set; } = true;

    /// <summary>
    /// Enable edge enhancement for better text recognition
    /// </summary>
    public bool EnableEdgeEnhancement { get; set; } = false;

    /// <summary>
    /// Enable image scaling for better OCR accuracy
    /// </summary>
    public bool EnableScaling { get; set; } = true;

    /// <summary>
    /// Scale factor for image resizing (1.0 = no scaling)
    /// </summary>
    [Range(0.5, 4.0)]
    public double ScaleFactor { get; set; } = 2.0;

    /// <summary>
    /// Enable binary thresholding for LED displays
    /// </summary>
    public bool EnableBinaryThresholding { get; set; } = true;

    /// <summary>
    /// Binary threshold value (0-255)
    /// </summary>
    [Range(0, 255)]
    public int BinaryThreshold { get; set; } = 128;
}
