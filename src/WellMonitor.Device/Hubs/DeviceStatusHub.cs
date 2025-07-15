using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WellMonitor.Device.Services;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Hubs
{
    /// <summary>
    /// SignalR hub for real-time device status updates
    /// </summary>
    public class DeviceStatusHub : Hub
    {
        private readonly ILogger<DeviceStatusHub> _logger;

        public DeviceStatusHub(ILogger<DeviceStatusHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Join a specific group for targeted updates
        /// </summary>
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Leave a specific group
        /// </summary>
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// Background service for sending real-time updates via SignalR
    /// </summary>
    public class RealtimeUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RealtimeUpdateService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(30); // Reduced from 5 seconds to 30 seconds
        
        // Cache to reduce database queries
        private DateTime _lastReadingQueryTime = DateTime.MinValue;
        private object? _cachedPumpStatus = null;

        public RealtimeUpdateService(
            IServiceProvider serviceProvider,
            ILogger<RealtimeUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Realtime update service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<DeviceStatusHub>>();
                    var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

                    // Send periodic status updates (with caching to reduce DB queries)
                    var now = DateTime.UtcNow;
                    if (_cachedPumpStatus == null || (now - _lastReadingQueryTime).TotalMinutes >= 2)
                    {
                        var recentReadings = await databaseService.GetReadingsAsync(
                            DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
                        var latestReading = recentReadings.OrderByDescending(r => r.TimestampUtc).FirstOrDefault();
                        
                        if (latestReading != null)
                        {
                            _cachedPumpStatus = new
                            {
                                Status = latestReading.Status,
                                CurrentDraw = latestReading.CurrentAmps,
                                PowerConsumption = Math.Round(latestReading.CurrentAmps * 240 / 1000, 2), // Approximate power
                                LastReading = latestReading.TimestampUtc
                            };
                        }
                        _lastReadingQueryTime = now;
                    }

                    if (_cachedPumpStatus != null)
                    {
                        await hubContext.Clients.Group("updates").SendAsync("UpdatePumpStatus", _cachedPumpStatus, stoppingToken);
                    }

                    // Send system status
                    var systemStatus = new
                    {
                        Uptime = GetSystemUptime(),
                        CpuUsage = GetCpuUsage(),
                        MemoryUsage = GetMemoryUsage(),
                        StorageUsage = GetStorageUsage(),
                        Temperature = GetSystemTemperature()
                    };

                    await hubContext.Clients.Group("updates").SendAsync("UpdateSystemStatus", systemStatus, stoppingToken);

                    // Wait before next update - using configurable interval to reduce database load
                    await Task.Delay(_updateInterval, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error in realtime update service");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("Realtime update service stopped");
        }

        private string GetSystemUptime()
        {
            var uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            return $"{uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m";
        }

        private double GetCpuUsage()
        {
            // Simplified CPU usage - in real implementation, use performance counters
            return Math.Round(Random.Shared.NextDouble() * 20 + 10, 1);
        }

        private double GetMemoryUsage()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            return Math.Round((double)totalMemory / (1024 * 1024 * 1024) * 100, 1);
        }

        private double GetStorageUsage()
        {
            try
            {
                var drive = new System.IO.DriveInfo("/");
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
    }
}
