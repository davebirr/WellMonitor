using Microsoft.Extensions.Logging;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services;

/// <summary>
/// Null OCR provider for cases where no real OCR providers are available
/// Returns empty results but allows the application to continue functioning
/// </summary>
public class NullOcrProvider : IOcrProvider, IDisposable
{
    private readonly ILogger<NullOcrProvider> _logger;

    public string Name => "Null";

    public bool IsAvailable => true; // Always available as a fallback

    public NullOcrProvider(ILogger<NullOcrProvider> logger)
    {
        _logger = logger;
    }

    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Null OCR provider initialized - no text extraction will be performed");
        return Task.FromResult(true);
    }

    public Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Null OCR provider called - returning empty result");
        
        return Task.FromResult(new OcrResult
        {
            Success = true,
            RawText = "",
            ProcessedText = "",
            Confidence = 0.0,
            Provider = Name,
            ProcessedAt = DateTime.UtcNow,
            ProcessingDurationMs = 0,
            ErrorMessage = null,
            Characters = new List<CharacterResult>(),
            TextRegions = new List<TextRegion>(),
            PreprocessingSteps = new List<string> { "NullProvider" },
            PassedQualityValidation = false
        });
    }

    public void Dispose()
    {
        // Nothing to dispose for null provider
    }
}
