using Azure.AI.Vision.ImageAnalysis;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services;

/// <summary>
/// Azure Cognitive Services OCR provider implementation
/// High-accuracy cloud-based OCR using Azure Computer Vision
/// Ideal for backup/fallback OCR and cloud-connected scenarios
/// </summary>
public class AzureCognitiveServicesOcrProvider : IOcrProvider, IDisposable
{
    private readonly ILogger<AzureCognitiveServicesOcrProvider> _logger;
    private readonly AzureCognitiveServicesOptions _options;
    private ImageAnalysisClient? _client;
    private bool _disposed;

    public string Name => "AzureCognitiveServices";

    public bool IsAvailable => _client != null && !string.IsNullOrEmpty(_options.Endpoint);

    public AzureCognitiveServicesOcrProvider(
        ILogger<AzureCognitiveServicesOcrProvider> logger,
        IOptions<OcrOptions> options)
    {
        _logger = logger;
        _options = options.Value.AzureCognitiveServices;
    }

    /// <summary>
    /// Initialize Azure Cognitive Services client
    /// </summary>
    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_client != null)
        {
            return Task.FromResult(true);
        }

        try
        {
            _logger.LogInformation("Initializing Azure Cognitive Services OCR provider...");

            if (string.IsNullOrEmpty(_options.Endpoint))
            {
                _logger.LogWarning("Azure Cognitive Services endpoint not configured");
                return Task.FromResult(false);
            }

            // Use Managed Identity or DefaultAzureCredential for authentication
            var credential = new DefaultAzureCredential();
            var endpoint = new Uri(_options.Endpoint);

            _client = new ImageAnalysisClient(endpoint, credential);

            _logger.LogInformation("Azure Cognitive Services OCR provider initialized successfully");
            _logger.LogDebug("Using endpoint: {Endpoint}", _options.Endpoint);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Cognitive Services OCR provider");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Extract text from image stream using Azure Cognitive Services
    /// </summary>
    public async Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Azure Cognitive Services provider not initialized");
        }

        if (imageStream == null || !imageStream.CanRead)
        {
            return CreateErrorResult("Invalid image stream");
        }

        var stopwatch = Stopwatch.StartNew();
        var result = new OcrResult
        {
            Provider = Name,
            ProcessedAt = DateTime.UtcNow
        };

        try
        {
            // Convert stream to BinaryData
            var imageData = await BinaryData.FromStreamAsync(imageStream, cancellationToken);

            // Analyze image with Azure Computer Vision
            var analysisResult = await _client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read,
                new ImageAnalysisOptions
                {
                    Language = "en"
                },
                cancellationToken);

            // Extract text results
            if (analysisResult.Value.Read?.Blocks != null)
            {
                var textBuilder = new List<string>();
                var allConfidences = new List<double>();

                foreach (var block in analysisResult.Value.Read.Blocks)
                {
                    foreach (var line in block.Lines)
                    {
                        textBuilder.Add(line.Text);
                        
                        // Calculate confidence from word-level confidence
                        var wordConfidences = line.Words.Select(w => (double)w.Confidence).ToList();
                        if (wordConfidences.Any())
                        {
                            allConfidences.AddRange(wordConfidences);
                        }

                        // Add text region information
                        result.TextRegions.Add(new TextRegion
                        {
                            Text = line.Text,
                            Confidence = wordConfidences.Any() ? wordConfidences.Average() : 0.0,
                            BoundingBox = ConvertBoundingBox(line.BoundingPolygon)
                        });
                    }
                }

                result.RawText = string.Join(" ", textBuilder);
                result.ProcessedText = CleanAzureText(result.RawText);
                result.Confidence = allConfidences.Any() ? allConfidences.Average() : 0.0;
                result.Success = !string.IsNullOrEmpty(result.RawText);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "No text detected in image";
            }

            stopwatch.Stop();
            result.ProcessingDurationMs = stopwatch.ElapsedMilliseconds;

            if (result.Success)
            {
                _logger.LogDebug("Azure Cognitive Services OCR successful: Text='{Text}', Confidence={Confidence}",
                    result.ProcessedText, result.Confidence);
            }
            else
            {
                _logger.LogWarning("Azure Cognitive Services OCR failed to extract text");
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ProcessingDurationMs = stopwatch.ElapsedMilliseconds;
            result.Success = false;
            result.ErrorMessage = ex.Message;
            
            _logger.LogError(ex, "Azure Cognitive Services OCR processing failed");
            return result;
        }
    }

    /// <summary>
    /// Dispose Azure client resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // ImageAnalysisClient doesn't implement IDisposable in current version
            // Just set to null for garbage collection
            _client = null;
            _disposed = true;
        }
    }

    #region Private Methods

    /// <summary>
    /// Clean Azure OCR text for better parsing
    /// </summary>
    private string CleanAzureText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Azure OCR typically returns cleaner text than Tesseract
        // Apply minimal cleaning to preserve accuracy
        return text.Trim()
            .Replace("O", "0")  // Common OCR mistake for LED displays
            .Replace("l", "1")  // Common OCR mistake
            .Replace("S", "5"); // Common OCR mistake
    }

    /// <summary>
    /// Convert Azure bounding polygon to our bounding box format
    /// </summary>
    private BoundingBox ConvertBoundingBox(IReadOnlyList<ImagePoint> polygon)
    {
        if (polygon == null || polygon.Count == 0)
            return new BoundingBox();

        var minX = polygon.Min(p => p.X);
        var maxX = polygon.Max(p => p.X);
        var minY = polygon.Min(p => p.Y);
        var maxY = polygon.Max(p => p.Y);

        return new BoundingBox
        {
            X = (int)minX,
            Y = (int)minY,
            Width = (int)(maxX - minX),
            Height = (int)(maxY - minY)
        };
    }

    /// <summary>
    /// Create error result
    /// </summary>
    private OcrResult CreateErrorResult(string errorMessage)
    {
        return new OcrResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Provider = Name,
            ProcessedAt = DateTime.UtcNow
        };
    }

    #endregion
}
