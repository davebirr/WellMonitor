using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Services;
using WellMonitor.Shared.Models;
using System;
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
        private readonly IDatabaseService _databaseService;
        private readonly IGpioService _gpioService;
        private readonly ILogger<MonitoringBackgroundService> _logger;
        private readonly TimeSpan _monitoringInterval = TimeSpan.FromSeconds(30); // Monitor every 30 seconds
        
        public MonitoringBackgroundService(
            ICameraService cameraService,
            IDatabaseService databaseService,
            IGpioService gpioService,
            ILogger<MonitoringBackgroundService> logger)
        {
            _cameraService = cameraService;
            _databaseService = databaseService;
            _gpioService = gpioService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monitoring background service started");
            
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
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
            // Capture image from camera
            var imageBytes = await _cameraService.CaptureImageAsync();
            
            // TODO: Process image with OCR to extract current reading and status
            // For now, simulate reading values
            var reading = new Reading
            {
                Id = new Random().Next(1, int.MaxValue), // Generate random ID for now
                TimestampUtc = DateTime.UtcNow,
                CurrentAmps = 5.2, // TODO: Extract from OCR
                Status = "Normal", // TODO: Extract from OCR
                Synced = false // Mark as not synced yet
            };

            // Log the reading to local database
            await _databaseService.SaveReadingAsync(reading);
            
            // Check for abnormal conditions
            if (reading.Status == "rcyc")
            {
                _logger.LogWarning("Rapid cycling detected - triggering relay cycle");
                await HandleRapidCyclingAsync(reading);
            }
            else if (reading.Status == "Dry")
            {
                _logger.LogWarning("Dry condition detected - pump drawing insufficient current");
            }

            _logger.LogDebug("Monitoring cycle completed. Current: {Current}A, Status: {Status}", 
                reading.CurrentAmps, reading.Status);
        }

        private async Task HandleRapidCyclingAsync(Reading reading)
        {
            try
            {
                // Log the relay action
                var relayLog = new RelayActionLog
                {
                    Id = new Random().Next(1, int.MaxValue), // Generate random ID for now
                    TimestampUtc = DateTime.UtcNow,
                    Action = "PowerCycle",
                    Reason = "RapidCycling"
                };

                // Cycle the relay (turn off for 5 seconds, then back on)
                _gpioService.SetRelayState(false);
                await Task.Delay(5000); // 5 second delay
                _gpioService.SetRelayState(true);

                await _databaseService.SaveRelayActionLogAsync(relayLog);
                
                _logger.LogInformation("Successfully cycled relay power due to rapid cycling");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cycle relay power");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monitoring background service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
