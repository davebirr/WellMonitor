using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Background service responsible for sending telemetry data to Azure IoT Hub
    /// Processes queued readings and sends them with retry logic
    /// </summary>
    public class TelemetryBackgroundService : BackgroundService
    {
        private readonly ITelemetryService _telemetryService;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<TelemetryBackgroundService> _logger;
        private readonly TimeSpan _telemetryInterval = TimeSpan.FromMinutes(5); // Send telemetry every 5 minutes
        
        public TelemetryBackgroundService(
            ITelemetryService telemetryService,
            IDatabaseService databaseService,
            ILogger<TelemetryBackgroundService> logger)
        {
            _telemetryService = telemetryService;
            _databaseService = databaseService;
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending telemetry");
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
                // Get unsent readings from database
                var unsentReadings = await _databaseService.GetUnsentReadingsAsync();
                
                foreach (var reading in unsentReadings)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        await _telemetryService.SendTelemetryAsync(reading);
                        reading.Synced = true;
                        // Update the reading in the database to mark as synced
                        await _databaseService.SaveReadingAsync(reading);
                        _logger.LogDebug("Sent telemetry for reading {ReadingId}", reading.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send telemetry for reading {ReadingId}", reading.Id);
                        // Reading remains unsent and will be retried in next cycle
                    }
                }

                // Also send pending relay action logs
                var unsentRelayLogs = await _databaseService.GetUnsentRelayActionLogsAsync();
                
                foreach (var log in unsentRelayLogs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        await _telemetryService.SendRelayActionLogAsync(log);
                        log.Synced = true;
                        await _databaseService.SaveRelayActionLogAsync(log);
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
