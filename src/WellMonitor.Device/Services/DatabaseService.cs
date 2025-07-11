using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WellMonitor.Shared.Models;
using WellMonitor.Device.Data;
using System.Linq;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// SQLite database service implementation for local data persistence
    /// Handles readings, relay actions, and summary data with Entity Framework Core
    /// </summary>
    public class DatabaseService : IDatabaseService, IDisposable
    {
        private readonly WellMonitorDbContext _context;
        private readonly ILogger<DatabaseService> _logger;
        private bool _disposed = false;

        public DatabaseService(WellMonitorDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Reading Operations

        public async Task SaveReadingAsync(Reading reading)
        {
            try
            {
                if (reading.Id == 0)
                {
                    await AddReadingAsync(reading);
                }
                else
                {
                    _context.Readings.Update(reading);
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Updated reading {ReadingId}", reading.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving reading {ReadingId}", reading.Id);
                throw;
            }
        }

        public async Task AddReadingAsync(Reading reading)
        {
            try
            {
                _context.Readings.Add(reading);
                await _context.SaveChangesAsync();
                _logger.LogDebug("Added reading {ReadingId} with timestamp {Timestamp}", 
                    reading.Id, reading.TimestampUtc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reading with timestamp {Timestamp}", 
                    reading.TimestampUtc);
                throw;
            }
        }

        public async Task<IEnumerable<Reading>> GetReadingsAsync(DateTime from, DateTime to)
        {
            try
            {
                var readings = await _context.Readings
                    .Where(r => r.TimestampUtc >= from && r.TimestampUtc <= to)
                    .OrderBy(r => r.TimestampUtc)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} readings from {From} to {To}", 
                    readings.Count, from, to);

                return readings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving readings from {From} to {To}", from, to);
                throw;
            }
        }

        public async Task<IEnumerable<Reading>> GetUnsentReadingsAsync()
        {
            try
            {
                var unsentReadings = await _context.Readings
                    .Where(r => !r.Synced)
                    .OrderBy(r => r.TimestampUtc)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} unsent readings", unsentReadings.Count);
                return unsentReadings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unsent readings");
                throw;
            }
        }

        public async Task MarkReadingAsSentAsync(int readingId)
        {
            try
            {
                var reading = await _context.Readings.FindAsync(readingId);
                if (reading != null)
                {
                    reading.Synced = true;
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Marked reading {ReadingId} as sent", readingId);
                }
                else
                {
                    _logger.LogWarning("Reading {ReadingId} not found when marking as sent", readingId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reading {ReadingId} as sent", readingId);
                throw;
            }
        }

        #endregion

        #region Relay Action Log Operations

        public async Task SaveRelayActionLogAsync(RelayActionLog log)
        {
            try
            {
                if (log.Id == 0)
                {
                    await AddRelayActionLogAsync(log);
                }
                else
                {
                    _context.RelayActionLogs.Update(log);
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Updated relay action log {LogId}", log.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving relay action log {LogId}", log.Id);
                throw;
            }
        }

        public async Task AddRelayActionLogAsync(RelayActionLog log)
        {
            try
            {
                _context.RelayActionLogs.Add(log);
                await _context.SaveChangesAsync();
                _logger.LogDebug("Added relay action log {LogId} - {Action}", log.Id, log.Action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding relay action log - {Action}", log.Action);
                throw;
            }
        }

        public async Task<IEnumerable<RelayActionLog>> GetUnsentRelayActionLogsAsync()
        {
            try
            {
                var unsentLogs = await _context.RelayActionLogs
                    .Where(l => !l.Synced)
                    .OrderBy(l => l.TimestampUtc)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} unsent relay action logs", unsentLogs.Count);
                return unsentLogs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unsent relay action logs");
                throw;
            }
        }

        public async Task MarkRelayActionLogAsSentAsync(int logId)
        {
            try
            {
                var log = await _context.RelayActionLogs.FindAsync(logId);
                if (log != null)
                {
                    log.Synced = true;
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Marked relay action log {LogId} as sent", logId);
                }
                else
                {
                    _logger.LogWarning("Relay action log {LogId} not found when marking as sent", logId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking relay action log {LogId} as sent", logId);
                throw;
            }
        }

        #endregion

        #region Summary Operations

        public async Task<HourlySummary?> GetHourlySummaryAsync(DateTime hourUtc)
        {
            try
            {
                var dateHour = hourUtc.ToString("yyyy-MM-dd HH");
                var summary = await _context.HourlySummaries
                    .FirstOrDefaultAsync(s => s.DateHour == dateHour);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving hourly summary for {Hour}", hourUtc);
                throw;
            }
        }

        public async Task SaveHourlySummaryAsync(HourlySummary summary)
        {
            try
            {
                var existing = await _context.HourlySummaries
                    .FirstOrDefaultAsync(s => s.DateHour == summary.DateHour);

                if (existing != null)
                {
                    // Update existing summary
                    existing.TotalKwh = summary.TotalKwh;
                    existing.PumpCycles = summary.PumpCycles;
                    existing.Synced = summary.Synced;
                }
                else
                {
                    // Add new summary
                    _context.HourlySummaries.Add(summary);
                }

                await _context.SaveChangesAsync();
                _logger.LogDebug("Saved hourly summary for {Hour}", summary.DateHour);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving hourly summary for {Hour}", summary.DateHour);
                throw;
            }
        }

        public async Task<DailySummary?> GetDailySummaryAsync(DateTime dateUtc)
        {
            try
            {
                var dateStr = dateUtc.ToString("yyyy-MM-dd");
                var summary = await _context.DailySummaries
                    .FirstOrDefaultAsync(s => s.Date == dateStr);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving daily summary for {Date}", dateUtc.Date);
                throw;
            }
        }

        public async Task SaveDailySummaryAsync(DailySummary summary)
        {
            try
            {
                var existing = await _context.DailySummaries
                    .FirstOrDefaultAsync(s => s.Date == summary.Date);

                if (existing != null)
                {
                    // Update existing summary
                    existing.TotalKwh = summary.TotalKwh;
                    existing.PumpCycles = summary.PumpCycles;
                    existing.Synced = summary.Synced;
                }
                else
                {
                    // Add new summary
                    _context.DailySummaries.Add(summary);
                }

                await _context.SaveChangesAsync();
                _logger.LogDebug("Saved daily summary for {Date}", summary.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving daily summary for {Date}", summary.Date);
                throw;
            }
        }

        public async Task<MonthlySummary?> GetMonthlySummaryAsync(DateTime monthUtc)
        {
            try
            {
                var monthStr = monthUtc.ToString("yyyy-MM");
                var summary = await _context.MonthlySummaries
                    .FirstOrDefaultAsync(s => s.Month == monthStr);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly summary for {Month}", monthUtc);
                throw;
            }
        }

        public async Task SaveMonthlySummaryAsync(MonthlySummary summary)
        {
            try
            {
                var existing = await _context.MonthlySummaries
                    .FirstOrDefaultAsync(s => s.Month == summary.Month);

                if (existing != null)
                {
                    // Update existing summary
                    existing.TotalKwh = summary.TotalKwh;
                    existing.Synced = summary.Synced;
                }
                else
                {
                    // Add new summary
                    _context.MonthlySummaries.Add(summary);
                }

                await _context.SaveChangesAsync();
                _logger.LogDebug("Saved monthly summary for {Month}", summary.Month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving monthly summary for {Month}", summary.Month);
                throw;
            }
        }

        #endregion

        #region Data Cleanup

        public async Task CleanupOldReadingsAsync(DateTime cutoffDate)
        {
            try
            {
                var oldReadings = await _context.Readings
                    .Where(r => r.TimestampUtc < cutoffDate && r.Synced)
                    .ToListAsync();

                if (oldReadings.Any())
                {
                    _context.Readings.RemoveRange(oldReadings);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} old readings before {Date}", 
                        oldReadings.Count, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old readings before {Date}", cutoffDate);
                throw;
            }
        }

        public async Task CleanupOldRelayLogsAsync(DateTime cutoffDate)
        {
            try
            {
                var oldLogs = await _context.RelayActionLogs
                    .Where(l => l.TimestampUtc < cutoffDate && l.Synced)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _context.RelayActionLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} old relay logs before {Date}", 
                        oldLogs.Count, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old relay logs before {Date}", cutoffDate);
                throw;
            }
        }

        #endregion

        #region Database Initialization

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
