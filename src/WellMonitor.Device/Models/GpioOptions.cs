namespace WellMonitor.Device.Models
{
    /// <summary>
    /// Options for configuring GPIO-related behavior, e.g. relay debounce timing.
    /// </summary>
    public class GpioOptions
    {
        public int RelayDebounceMs { get; set; }
    }
}
