using System;

namespace WellMonitor.Shared.Models
{
    public class DailySummary
    {
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty; // Format: YYYY-MM-DD
        public double TotalKwh { get; set; }
        public int PumpCycles { get; set; }
        public bool Synced { get; set; }
    }
}
