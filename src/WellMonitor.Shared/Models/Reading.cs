using System;

namespace WellMonitor.Shared.Models
{
    public class Reading
    {
        public int Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public double CurrentAmps { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool Synced { get; set; }
        public string? Error { get; set; }
    }
}
