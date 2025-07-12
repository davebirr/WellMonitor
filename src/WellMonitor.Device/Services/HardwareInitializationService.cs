using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Hosted service responsible for hardware initialization and validation
    /// Ensures GPIO, camera, and OCR providers are properly initialized before starting monitoring
    /// </summary>
    public class HardwareInitializationService : IHostedService
    {
        private readonly IGpioService _gpioService;
        private readonly ICameraService _cameraService;
        private readonly IEnumerable<IOcrProvider> _ocrProviders;
        private readonly IOcrService _ocrService;
        private readonly OcrDiagnosticsService _ocrDiagnosticsService;
        private readonly ILogger<HardwareInitializationService> _logger;
        
        public HardwareInitializationService(
            IGpioService gpioService,
            ICameraService cameraService,
            IEnumerable<IOcrProvider> ocrProviders,
            IOcrService ocrService,
            OcrDiagnosticsService ocrDiagnosticsService,
            ILogger<HardwareInitializationService> logger)
        {
            _gpioService = gpioService;
            _cameraService = cameraService;
            _ocrProviders = ocrProviders;
            _ocrService = ocrService;
            _ocrDiagnosticsService = ocrDiagnosticsService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting hardware initialization...");
            
            // Check if we're in development mode
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? 
                             Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
                             "Production";
            
            var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
            
            try
            {
                // Initialize GPIO hardware
                await InitializeGpioAsync(cancellationToken);
                
                // Initialize camera hardware
                await InitializeCameraAsync(cancellationToken);
                
                // Run OCR diagnostics first
                await _ocrDiagnosticsService.RunDiagnosticsAsync();
                
                // Initialize OCR providers
                await InitializeOcrProvidersAsync(cancellationToken);
                
                _logger.LogInformation("Hardware initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Hardware initialization failed");
                
                if (isDevelopment)
                {
                    _logger.LogWarning("Development mode: Continuing despite hardware initialization failure");
                    _logger.LogWarning("Hardware-dependent features may not work correctly");
                }
                else
                {
                    // In production, allow continuation if at least camera and GPIO work
                    // OCR failures shouldn't prevent the device from running basic monitoring
                    _logger.LogWarning("Production mode: Continuing with limited OCR functionality");
                    _logger.LogWarning("Application will run with reduced OCR capabilities");
                }
            }
        }

        private async Task InitializeGpioAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Initializing GPIO hardware...");
                
                // Test GPIO functionality
                // Set relay to known state (off)
                _gpioService.SetRelayState(false);
                await Task.Delay(100, cancellationToken); // Small delay for hardware response
                
                // Verify relay state can be read
                var relayState = _gpioService.GetRelayState();
                
                _logger.LogInformation("GPIO hardware initialized successfully. Relay state: {RelayState}", relayState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize GPIO hardware");
                throw new InvalidOperationException("GPIO hardware initialization failed", ex);
            }
        }

        private async Task InitializeCameraAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Initializing camera hardware...");
                
                // Test camera functionality by taking a test image
                var testImage = await _cameraService.CaptureImageAsync();
                
                if (testImage == null || testImage.Length == 0)
                {
                    throw new InvalidOperationException("Camera test capture returned empty image");
                }
                
                _logger.LogInformation("Camera hardware initialized successfully. Test image size: {ImageSize} bytes", testImage.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize camera hardware");
                throw new InvalidOperationException("Camera hardware initialization failed", ex);
            }
        }

        private async Task InitializeOcrProvidersAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Initializing OCR providers...");
                
                var initializationTasks = _ocrProviders.Select(async provider =>
                {
                    try
                    {
                        _logger.LogInformation("Initializing {ProviderName} OCR provider...", provider.Name);
                        var success = await provider.InitializeAsync(cancellationToken);
                        
                        if (success)
                        {
                            _logger.LogInformation("{ProviderName} OCR provider initialized successfully", provider.Name);
                        }
                        else
                        {
                            _logger.LogWarning("{ProviderName} OCR provider failed to initialize", provider.Name);
                        }
                        
                        return (Provider: provider.Name, Success: success);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception initializing {ProviderName} OCR provider", provider.Name);
                        return (Provider: provider.Name, Success: false);
                    }
                });

                var results = await Task.WhenAll(initializationTasks);
                var successfulProviders = results.Where(r => r.Success).ToList();
                var failedProviders = results.Where(r => !r.Success).ToList();

                _logger.LogInformation("OCR provider initialization completed: {Successful} successful, {Failed} failed", 
                    successfulProviders.Count, failedProviders.Count);

                if (successfulProviders.Count == 0)
                {
                    _logger.LogWarning("No OCR providers were successfully initialized - continuing with limited functionality");
                    _logger.LogWarning("The application will capture images but OCR text extraction will be unavailable");
                    return; // Don't throw - allow application to continue without OCR
                }

                if (failedProviders.Count > 0)
                {
                    _logger.LogWarning("Failed OCR providers: {FailedProviders}", 
                        string.Join(", ", failedProviders.Select(f => f.Provider)));
                }

                // Refresh OCR service provider selection now that providers are initialized
                _logger.LogInformation("Refreshing OCR provider selection with newly initialized providers...");
                _ocrService.RefreshProviderSelection();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OCR providers");
                throw new InvalidOperationException("OCR provider initialization failed", ex);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping hardware initialization service...");
            
            try
            {
                // Clean shutdown: ensure relay is in safe state
                _gpioService.SetRelayState(false);
                _logger.LogInformation("Hardware safely shut down");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during hardware shutdown");
            }
            
            await Task.CompletedTask;
        }
    }
}
