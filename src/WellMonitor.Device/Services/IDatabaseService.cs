using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Services
{
    public interface IDatabaseService
    {
        Task AddReadingAsync(Reading reading);
        Task<IEnumerable<Reading>> GetReadingsAsync(DateTime from, DateTime to);
        Task AddRelayActionLogAsync(RelayActionLog log);
        // ...other CRUD methods for summaries, etc.
    }
}
