using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Services
{
    public class DatabaseService : IDatabaseService
    {
        public Task AddReadingAsync(Reading reading)
        {
            // TODO: Implement SQLite insert
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Reading>> GetReadingsAsync(DateTime from, DateTime to)
        {
            // TODO: Implement SQLite query
            return Task.FromResult<IEnumerable<Reading>>(new List<Reading>());
        }

        public Task AddRelayActionLogAsync(RelayActionLog log)
        {
            // TODO: Implement SQLite insert
            return Task.CompletedTask;
        }
    }
}
