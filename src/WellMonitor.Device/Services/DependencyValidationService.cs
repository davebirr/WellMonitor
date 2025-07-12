using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices.Client;
using WellMonitor.Device.Services;
using WellMonitor.Device.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Hosted service responsible for validating critical dependencies at startup
    /// Ensures all required configuration and services are available before starting the application
    /// </summary>
    public class DependencyValidationService : IHostedService
    {
        private readonly ISecretsService _secretsService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DependencyValidationService> _logger;
        
        public DependencyValidationService(
            ISecretsService secretsService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DependencyValidationService> logger)
        {
            _secretsService = secretsService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting dependency validation...");
            
            try
            {
                // Validate secrets and configuration
                await ValidateSecretsAsync();
                
                // Validate database connectivity
                await ValidateDatabaseAsync();
                
                // Load device twin configuration if available
                await LoadDeviceTwinConfigurationAsync();
                
                _logger.LogInformation("Dependency validation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Dependency validation failed");
                throw; // Fail fast if critical dependencies are missing
            }
        }

        private async Task ValidateSecretsAsync()
        {
            _logger.LogInformation("Validating secrets and configuration...");
            
            var validationErrors = new List<string>();
            
            // Check Azure IoT Hub connection string
            var iotHubConnectionString = await _secretsService.GetIotHubConnectionStringAsync();
            if (string.IsNullOrWhiteSpace(iotHubConnectionString) || IsPlaceholderValue(iotHubConnectionString))
            {
                validationErrors.Add("Azure IoT Hub connection string is missing");
            }
            else
            {
                _logger.LogInformation("Azure IoT Hub connection string found");
            }
            
            // Check storage connection string (optional but recommended)
            var storageConnectionString = await _secretsService.GetStorageConnectionStringAsync();
            if (string.IsNullOrWhiteSpace(storageConnectionString) || IsPlaceholderValue(storageConnectionString))
            {
                _logger.LogWarning("Azure Storage connection string is missing - some features may be limited");
            }
            else
            {
                _logger.LogInformation("Azure Storage connection string found");
            }
            
            // Check local encryption key
            var encryptionKey = await _secretsService.GetLocalEncryptionKeyAsync();
            if (string.IsNullOrWhiteSpace(encryptionKey) || IsPlaceholderValue(encryptionKey))
            {
                validationErrors.Add("Local encryption key is missing");
            }
            else
            {
                _logger.LogInformation("Local encryption key found");
            }
            
            // Check if we're in development mode
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? 
                             Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
                             "Production";
            
            var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation("Current environment: {Environment}", environment);
            
            if (validationErrors.Any())
            {
                var errorMessage = string.Join("; ", validationErrors);
                
                if (isDevelopment)
                {
                    _logger.LogWarning("Development mode: Running with missing configuration: {ErrorMessage}", errorMessage);
                    _logger.LogWarning("Some features may not work correctly without proper configuration");
                }
                else
                {
                    throw new InvalidOperationException($"Critical configuration missing: {errorMessage}");
                }
            }
            
            await Task.CompletedTask;
        }
        
        private async Task LoadDeviceTwinConfigurationAsync()
        {
            try
            {
                var iotHubConnectionString = await _secretsService.GetIotHubConnectionStringAsync();
                
                if (string.IsNullOrWhiteSpace(iotHubConnectionString) || IsPlaceholderValue(iotHubConnectionString))
                {
                    _logger.LogWarning("Azure IoT Hub connection string not available - device twin configuration will be skipped");
                    return;
                }

                using var scope = _serviceScopeFactory.CreateScope();
                var deviceTwinService = scope.ServiceProvider.GetRequiredService<IDeviceTwinService>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var gpioOptions = scope.ServiceProvider.GetRequiredService<GpioOptions>();
                var cameraOptions = scope.ServiceProvider.GetRequiredService<CameraOptions>();
                
                // Create device client
                var deviceClient = DeviceClient.CreateFromConnectionString(iotHubConnectionString);
                
                // Load device twin configuration
                var deviceTwinConfig = await deviceTwinService.FetchAndApplyConfigAsync(
                    deviceClient, 
                    configuration, 
                    gpioOptions, 
                    cameraOptions, 
                    _logger);

                // Load OCR configuration from device twin
                try
                {
                    var ocrOptions = await deviceTwinService.FetchAndApplyOcrConfigAsync(
                        deviceClient,
                        configuration,
                        _logger);
                    
                    // Apply OCR configuration to runtime configuration service
                    var runtimeConfigService = scope.ServiceProvider.GetRequiredService<IRuntimeConfigurationService>();
                    await runtimeConfigService.UpdateOcrOptionsAsync(ocrOptions);
                    
                    _logger.LogInformation("OCR configuration loaded and applied from device twin: Provider={Provider}, Confidence={Confidence}", 
                        ocrOptions.Provider, ocrOptions.MinimumConfidence);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load OCR configuration from device twin, using defaults");
                }

                // Load Debug configuration from device twin
                try
                {
                    var debugOptions = await deviceTwinService.FetchAndApplyDebugConfigAsync(
                        deviceClient,
                        configuration,
                        _logger);
                    
                    // Apply Debug configuration to runtime configuration service
                    var runtimeConfigService = scope.ServiceProvider.GetRequiredService<IRuntimeConfigurationService>();
                    await runtimeConfigService.UpdateDebugOptionsAsync(debugOptions);
                    
                    _logger.LogInformation("Debug configuration loaded and applied from device twin: ImageSaveEnabled={ImageSaveEnabled}, DebugMode={DebugMode}", 
                        debugOptions.ImageSaveEnabled, debugOptions.DebugMode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load Debug configuration from device twin, using defaults");
                }
                
                _logger.LogInformation("Device twin configuration loaded successfully");
                
                // Store the device client for use by other services
                // TODO: Consider using a singleton pattern for device client management
                
                await deviceClient.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load device twin configuration - will use default values");
            }
        }

        private static bool IsPlaceholderValue(string value)
        {
            var placeholderIndicators = new[]
            {
                "test-",
                "<YOUR_",
                "placeholder",
                "REPLACE_ME",
                "TODO:",
                "CHANGE_ME"
            };
            
            return placeholderIndicators.Any(indicator => 
                value.Contains(indicator, StringComparison.OrdinalIgnoreCase));
        }

        private async Task ValidateDatabaseAsync()
        {
            _logger.LogInformation("Validating database connectivity...");
            
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
                
                // Initialize database (creates tables if they don't exist)
                await databaseService.InitializeDatabaseAsync();
                
                // Test database connectivity by attempting to get readings
                var testQuery = DateTime.UtcNow.AddDays(-1);
                var readings = await databaseService.GetReadingsAsync(testQuery, DateTime.UtcNow);
                
                _logger.LogInformation("Database connectivity validated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connectivity validation failed");
                throw new InvalidOperationException("Database is not accessible", ex);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Dependency validation service stopped");
            await Task.CompletedTask;
        }
    }
}
