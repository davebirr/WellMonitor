using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WellMonitor.Device.Models;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Services;

/// <summary>
/// Implementation of dynamic OCR service with hot-swappable configuration
/// </summary>
public class DynamicOcrService : IDynamicOcrService, IDisposable
{
    private readonly ILogger<DynamicOcrService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;
    private OcrOptions _currentOptions;
    private OcrService? _currentOcrService;
    private readonly object _lock = new();
    private bool _disposed;

    public DynamicOcrService(
        ILogger<DynamicOcrService> logger,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        IOptions<OcrOptions> initialOptions)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
        _currentOptions = initialOptions.Value;
        
        // Initialize with current options synchronously
        try
        {
            var providers = CreateProviders(_currentOptions);
            _currentOcrService = new OcrService(
                _loggerFactory.CreateLogger<OcrService>(),
                Microsoft.Extensions.Options.Options.Create(_currentOptions),
                providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OCR service");
        }
    }

    /// <summary>
    /// Update OCR configuration and switch providers if needed
    /// </summary>
    public async Task UpdateConfigurationAsync(OcrOptions ocrOptions, CancellationToken cancellationToken = default)
    {
        await UpdateConfigurationInternalAsync(ocrOptions);
    }

    /// <summary>
    /// Gets the current OCR service instance
    /// </summary>
    public IOcrService GetCurrentOcrService()
    {
        lock (_lock)
        {
            return _currentOcrService ?? throw new InvalidOperationException("OCR service not initialized");
        }
    }

    /// <summary>
    /// Tests if a specific OCR provider is available
    /// </summary>
    public async Task<bool> TestProviderAvailabilityAsync(string providerName, CancellationToken cancellationToken = default)
    {
        var availability = await TestAllProvidersAvailabilityAsync();
        return availability.ContainsKey(providerName) && availability[providerName];
    }

    /// <summary>
    /// Get current OCR configuration
    /// </summary>
    public OcrOptions GetCurrentConfiguration()
    {
        lock (_lock)
        {
            return _currentOptions;
        }
    }

    /// <summary>
    /// Process image with current configuration
    /// </summary>
    public async Task<PumpReading> ProcessImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        var service = GetCurrentOcrService();
        if (service == null)
        {
            _logger.LogError("OCR service not available");
            return new PumpReading
            {
                Status = WellMonitor.Shared.Models.PumpStatus.Unknown,
                IsValid = false,
                RawText = "OCR service not available"
            };
        }

        return await service.ProcessImageAsync(imageBytes, cancellationToken);
    }

    /// <summary>
    /// Get OCR statistics
    /// </summary>
    public OcrStatistics GetStatistics()
    {
        var service = GetCurrentOcrService();
        return service?.GetStatistics() ?? new OcrStatistics();
    }

    /// <summary>
    /// Internal method to update OCR configuration
    /// </summary>
    private async Task<bool> UpdateConfigurationInternalAsync(OcrOptions newOptions)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    _logger.LogInformation("Updating OCR configuration: Provider={Provider}, MinConfidence={MinConfidence}",
                        newOptions.Provider, newOptions.MinimumConfidence);

                    // Dispose current service if exists
                    if (_currentOcrService != null)
                    {
                        // OCR service doesn't implement IDisposable, so we just set to null
                        _currentOcrService = null;
                    }

                    // Create new OCR service with updated configuration
                    var providers = CreateProviders(newOptions);
                    _currentOcrService = new OcrService(
                        _loggerFactory.CreateLogger<OcrService>(),
                        Microsoft.Extensions.Options.Options.Create(newOptions),
                        providers);

                    _currentOptions = newOptions;
                    
                    _logger.LogInformation("OCR configuration updated successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update OCR configuration");
                    return false;
                }
            }
        });
        
        return true;
    }

    /// <summary>
    /// Test all provider availability
    /// </summary>
    private async Task<Dictionary<string, bool>> TestAllProvidersAvailabilityAsync()
    {
        var availability = new Dictionary<string, bool>();

        try
        {
            // Test Tesseract
            var tesseractProvider = new TesseractOcrProvider(
                _loggerFactory.CreateLogger<TesseractOcrProvider>(),
                Microsoft.Extensions.Options.Options.Create(_currentOptions));
            
            availability["Tesseract"] = await tesseractProvider.InitializeAsync();
            tesseractProvider.Dispose();

            // Test Azure Cognitive Services
            var azureProvider = new AzureCognitiveServicesOcrProvider(
                _loggerFactory.CreateLogger<AzureCognitiveServicesOcrProvider>(),
                Microsoft.Extensions.Options.Options.Create(_currentOptions));
            
            availability["AzureCognitiveServices"] = await azureProvider.InitializeAsync();
            azureProvider.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test provider availability");
        }

        return availability;
    }

    /// <summary>
    /// Create OCR providers based on configuration
    /// </summary>
    private IEnumerable<IOcrProvider> CreateProviders(OcrOptions options)
    {
        var providers = new List<IOcrProvider>();

        // Always create Tesseract provider
        providers.Add(new TesseractOcrProvider(
            _loggerFactory.CreateLogger<TesseractOcrProvider>(),
            Microsoft.Extensions.Options.Options.Create(options)));

        // Create Azure provider if endpoint is configured
        if (!string.IsNullOrEmpty(options.AzureCognitiveServices.Endpoint))
        {
            providers.Add(new AzureCognitiveServicesOcrProvider(
                _loggerFactory.CreateLogger<AzureCognitiveServicesOcrProvider>(),
                Microsoft.Extensions.Options.Options.Create(options)));
        }

        return providers;
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                // OCR service doesn't implement IDisposable, so we just set to null
                _currentOcrService = null;
                _disposed = true;
            }
        }
    }
}
