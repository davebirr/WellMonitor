using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services;

/// <summary>
/// OCR service interface for extracting text from LED display images
/// Designed for high-reliability industrial monitoring applications
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extract text from an image file with confidence scoring
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>OCR result with extracted text and confidence metrics</returns>
    Task<OcrResult> ExtractTextAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text from image stream (for real-time processing)
    /// </summary>
    /// <param name="imageStream">Image data stream</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>OCR result with extracted text and confidence metrics</returns>
    Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text from image bytes (backward compatibility)
    /// </summary>
    /// <param name="imageBytes">Image data as byte array</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>OCR result with extracted text and confidence metrics</returns>
    Task<OcrResult> ExtractTextAsync(byte[] imageBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse pump reading from raw OCR text
    /// </summary>
    /// <param name="rawText">Raw text extracted from OCR</param>
    /// <returns>Parsed pump reading with status and current value</returns>
    PumpReading ParsePumpReading(string rawText);

    /// <summary>
    /// Preprocess image for better OCR accuracy
    /// </summary>
    /// <param name="inputPath">Input image path</param>
    /// <param name="outputPath">Output path for preprocessed image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if preprocessing was successful</returns>
    Task<bool> PreprocessImageAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate OCR quality and confidence
    /// </summary>
    /// <param name="ocrResult">OCR result to validate</param>
    /// <returns>True if OCR quality meets minimum standards</returns>
    bool ValidateOcrQuality(OcrResult ocrResult);

    /// <summary>
    /// Get OCR processing statistics
    /// </summary>
    /// <returns>Current OCR statistics</returns>
    OcrStatistics GetStatistics();

    /// <summary>
    /// Reset OCR processing statistics
    /// </summary>
    void ResetStatistics();

    /// <summary>
    /// Process image and return pump reading (high-level method)
    /// </summary>
    /// <param name="imageBytes">Image data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed pump reading</returns>
    Task<PumpReading> ProcessImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
}
