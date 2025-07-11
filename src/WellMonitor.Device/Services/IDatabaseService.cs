using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Services
{
    public interface IDatabaseService
    {
        // Reading operations
        Task SaveReadingAsync(Reading reading);
        Task AddReadingAsync(Reading reading);
        Task<IEnumerable<Reading>> GetReadingsAsync(DateTime from, DateTime to);
        Task<IEnumerable<Reading>> GetUnsentReadingsAsync();
        Task MarkReadingAsSentAsync(int readingId);
        
        // Relay action log operations
        Task SaveRelayActionLogAsync(RelayActionLog log);
        Task AddRelayActionLogAsync(RelayActionLog log);
        Task<IEnumerable<RelayActionLog>> GetUnsentRelayActionLogsAsync();
        Task MarkRelayActionLogAsSentAsync(int logId);
        
        // Summary operations
        Task<HourlySummary?> GetHourlySummaryAsync(DateTime hourUtc);
        Task SaveHourlySummaryAsync(HourlySummary summary);
        Task<DailySummary?> GetDailySummaryAsync(DateTime dateUtc);
        Task SaveDailySummaryAsync(DailySummary summary);
        Task<MonthlySummary?> GetMonthlySummaryAsync(DateTime monthUtc);
        Task SaveMonthlySummaryAsync(MonthlySummary summary);
        
        // Data cleanup operations
        Task CleanupOldReadingsAsync(DateTime cutoffDate);
        Task CleanupOldRelayLogsAsync(DateTime cutoffDate);
        
        // Database initialization
        Task InitializeDatabaseAsync();
    }
}
