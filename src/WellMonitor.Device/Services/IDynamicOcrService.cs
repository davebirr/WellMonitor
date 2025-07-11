using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services;

/// <summary>
/// Service for managing dynamic OCR configuration changes
/// </summary>
public interface IDynamicOcrService
{
    /// <summary>
    /// Updates the OCR configuration and switches providers if needed
    /// </summary>
    /// <param name="ocrOptions">New OCR configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateConfigurationAsync(OcrOptions ocrOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current OCR service instance
    /// </summary>
    IOcrService GetCurrentOcrService();

    /// <summary>
    /// Tests if a specific OCR provider is available
    /// </summary>
    /// <param name="providerName">Name of the provider to test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> TestProviderAvailabilityAsync(string providerName, CancellationToken cancellationToken = default);
}
