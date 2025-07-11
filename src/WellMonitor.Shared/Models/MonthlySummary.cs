using System;

namespace WellMonitor.Shared.Models
{
    public class MonthlySummary
    {
        public string Month { get; set; } = string.Empty; // Format: YYYY-MM
        public double TotalKwh { get; set; }
        public bool Synced { get; set; }
    }
}
