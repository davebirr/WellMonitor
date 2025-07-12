using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices.Client;
using WellMonitor.Device.Services;
using WellMonitor.Shared.Models;
using WellMonitor.Device.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Background service responsible for continuous monitoring of the well pump
    /// Captures images, processes OCR, and logs readings
    /// </summary>
    public class MonitoringBackgroundService : BackgroundService
    {
        private readonly ICameraService _cameraService;
        private readonly IGpioService _gpioService;
        private readonly IDynamicOcrService _dynamicOcrService;
        private readonly PumpStatusAnalyzer _pumpStatusAnalyzer;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MonitoringBackgroundService> _logger;
        private readonly IDeviceTwinService _deviceTwinService;
        private readonly IConfiguration _configuration;
        
        // Configuration that gets updated from device twin
        private TimeSpan _monitoringInterval = TimeSpan.FromSeconds(30);
        private PowerManagementOptions _powerManagementOptions = new();
        private DateTime _lastConfigUpdate = DateTime.MinValue;
        
        public MonitoringBackgroundService(
            ICameraService cameraService,
            IGpioService gpioService,
            IDynamicOcrService dynamicOcrService,
            PumpStatusAnalyzer pumpStatusAnalyzer,
            IDeviceTwinService deviceTwinService,
            IConfiguration configuration,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<MonitoringBackgroundService> logger)
        {
            _cameraService = cameraService;
            _gpioService = gpioService;
            _dynamicOcrService = dynamicOcrService;
            _pumpStatusAnalyzer = pumpStatusAnalyzer;
            _deviceTwinService = deviceTwinService;
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monitoring background service started");
            
            try
            {
                // Initial configuration update
                await UpdateConfigurationAsync();
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Periodically update configuration from device twin
                        await UpdateConfigurationAsync();
                        
                        await PerformMonitoringCycleAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during monitoring cycle");
                    }

                    await Task.Delay(_monitoringInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Monitoring background service is stopping due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Monitoring background service failed");
            }
        }

        private async Task PerformMonitoringCycleAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Starting monitoring cycle...");

                // Capture image from camera
                var imageBytes = await _cameraService.CaptureImageAsync();
                _logger.LogDebug("Captured image: {Size} bytes", imageBytes.Length);

                // Process image with OCR to extract current reading and status
                var ocrService = _dynamicOcrService.GetCurrentOcrService();
                var pumpReading = await ocrService.ProcessImageAsync(imageBytes, cancellationToken);
                
                _logger.LogDebug("OCR processing completed: Status={Status}, Current={Current}A, Confidence={Confidence}, Valid={Valid}", 
                    pumpReading.Status, pumpReading.CurrentAmps?.ToString("F2") ?? "N/A", pumpReading.Confidence, pumpReading.IsValid);

                // Convert to Reading model for database storage
                var reading = new Reading
                {
                    TimestampUtc = DateTime.UtcNow,
                    CurrentAmps = pumpReading.CurrentAmps ?? 0.0,
                    Status = pumpReading.Status.ToString(),
                    Synced = false,
                    Error = pumpReading.IsValid ? null : "Invalid OCR reading"
                };

                // Log the reading to local database
                using var scope = _serviceScopeFactory.CreateScope();
                var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
                await databaseService.AddReadingAsync(reading);
                
                _logger.LogInformation("Reading logged: Current={Current}A, Status={Status}, Valid={Valid}", 
                    pumpReading.CurrentAmps?.ToString("F2") ?? "N/A", pumpReading.Status, pumpReading.IsValid);

                // Check for abnormal conditions that require action
                if (pumpReading.IsValid)
                {
                    await HandlePumpStatusAsync(pumpReading, reading);
                }
                else
                {
                    _logger.LogWarning("Invalid pump reading - no action taken. OCR Text: '{Text}'", pumpReading.RawText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monitoring cycle");
                
                // Log error reading to database
                var errorReading = new Reading
                {
                    TimestampUtc = DateTime.UtcNow,
                    CurrentAmps = 0.0,
                    Status = PumpStatus.Unknown.ToString(),
                    Synced = false,
                    Error = ex.Message
                };

                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
                    await databaseService.AddReadingAsync(errorReading);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to log error reading to database");
                }
            }
        }

        private async Task HandleRapidCyclingAsync(Reading reading)
        {
            try
            {
                // Log the relay action
                var relayLog = new RelayActionLog
                {
                    // Don't set ID - let the database generate it
                    TimestampUtc = DateTime.UtcNow,
                    Action = "PowerCycle",
                    Reason = "RapidCycling"
                };

                // Cycle the relay (turn off for 5 seconds, then back on)
                _gpioService.SetRelayState(false);
                await Task.Delay(5000); // 5 second delay
                _gpioService.SetRelayState(true);

                using var scope = _serviceScopeFactory.CreateScope();
                var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
                await databaseService.AddRelayActionLogAsync(relayLog); // Use AddRelayActionLogAsync instead of SaveRelayActionLogAsync
                
                _logger.LogInformation("Successfully cycled relay power due to rapid cycling");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cycle relay power");
            }
        }

        private async Task HandlePumpStatusAsync(PumpReading pumpReading, Reading reading)
        {
            try
            {
                // Check if automatic actions are enabled
                if (!_powerManagementOptions.EnableAutoActions)
                {
                    _logger.LogDebug("Automatic actions disabled, skipping pump status handling");
                    return;
                }

                // Check for rapid cycling condition that requires power cycling
                if (pumpReading.Status == PumpStatus.RapidCycle)
                {
                    _logger.LogWarning("Rapid cycling detected - checking if power cycle is needed");
                    
                    // Check if enough time has passed since last relay action
                    using var scope = _serviceScopeFactory.CreateScope();
                    var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
                    
                    // Get recent relay actions to check timing (look back 24 hours)
                    var recentActions = await databaseService.GetUnsentRelayActionLogsAsync();
                    var lastRelayAction = recentActions.OrderByDescending(a => a.TimestampUtc).FirstOrDefault();
                    
                    var timeSinceLastAction = lastRelayAction != null 
                        ? DateTime.UtcNow - lastRelayAction.TimestampUtc 
                        : TimeSpan.MaxValue;
                    
                    // Use configurable minimum interval
                    if (timeSinceLastAction.TotalMinutes >= _powerManagementOptions.MinimumCycleIntervalMinutes)
                    {
                        _logger.LogWarning("Initiating power cycle for rapid cycling condition");
                        
                        // Cycle the relay power: Turn off, wait, turn on
                        _gpioService.SetRelayState(false);
                        await Task.Delay(_powerManagementOptions.PowerCycleDelaySeconds * 1000); // Convert to milliseconds
                        _gpioService.SetRelayState(true);
                        
                        // Log the relay action
                        var relayAction = new RelayActionLog
                        {
                            TimestampUtc = DateTime.UtcNow,
                            Action = "PowerCycle",
                            Reason = "RapidCycling",
                            Synced = false
                        };
                        
                        await databaseService.AddRelayActionLogAsync(relayAction);
                        
                        _logger.LogInformation("Power cycle completed and logged (delay: {Delay}s)", _powerManagementOptions.PowerCycleDelaySeconds);
                    }
                    else
                    {
                        var waitTime = _powerManagementOptions.MinimumCycleIntervalMinutes - timeSinceLastAction.TotalMinutes;
                        _logger.LogInformation("Rapid cycling detected but power cycle suppressed. " +
                            "Must wait {WaitTime:F1} more minutes since last action", waitTime);
                    }
                }
                else if (pumpReading.Status == PumpStatus.Dry)
                {
                    _logger.LogWarning("Dry condition detected - pump may be running dry");
                    
                    // Check if dry condition cycling is enabled (usually disabled for safety)
                    if (_powerManagementOptions.EnableDryConditionCycling)
                    {
                        _logger.LogWarning("Dry condition cycling is enabled - this is potentially dangerous");
                        // Implement dry condition cycling logic here if needed
                    }
                    else
                    {
                        _logger.LogInformation("Dry condition cycling disabled for safety - no action taken");
                    }
                }
                else if (pumpReading.Status == PumpStatus.Off && pumpReading.CurrentAmps > 0.1)
                {
                    _logger.LogWarning("Inconsistent reading: Status shows Off but current is {Current}A", 
                        pumpReading.CurrentAmps);
                }
                
                // Additional status handling can be added here
                _logger.LogDebug("Pump status handling completed for {Status}", pumpReading.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling pump status: {Status}", pumpReading.Status);
            }
        }

        /// <summary>
        /// Updates configuration from device twin periodically
        /// </summary>
        private async Task UpdateConfigurationAsync()
        {
            try
            {
                // Only update if enough time has passed (every 10 minutes)
                if (DateTime.UtcNow - _lastConfigUpdate < TimeSpan.FromMinutes(10))
                    return;

                // Get device client from a scoped service
                using var scope = _serviceScopeFactory.CreateScope();
                var secretsService = scope.ServiceProvider.GetRequiredService<ISecretsService>();
                var connectionString = await secretsService.GetIotHubConnectionStringAsync();
                
                if (!string.IsNullOrEmpty(connectionString))
                {
                    using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
                    
                    // Update pump analyzer configuration
                    await _pumpStatusAnalyzer.UpdateConfigurationAsync(deviceClient);
                    
                    // Update monitoring interval from device twin
                    var twin = await deviceClient.GetTwinAsync();
                    var desired = twin.Properties.Desired;
                    
                    if (desired.Contains("monitoringIntervalSeconds"))
                    {
                        var intervalSeconds = (int)desired["monitoringIntervalSeconds"];
                        _monitoringInterval = TimeSpan.FromSeconds(intervalSeconds);
                        _logger.LogInformation("Monitoring interval updated to {Interval} seconds", intervalSeconds);
                    }
                    
                    // Update power management options
                    _powerManagementOptions = await _deviceTwinService.FetchPowerManagementConfigAsync(deviceClient, _configuration, _logger);
                    
                    _lastConfigUpdate = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration from device twin");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monitoring background service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
