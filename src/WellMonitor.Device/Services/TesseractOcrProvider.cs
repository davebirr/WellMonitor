using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using Tesseract;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services;

/// <summary>
/// Tesseract OCR provider implementation
/// High-performance offline OCR using Tesseract engine
/// Optimized for LED display recognition
/// </summary>
public class TesseractOcrProvider : IOcrProvider, IDisposable
{
    private readonly ILogger<TesseractOcrProvider> _logger;
    private readonly TesseractOptions _options;
    private TesseractEngine? _engine;
    private bool _disposed;
    private readonly object _lock = new();

    public string Name => "Tesseract";

    public bool IsAvailable => _engine != null;

    public TesseractOcrProvider(ILogger<TesseractOcrProvider> logger, IOptions<OcrOptions> options)
    {
        _logger = logger;
        _options = options.Value.Tesseract;
    }

    /// <summary>
    /// Initialize Tesseract engine with configuration
    /// </summary>
    public Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_engine != null)
        {
            return Task.FromResult(true);
        }

        try
        {
            _logger.LogInformation("Initializing Tesseract OCR provider...");

            // Determine tessdata path
            var tessDataPath = _options.DataPath ?? GetDefaultTessDataPath();
            
            if (!Directory.Exists(tessDataPath))
            {
                _logger.LogError("Tesseract tessdata directory not found: {Path}", tessDataPath);
                return Task.FromResult(false);
            }

            // Initialize engine with configuration
            _engine = new TesseractEngine(tessDataPath, _options.Language, EngineMode.Default);
            
            // Configure engine settings
            ConfigureEngine();

            _logger.LogInformation("Tesseract OCR provider initialized successfully");
            _logger.LogDebug("Tesseract configuration: Language={Language}, EngineMode={EngineMode}, PSM={PSM}",
                _options.Language, _options.EngineMode, _options.PageSegmentationMode);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Tesseract OCR provider");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Extract text from image stream using Tesseract
    /// </summary>
    public async Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        if (_engine == null)
        {
            throw new InvalidOperationException("Tesseract provider not initialized");
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
            // Convert stream to byte array
            var imageBytes = await ReadStreamToByteArrayAsync(imageStream, cancellationToken);
            
            // Process image with Tesseract
            lock (_lock)
            {
                using var pix = Pix.LoadFromMemory(imageBytes);
                using var page = _engine.Process(pix);

                // Extract text and confidence
                result.RawText = page.GetText().Trim();
                result.ProcessedText = CleanTesseractText(result.RawText);
                result.Confidence = page.GetMeanConfidence();
                result.Success = !string.IsNullOrEmpty(result.RawText);

                // Extract detailed character information
                ExtractCharacterResults(page, result);
            }

            stopwatch.Stop();
            result.ProcessingDurationMs = stopwatch.ElapsedMilliseconds;

            if (result.Success)
            {
                _logger.LogDebug("Tesseract OCR successful: Text='{Text}', Confidence={Confidence}",
                    result.ProcessedText, result.Confidence);
            }
            else
            {
                _logger.LogWarning("Tesseract OCR failed to extract text");
                result.ErrorMessage = "No text extracted from image";
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ProcessingDurationMs = stopwatch.ElapsedMilliseconds;
            result.Success = false;
            result.ErrorMessage = ex.Message;
            
            _logger.LogError(ex, "Tesseract OCR processing failed");
            return result;
        }
    }

    /// <summary>
    /// Dispose Tesseract resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _engine?.Dispose();
            _engine = null;
            _disposed = true;
        }
    }

    #region Private Methods

    /// <summary>
    /// Configure Tesseract engine with custom settings
    /// </summary>
    private void ConfigureEngine()
    {
        if (_engine == null) return;

        try
        {
            // Set page segmentation mode
            _engine.SetVariable("tessedit_pageseg_mode", _options.PageSegmentationMode.ToString());

            // Apply custom configuration
            foreach (var config in _options.CustomConfig)
            {
                _engine.SetVariable(config.Key, config.Value);
                _logger.LogDebug("Applied Tesseract config: {Key}={Value}", config.Key, config.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply some Tesseract configuration settings");
        }
    }

    /// <summary>
    /// Get default tessdata path based on platform
    /// </summary>
    private string GetDefaultTessDataPath()
    {
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "tessdata"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tesseract", "tessdata"),
            "/usr/share/tesseract-ocr/4.00/tessdata", // Linux
            "/usr/share/tesseract-ocr/tessdata",      // Linux alternative
            "/opt/homebrew/share/tessdata",           // macOS Homebrew
            "/usr/local/share/tessdata"               // macOS alternative
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                _logger.LogDebug("Using tessdata path: {Path}", path);
                return path;
            }
        }

        throw new DirectoryNotFoundException("Could not find tessdata directory. Please install Tesseract or configure tessdata path.");
    }

    /// <summary>
    /// Clean Tesseract OCR text for better parsing
    /// </summary>
    private string CleanTesseractText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var builder = new StringBuilder();
        
        foreach (char c in text)
        {
            // Keep only alphanumeric characters, decimal points, and specific pump status text
            if (char.IsLetterOrDigit(c) || c == '.' || c == ' ')
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// Extract character-level OCR results
    /// </summary>
    private void ExtractCharacterResults(Page page, OcrResult result)
    {
        try
        {
            using var iterator = page.GetIterator();
            iterator.Begin();

            do
            {
                var confidence = iterator.GetConfidence(PageIteratorLevel.Symbol);
                var text = iterator.GetText(PageIteratorLevel.Symbol);
                
                if (!string.IsNullOrEmpty(text) && text.Length == 1)
                {
                    iterator.TryGetBoundingBox(PageIteratorLevel.Symbol, out var bounds);
                    
                    result.Characters.Add(new CharacterResult
                    {
                        Character = text[0],
                        Confidence = confidence / 100.0, // Convert to 0-1 scale
                        BoundingBox = new BoundingBox
                        {
                            X = bounds.X1,
                            Y = bounds.Y1,
                            Width = bounds.X2 - bounds.X1,
                            Height = bounds.Y2 - bounds.Y1
                        }
                    });
                }
            } while (iterator.Next(PageIteratorLevel.Symbol));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract character-level OCR results");
        }
    }

    /// <summary>
    /// Read stream to byte array
    /// </summary>
    private async Task<byte[]> ReadStreamToByteArrayAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
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
