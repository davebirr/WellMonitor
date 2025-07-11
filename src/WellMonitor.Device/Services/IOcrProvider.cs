using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services;

/// <summary>
/// OCR provider interface for implementing different OCR engines
/// Supports pluggable OCR providers (Tesseract, Azure Cognitive Services, etc.)
/// </summary>
public interface IOcrProvider
{
    /// <summary>
    /// Name of the OCR provider
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this provider is available and configured
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Extract text from image stream
    /// </summary>
    /// <param name="imageStream">Image data stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OCR result with extracted text</returns>
    Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize the provider with configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if initialization was successful</returns>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispose provider resources
    /// </summary>
    void Dispose();
}
