namespace WellMonitor.Device.Models
{
    /// <summary>
    /// Configuration options for the Raspberry Pi camera
    /// </summary>
    public class CameraOptions
    {
        /// <summary>
        /// Width of the captured image in pixels
        /// </summary>
        public int Width { get; set; } = 1920;

        /// <summary>
        /// Height of the captured image in pixels
        /// </summary>
        public int Height { get; set; } = 1080;

        /// <summary>
        /// Image quality (0-100, higher is better quality)
        /// </summary>
        public int Quality { get; set; } = 95;

        /// <summary>
        /// Timeout for camera operations in milliseconds
        /// </summary>
        public int TimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Camera warm-up time in milliseconds before capture
        /// </summary>
        public int WarmupTimeMs { get; set; } = 2000;

        /// <summary>
        /// Whether to enable camera preview (useful for debugging)
        /// </summary>
        public bool EnablePreview { get; set; } = false;

        /// <summary>
        /// Directory to save captured images for debugging
        /// </summary>
        public string? DebugImagePath { get; set; }

        /// <summary>
        /// Camera rotation in degrees (0, 90, 180, 270)
        /// </summary>
        public int Rotation { get; set; } = 0;

        /// <summary>
        /// Camera brightness (-100 to 100)
        /// </summary>
        public int Brightness { get; set; } = 50;

        /// <summary>
        /// Camera contrast (-100 to 100)
        /// </summary>
        public int Contrast { get; set; } = 0;

        /// <summary>
        /// Camera saturation (-100 to 100)
        /// </summary>
        public int Saturation { get; set; } = 0;

        /// <summary>
        /// Camera gain/ISO sensitivity (1.0-64.0, higher for low light)
        /// </summary>
        public double Gain { get; set; } = 1.0;

        /// <summary>
        /// Shutter speed in microseconds (for low light, try 50000-200000)
        /// </summary>
        public int ShutterSpeedMicroseconds { get; set; } = 0;

        /// <summary>
        /// Enable/disable automatic exposure
        /// </summary>
        public bool AutoExposure { get; set; } = true;

        /// <summary>
        /// Enable/disable automatic white balance
        /// </summary>
        public bool AutoWhiteBalance { get; set; } = true;
    }
}
