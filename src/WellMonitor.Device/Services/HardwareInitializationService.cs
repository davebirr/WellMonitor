using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Hosted service responsible for hardware initialization and validation
    /// Ensures GPIO and camera hardware are properly initialized before starting monitoring
    /// </summary>
    public class HardwareInitializationService : IHostedService
    {
        private readonly IGpioService _gpioService;
        private readonly ICameraService _cameraService;
        private readonly ILogger<HardwareInitializationService> _logger;
        
        public HardwareInitializationService(
            IGpioService gpioService,
            ICameraService cameraService,
            ILogger<HardwareInitializationService> logger)
        {
            _gpioService = gpioService;
            _cameraService = cameraService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting hardware initialization...");
            
            try
            {
                // Initialize GPIO hardware
                await InitializeGpioAsync(cancellationToken);
                
                // Initialize camera hardware
                await InitializeCameraAsync(cancellationToken);
                
                _logger.LogInformation("Hardware initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Hardware initialization failed");
                throw; // Fail fast if hardware initialization fails
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
