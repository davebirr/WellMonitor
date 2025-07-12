using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Text.RegularExpressions;
using WellMonitor.Device.Models;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Services;

/// <summary>
/// High-quality OCR service with enterprise-grade features
/// Supports both Tesseract and Azure Cognitive Services
/// Designed for industrial monitoring applications
/// </summary>
public class OcrService : IOcrService
{
    private readonly ILogger<OcrService> _logger;
    private readonly OcrOptions _options;
    private readonly IOcrProvider _primaryProvider;
    private readonly IOcrProvider? _fallbackProvider;
    private readonly OcrStatistics _statistics;
    private readonly SemaphoreSlim _semaphore;

    public OcrService(
        ILogger<OcrService> logger,
        IOptions<OcrOptions> options,
        IEnumerable<IOcrProvider> providers)
    {
        _logger = logger;
        _options = options.Value;
        _statistics = new OcrStatistics();
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

        // Select primary and fallback providers based on configuration
        var providerList = providers.ToList();
        
        // Check for available (initialized) providers
        var availableProviders = providerList.Where(p => p.IsAvailable).ToList();
        
        if (availableProviders.Count == 0)
        {
            _logger.LogWarning("No OCR providers are available - OCR service will operate in limited mode");
            // Use any provider from the list (including NullOcrProvider which should always be available)
            _primaryProvider = providerList.FirstOrDefault(p => p.Name == "Null") ?? providerList.FirstOrDefault()!;
            _fallbackProvider = null;
        }
        else
        {
            _primaryProvider = availableProviders.FirstOrDefault(p => p.Name == _options.Provider) 
                ?? availableProviders.FirstOrDefault() 
                ?? providerList.FirstOrDefault()!;

            _fallbackProvider = availableProviders.FirstOrDefault(p => p.Name != _primaryProvider.Name);
        }

        if (_primaryProvider == null)
        {
            throw new InvalidOperationException("No OCR providers available, including fallback providers");
        }

        _logger.LogInformation("OCR Service initialized with primary provider: {Provider}", _primaryProvider.Name);
        if (_fallbackProvider != null)
        {
            _logger.LogInformation("Fallback provider available: {Provider}", _fallbackProvider.Name);
        }
        else if (availableProviders.Count == 0)
        {
            _logger.LogWarning("OCR service operating in limited mode - text extraction will not be available");
        }
    }

    /// <summary>
    /// Extract text from an image file with confidence scoring
    /// </summary>
    public async Task<OcrResult> ExtractTextAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            return CreateErrorResult("Image file not found or invalid path");
        }

        try
        {
            using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            return await ExtractTextAsync(fileStream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read image file: {ImagePath}", imagePath);
            return CreateErrorResult($"Failed to read image file: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract text from image stream (for real-time processing)
    /// </summary>
    public async Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        if (imageStream == null || !imageStream.CanRead)
        {
            return CreateErrorResult("Invalid image stream");
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await ExtractTextInternalAsync(imageStream, cancellationToken);
            stopwatch.Stop();

            result.ProcessingDurationMs = stopwatch.ElapsedMilliseconds;
            UpdateStatistics(result);

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Extract text from image bytes (backward compatibility)
    /// </summary>
    public async Task<OcrResult> ExtractTextAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return CreateErrorResult("Invalid image data");
        }

        using var memoryStream = new MemoryStream(imageBytes);
        return await ExtractTextAsync(memoryStream, cancellationToken);
    }

    /// <summary>
    /// Parse pump reading from raw OCR text
    /// </summary>
    public PumpReading ParsePumpReading(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new PumpReading
            {
                Status = PumpStatus.Unknown,
                RawText = rawText,
                Confidence = 0.0,
                IsValid = false
            };
        }

        var cleanedText = CleanOcrText(rawText);
        var reading = new PumpReading
        {
            RawText = rawText,
            Timestamp = DateTime.UtcNow
        };

        // Check for specific status messages first
        if (IsStatusMessage(cleanedText, PumpStatusConstants.Dry))
        {
            reading.Status = PumpStatus.Dry;
            reading.Confidence = 0.9;
            reading.IsValid = true;
        }
        else if (IsStatusMessage(cleanedText, PumpStatusConstants.RapidCycle))
        {
            reading.Status = PumpStatus.RapidCycle;
            reading.Confidence = 0.9;
            reading.IsValid = true;
        }
        else if (IsBlankOrDark(cleanedText))
        {
            reading.Status = PumpStatus.Off;
            reading.Confidence = 0.8;
            reading.IsValid = true;
        }
        else if (TryParseCurrentValue(cleanedText, out var current, out var confidence))
        {
            reading.CurrentAmps = current;
            reading.Confidence = confidence;
            reading.IsValid = true;

            // Determine status based on current value
            if (current <= PumpStatusConstants.IdleThreshold)
            {
                reading.Status = PumpStatus.Idle;
            }
            else if (current >= PumpStatusConstants.MinimumRunningCurrent)
            {
                reading.Status = PumpStatus.Normal;
            }
            else
            {
                reading.Status = PumpStatus.Unknown;
                reading.IsValid = false;
            }
        }
        else
        {
            reading.Status = PumpStatus.Unknown;
            reading.Confidence = 0.0;
            reading.IsValid = false;
        }

        _logger.LogDebug("Parsed pump reading: Status={Status}, Current={Current}A, Confidence={Confidence}",
            reading.Status, reading.CurrentAmps, reading.Confidence);

        return reading;
    }

    /// <summary>
    /// Preprocess image for better OCR accuracy
    /// </summary>
    public async Task<bool> PreprocessImageAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        if (!_options.EnablePreprocessing)
        {
            return true;
        }

        try
        {
            using var image = await Image.LoadAsync<Rgba32>(inputPath, cancellationToken);
            
            var preprocessingSteps = new List<string>();

            image.Mutate(x =>
            {
                // Convert to grayscale
                if (_options.ImagePreprocessing.EnableGrayscale)
                {
                    x.Grayscale();
                    preprocessingSteps.Add("Grayscale");
                }

                // Scale image
                if (_options.ImagePreprocessing.EnableScaling && _options.ImagePreprocessing.ScaleFactor != 1.0)
                {
                    var newWidth = (int)(image.Width * _options.ImagePreprocessing.ScaleFactor);
                    var newHeight = (int)(image.Height * _options.ImagePreprocessing.ScaleFactor);
                    x.Resize(newWidth, newHeight);
                    preprocessingSteps.Add($"Scale ({_options.ImagePreprocessing.ScaleFactor}x)");
                }

                // Adjust brightness
                if (_options.ImagePreprocessing.EnableBrightnessAdjustment)
                {
                    x.Brightness(_options.ImagePreprocessing.BrightnessAdjustment / 100f);
                    preprocessingSteps.Add($"Brightness ({_options.ImagePreprocessing.BrightnessAdjustment})");
                }

                // Enhance contrast
                if (_options.ImagePreprocessing.EnableContrastEnhancement)
                {
                    x.Contrast((float)_options.ImagePreprocessing.ContrastFactor);
                    preprocessingSteps.Add($"Contrast ({_options.ImagePreprocessing.ContrastFactor})");
                }

                // Apply binary threshold for LED displays
                if (_options.ImagePreprocessing.EnableBinaryThresholding)
                {
                    x.BinaryThreshold(_options.ImagePreprocessing.BinaryThreshold / 255f);
                    preprocessingSteps.Add($"Binary Threshold ({_options.ImagePreprocessing.BinaryThreshold})");
                }
            });

            await image.SaveAsync(outputPath, cancellationToken);
            
            _logger.LogDebug("Image preprocessing completed: {Steps}", string.Join(", ", preprocessingSteps));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image preprocessing failed for {InputPath}", inputPath);
            return false;
        }
    }

    /// <summary>
    /// Validate OCR quality and confidence
    /// </summary>
    public bool ValidateOcrQuality(OcrResult ocrResult)
    {
        if (ocrResult == null || !ocrResult.Success)
        {
            return false;
        }

        // Check minimum confidence threshold
        if (ocrResult.Confidence < _options.MinimumConfidence)
        {
            _logger.LogWarning("OCR confidence {Confidence} below minimum threshold {MinThreshold}",
                ocrResult.Confidence, _options.MinimumConfidence);
            return false;
        }

        // Check if text is reasonable for pump display
        if (string.IsNullOrWhiteSpace(ocrResult.ProcessedText))
        {
            return false;
        }

        // Additional quality checks
        if (ocrResult.QualityMetrics != null)
        {
            return ocrResult.QualityMetrics.MeetsMinimumStandards;
        }

        return true;
    }

    /// <summary>
    /// Get OCR processing statistics
    /// </summary>
    public OcrStatistics GetStatistics()
    {
        return _statistics;
    }

    /// <summary>
    /// Reset OCR processing statistics
    /// </summary>
    public void ResetStatistics()
    {
        _statistics.TotalOperations = 0;
        _statistics.SuccessfulOperations = 0;
        _statistics.FailedOperations = 0;
        _statistics.AverageProcessingTimeMs = 0;
        _statistics.AverageConfidence = 0;
        _statistics.LastResetAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Process image and return pump reading (high-level method)
    /// </summary>
    public async Task<PumpReading> ProcessImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        var ocrResult = await ExtractTextAsync(imageBytes, cancellationToken);
        
        if (!ocrResult.Success)
        {
            _logger.LogWarning("OCR processing failed: {Error}", ocrResult.ErrorMessage);
            return new PumpReading
            {
                Status = PumpStatus.Unknown,
                RawText = ocrResult.RawText,
                Confidence = 0.0,
                IsValid = false
            };
        }

        var pumpReading = ParsePumpReading(ocrResult.ProcessedText);
        
        // Add OCR metadata to pump reading
        pumpReading.Metadata["OcrConfidence"] = ocrResult.Confidence;
        pumpReading.Metadata["OcrProvider"] = ocrResult.Provider;
        pumpReading.Metadata["ProcessingDurationMs"] = ocrResult.ProcessingDurationMs;

        return pumpReading;
    }

    #region Private Methods

    /// <summary>
    /// Internal text extraction with retry logic
    /// </summary>
    private async Task<OcrResult> ExtractTextInternalAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        OcrResult? result = null;
        Exception? lastException = null;

        // Try primary provider first
        for (int attempt = 0; attempt < _options.MaxRetryAttempts; attempt++)
        {
            try
            {
                imageStream.Seek(0, SeekOrigin.Begin);
                result = await _primaryProvider.ExtractTextAsync(imageStream, cancellationToken);
                
                if (result.Success && ValidateOcrQuality(result))
                {
                    result.RetryAttempts = attempt;
                    return result;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "OCR attempt {Attempt} failed with primary provider {Provider}",
                    attempt + 1, _primaryProvider.Name);
            }

            if (attempt < _options.MaxRetryAttempts - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
        }

        // Try fallback provider if available
        if (_fallbackProvider != null)
        {
            try
            {
                imageStream.Seek(0, SeekOrigin.Begin);
                result = await _fallbackProvider.ExtractTextAsync(imageStream, cancellationToken);
                
                if (result.Success && ValidateOcrQuality(result))
                {
                    result.RetryAttempts = _options.MaxRetryAttempts;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallback OCR provider {Provider} also failed", _fallbackProvider.Name);
            }
        }

        return CreateErrorResult($"OCR failed after {_options.MaxRetryAttempts} attempts: {lastException?.Message}");
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
            Confidence = 0.0,
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Clean OCR text for parsing
    /// </summary>
    private string CleanOcrText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove common OCR artifacts and normalize
        return Regex.Replace(text, @"[^\w\s\.\-]", "")
            .Replace("O", "0")  // Common OCR mistake
            .Replace("l", "1")  // Common OCR mistake
            .Replace("S", "5")  // Common OCR mistake
            .Trim()
            .ToUpperInvariant();
    }

    /// <summary>
    /// Check if text represents a status message
    /// </summary>
    private bool IsStatusMessage(string text, string expectedMessage)
    {
        return text.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase) ||
               CalculateLevenshteinDistance(text, expectedMessage) <= 2;
    }

    /// <summary>
    /// Check if text represents a blank or dark display
    /// </summary>
    private bool IsBlankOrDark(string text)
    {
        return string.IsNullOrWhiteSpace(text) || text.Length < 2;
    }

    /// <summary>
    /// Try to parse current value from text
    /// </summary>
    private bool TryParseCurrentValue(string text, out double current, out double confidence)
    {
        current = 0;
        confidence = 0;

        // Regular expression to find decimal numbers
        var match = Regex.Match(text, @"(\d+\.?\d*)", RegexOptions.IgnoreCase);
        
        if (match.Success && double.TryParse(match.Value, out current))
        {
            // Calculate confidence based on text quality
            confidence = CalculateParsingConfidence(text, match.Value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculate parsing confidence based on text quality
    /// </summary>
    private double CalculateParsingConfidence(string originalText, string parsedValue)
    {
        if (string.IsNullOrEmpty(originalText) || string.IsNullOrEmpty(parsedValue))
            return 0.0;

        // Base confidence on how much of the text was successfully parsed
        var baseConfidence = (double)parsedValue.Length / originalText.Length;
        
        // Adjust based on expected patterns
        if (Regex.IsMatch(originalText, @"^\d+\.?\d*$"))
        {
            baseConfidence += 0.2; // Clean decimal number
        }

        return Math.Min(1.0, baseConfidence);
    }

    /// <summary>
    /// Calculate Levenshtein distance between two strings
    /// </summary>
    private int CalculateLevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    /// <summary>
    /// Update processing statistics
    /// </summary>
    private void UpdateStatistics(OcrResult result)
    {
        _statistics.TotalOperations++;
        
        if (result.Success)
        {
            _statistics.SuccessfulOperations++;
            
            // Update rolling average
            var totalProcessingTime = _statistics.AverageProcessingTimeMs * (_statistics.SuccessfulOperations - 1);
            _statistics.AverageProcessingTimeMs = (totalProcessingTime + result.ProcessingDurationMs) / _statistics.SuccessfulOperations;
            
            var totalConfidence = _statistics.AverageConfidence * (_statistics.SuccessfulOperations - 1);
            _statistics.AverageConfidence = (totalConfidence + result.Confidence) / _statistics.SuccessfulOperations;
        }
        else
        {
            _statistics.FailedOperations++;
        }
    }

    #endregion
}
