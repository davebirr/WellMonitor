using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Devices.Client;
using WellMonitor.Device.Services;
using WellMonitor.Device.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Background service responsible for sending telemetry data to Azure IoT Hub
    /// Processes queued readings and sends them with retry logic
    /// Also logs periodic configuration summaries for monitoring
    /// </summary>
    public class TelemetryBackgroundService : BackgroundService
    {
        private readonly ITelemetryService _telemetryService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<TelemetryBackgroundService> _logger;
        private readonly TimeSpan _telemetryInterval = TimeSpan.FromMinutes(5); // Send telemetry every 5 minutes
        private readonly TimeSpan _configLogInterval = TimeSpan.FromHours(1); // Log configuration every hour
        private int _cycleCount = 0;
        
        public TelemetryBackgroundService(
            ITelemetryService telemetryService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<TelemetryBackgroundService> logger)
        {
            _telemetryService = telemetryService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telemetry background service started");
            
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await SendPendingTelemetryAsync(stoppingToken);
                        
                        // Log configuration summary every hour (every 12 cycles of 5 minutes)
                        _cycleCount++;
                        if (_cycleCount % 12 == 0)
                        {
                            _logger.LogInformation("📊 Starting hourly configuration summary (cycle {CycleCount})...", _cycleCount);
                            await LogPeriodicConfigurationAsync(stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in telemetry service cycle");
                    }

                    await Task.Delay(_telemetryInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Telemetry background service is stopping due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Telemetry background service failed");
            }
        }

        private async Task SendPendingTelemetryAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
                
                // Get unsent readings from database
                var unsentReadings = await databaseService.GetUnsentReadingsAsync();
                
                foreach (var reading in unsentReadings)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        await _telemetryService.SendTelemetryAsync(reading);
                        reading.Synced = true;
                        // Update the reading in the database to mark as synced
                        await databaseService.SaveReadingAsync(reading);
                        _logger.LogDebug("Sent telemetry for reading {ReadingId}", reading.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send telemetry for reading {ReadingId}", reading.Id);
                        // Reading remains unsent and will be retried in next cycle
                    }
                }

                // Also send pending relay action logs
                var unsentRelayLogs = await databaseService.GetUnsentRelayActionLogsAsync();
                
                foreach (var log in unsentRelayLogs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        await _telemetryService.SendRelayActionLogAsync(log);
                        log.Synced = true;
                        await databaseService.SaveRelayActionLogAsync(log);
                        _logger.LogDebug("Sent relay action log {LogId}", log.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send relay action log {LogId}", log.Id);
                        // Log remains unsent and will be retried in next cycle
                    }
                }

                if (unsentReadings.Any() || unsentRelayLogs.Any())
                {
                    _logger.LogInformation("Processed {ReadingCount} readings and {LogCount} relay logs for telemetry", 
                        unsentReadings.Count(), unsentRelayLogs.Count());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending telemetry");
            }
        }

        private async Task LogPeriodicConfigurationAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var deviceTwinService = scope.ServiceProvider.GetService<IDeviceTwinService>();
                var secretsService = scope.ServiceProvider.GetService<ISecretsService>();
                var cameraOptions = scope.ServiceProvider.GetService<IOptions<CameraOptions>>()?.Value;
                
                if (deviceTwinService == null || secretsService == null || cameraOptions == null)
                {
                    _logger.LogWarning("Unable to log periodic configuration - required services not available");
                    return;
                }
                
                // Get Azure IoT Hub connection string
                var iotHubConnectionString = await secretsService.GetIotHubConnectionStringAsync();
                if (string.IsNullOrEmpty(iotHubConnectionString))
                {
                    _logger.LogWarning("Unable to log periodic configuration - Azure IoT Hub connection string not available");
                    return;
                }
                
                // Create device client for this operation
                var deviceClient = Microsoft.Azure.Devices.Client.DeviceClient.CreateFromConnectionString(iotHubConnectionString);
                
                try
                {
                    _logger.LogInformation("⏰ Logging hourly configuration summary...");
                    await deviceTwinService.LogPeriodicConfigurationSummaryAsync(deviceClient, cameraOptions, _logger);
                }
                finally
                {
                    await deviceClient.CloseAsync();
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log periodic configuration summary");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telemetry background service is stopping");
            
            // Try to send any remaining telemetry on shutdown
            try
            {
                await SendPendingTelemetryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send final telemetry during shutdown");
            }
            
            await base.StopAsync(stoppingToken);
        }
    }
}
