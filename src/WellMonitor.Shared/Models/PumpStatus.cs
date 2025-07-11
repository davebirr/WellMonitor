namespace WellMonitor.Shared.Models
{
    /// <summary>
    /// Pump status conditions that can be detected
    /// </summary>
    public enum PumpStatus
    {
        /// <summary>
        /// Pump is operating normally with expected current draw
        /// </summary>
        Normal,
        
        /// <summary>
        /// Pump is drawing minimal current but powered (0.00-0.05A typical)
        /// </summary>
        Idle,
        
        /// <summary>
        /// Pump is cycling too rapidly, indicating a problem
        /// </summary>
        RapidCycle,
        
        /// <summary>
        /// Pump is running but drawing less current than expected (dry well)
        /// </summary>
        Dry,
        
        /// <summary>
        /// No power to pump or display is completely off/dark
        /// </summary>
        Off,
        
        /// <summary>
        /// Unable to read the display or OCR failed
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Constants for pump status string values
    /// </summary>
    public static class PumpStatusConstants
    {
        public const string Normal = "Normal";
        public const string Idle = "Idle";
        public const string RapidCycle = "rcyc";
        public const string Dry = "Dry";
        public const string Off = "Off";
        public const string Unknown = "Unknown";
        
        /// <summary>
        /// Threshold for considering pump as idle (very low current)
        /// </summary>
        public const double IdleThreshold = 0.05; // Amps
        
        /// <summary>
        /// Typical minimum current when pump is actually running
        /// </summary>
        public const double MinimumRunningCurrent = 0.1; // Amps
    }
}
