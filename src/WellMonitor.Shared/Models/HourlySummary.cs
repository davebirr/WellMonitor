using System;

namespace WellMonitor.Shared.Models
{
    public class HourlySummary
    {
        public int Id { get; set; }
        public string DateHour { get; set; } = string.Empty; // Format: YYYY-MM-DD HH
        public double TotalKwh { get; set; }
        public int PumpCycles { get; set; }
        public bool Synced { get; set; }
    }
}
