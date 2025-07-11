using System;

namespace WellMonitor.Shared.Models
{
    public class MonthlySummary
    {
        public int Id { get; set; }
        public string Month { get; set; } = string.Empty; // Format: YYYY-MM
        public double TotalKwh { get; set; }
        public bool Synced { get; set; }
    }
}
