using System;

namespace WellMonitor.Shared.Models
{
    public class RelayActionLog
    {
        public int Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Action { get; set; } = string.Empty; // e.g., Cycle, ManualOverride
        public string? Reason { get; set; }
        public bool Synced { get; set; }
        public string? Error { get; set; }
    }
}
