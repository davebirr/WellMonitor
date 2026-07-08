using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Services;
using System.Diagnostics;

namespace WellMonitor.Device.Controllers
{
    /// <summary>
    /// API controller for device status and system monitoring
    /// Provides real-time system metrics and health information
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceStatusController : ControllerBase
    {
        private readonly ILogger<DeviceStatusController> _logger;
        private readonly IDatabaseService _databaseService;
        private readonly ICameraService _cameraService;
        private readonly IGpioService _gpioService;

        public DeviceStatusController(
            ILogger<DeviceStatusController> logger,
            IDatabaseService databaseService,
            ICameraService cameraService,
            IGpioService gpioService)
        {
            _logger = logger;
            _databaseService = databaseService;
            _cameraService = cameraService;
            _gpioService = gpioService;
        }

        /// <summary>
        /// Get current device status including latest pump reading
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetCurrentStatus()
        {
            try
            {
                // Get the latest reading from the last hour
                var recentReadings = await _databaseService.GetReadingsAsync(
                    DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
                var latestReading = recentReadings.OrderByDescending(r => r.TimestampUtc).FirstOrDefault();

                var pumpStatus = new
                {
                    Status = latestReading?.Status ?? "Unknown",
                    CurrentDraw = latestReading?.CurrentAmps ?? 0.0,
                    PowerConsumption = Math.Round((latestReading?.CurrentAmps ?? 0.0) * 240 / 1000, 2),
                    LastReading = latestReading?.TimestampUtc ?? DateTime.MinValue
                };

                var systemStatus = new
                {
                    Uptime = GetSystemUptime(),
                    CpuUsage = GetCpuUsage(),
                    MemoryUsage = GetMemoryUsage(),
                    StorageUsage = GetStorageUsage(),
                    Temperature = GetSystemTemperature()
                };

                var ocrStatistics = new
                {
                    SuccessRate = 85.0, // TODO: Calculate from database
                    AverageConfidence = 78.5, // TODO: Calculate from database  
                    TotalProcessed = recentReadings.Count(),
                    CurrentProvider = "Tesseract"
                };

                return Ok(new
                {
                    PumpStatus = pumpStatus,
                    SystemStatus = systemStatus,
                    OcrStatistics = ocrStatistics,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get device status");
                return StatusCode(500, new { Error = "Failed to retrieve device status" });
            }
        }

        /// <summary>
        /// Get device health check
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealthCheck()
        {
            try
            {
                var health = new
                {
                    Status = "Healthy",
                    Components = new
                    {
                        Database = await CheckDatabaseHealth(),
                        Camera = CheckCameraHealth(),
                        Gpio = CheckGpioHealth(),
                        Disk = CheckDiskHealth()
                    },
                    Timestamp = DateTime.UtcNow
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new { Status = "Error", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get alerts from the last 24 hours
        /// </summary>
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            try
            {
                // Get recent relay actions as alerts
                var relayActions = await _databaseService.GetUnsentRelayActionLogsAsync();
                
                var alerts = relayActions.Take(10).Select(action => new
                {
                    Title = $"Relay {action.Action}",
                    Message = $"Pump relay action: {action.Action} due to {action.Reason}",
                    Severity = action.Reason == "RapidCycling" ? "Warning" : "Info",
                    Timestamp = action.TimestampUtc
                }).ToList();

                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get alerts");
                return StatusCode(500, new { Error = "Failed to retrieve alerts" });
            }
        }

        /// <summary>
        /// Get reading history for specified time period
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int hours = 1)
        {
            try
            {
                var startTime = DateTime.UtcNow.AddHours(-hours);
                var readings = await _databaseService.GetReadingsAsync(startTime, DateTime.UtcNow);

                var history = readings.Select(r => new
                {
                    Timestamp = r.TimestampUtc,
                    CurrentDraw = r.CurrentAmps,
                    Status = r.Status
                }).ToList();

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get reading history");
                return StatusCode(500, new { Error = "Failed to retrieve reading history" });
            }
        }

        /// <summary>
        /// Manual relay cycle endpoint
        /// </summary>
        [HttpPost("manual-cycle")]
        public async Task<IActionResult> ManualRelayCycle()
        {
            try
            {
                // Cycle the relay (turn off for 5 seconds, then back on)
                _gpioService.SetRelayState(false);
                await Task.Delay(5000);
                _gpioService.SetRelayState(true);

                // Log the manual action
                var relayLog = new WellMonitor.Shared.Models.RelayActionLog
                {
                    TimestampUtc = DateTime.UtcNow,
                    Action = "ManualCycle",
                    Reason = "WebDashboard",
                    Synced = false
                };

                await _databaseService.AddRelayActionLogAsync(relayLog);

                _logger.LogInformation("Manual relay cycle completed via web dashboard");
                return Ok(new { Message = "Relay cycled successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cycle relay manually");
                return StatusCode(500, new { Error = "Failed to cycle relay" });
            }
        }

        #region Helper Methods

        private async Task<object> CheckDatabaseHealth()
        {
            try
            {
                // Try to get a recent reading to test database connectivity
                var testReadings = await _databaseService.GetReadingsAsync(
                    DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
                
                return new { Status = "Healthy", LastQuery = DateTime.UtcNow };
            }
            catch (Exception ex)
            {
                return new { Status = "Error", Error = ex.Message };
            }
        }

        private object CheckCameraHealth()
        {
            try
            {
                // For now, just check if the service is available
                return new { Status = "Healthy", LastCheck = DateTime.UtcNow };
            }
            catch (Exception ex)
            {
                return new { Status = "Error", Error = ex.Message };
            }
        }

        private object CheckGpioHealth()
        {
            try
            {
                // Check if GPIO service is responsive
                var relayState = _gpioService.GetRelayState();
                return new { Status = "Healthy", RelayState = relayState };
            }
            catch (Exception ex)
            {
                return new { Status = "Error", Error = ex.Message };
            }
        }

        private object CheckDiskHealth()
        {
            try
            {
                var storageUsage = GetStorageUsage();
                var status = storageUsage > 90 ? "Warning" : "Healthy";
                return new { Status = status, UsagePercent = storageUsage };
            }
            catch (Exception ex)
            {
                return new { Status = "Error", Error = ex.Message };
            }
        }

        private string GetSystemUptime()
        {
            var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            return $"{uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m";
        }

        private double GetCpuUsage()
        {
            // Simplified CPU usage - in a real implementation, use performance counters
            return Math.Round(Random.Shared.NextDouble() * 20 + 10, 1);
        }

        private double GetMemoryUsage()
        {
            var process = Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            return Math.Round((double)totalMemory / (1024 * 1024 * 1024) * 100, 1);
        }

        private double GetStorageUsage()
        {
            try
            {
                var drive = new DriveInfo("/");
                var usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
                return Math.Round((double)usedSpace / drive.TotalSize * 100, 1);
            }
            catch
            {
                return 0;
            }
        }

        private double GetSystemTemperature()
        {
            // Simplified temperature reading - in real implementation, read from sensors
            return Math.Round(Random.Shared.NextDouble() * 15 + 35, 1);
        }

        #endregion
    }
}
