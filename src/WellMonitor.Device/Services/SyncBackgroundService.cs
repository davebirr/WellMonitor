using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Background service responsible for syncing daily/monthly summaries to Azure
    /// Handles periodic aggregation and sync operations
    /// </summary>
    public class SyncBackgroundService : BackgroundService
    {
        private readonly ISyncService _syncService;
        private readonly ILogger<SyncBackgroundService> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromHours(1); // Sync every hour
        
        public SyncBackgroundService(
            ISyncService syncService,
            ILogger<SyncBackgroundService> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sync background service started");
            
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await PerformSyncOperationsAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during sync operations");
                    }

                    await Task.Delay(_syncInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Sync background service is stopping due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Sync background service failed");
            }
        }

        private async Task PerformSyncOperationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Sync telemetry data (readings and logs)
                await _syncService.SyncTelemetryAsync();
                
                // Sync aggregated summaries (daily/monthly)
                await _syncService.SyncSummariesAsync();
                
                _logger.LogDebug("Sync operations completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing sync operations");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sync background service is stopping");
            
            // Try to perform final sync on shutdown
            try
            {
                await PerformSyncOperationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to perform final sync during shutdown");
            }
            
            await base.StopAsync(stoppingToken);
        }
    }
}
