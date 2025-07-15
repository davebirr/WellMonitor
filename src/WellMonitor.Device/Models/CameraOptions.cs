using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Runtime configuration source for CameraOptions that can be updated via device twin
    /// </summary>
    public class RuntimeCameraOptionsSource : IOptionsMonitor<CameraOptions>
    {
        private readonly ILogger<RuntimeCameraOptionsSource> _logger;
        private CameraOptions _currentOptions;
        private readonly List<IDisposable> _subscriptions = new();

        public RuntimeCameraOptionsSource(ILogger<RuntimeCameraOptionsSource> logger)
        {
            _logger = logger;
            _currentOptions = new CameraOptions();
        }

        public CameraOptions CurrentValue => _currentOptions;

        public CameraOptions Get(string? name) => _currentOptions;

        public IDisposable OnChange(Action<CameraOptions, string?> listener)
        {
            var subscription = new ChangeSubscription(listener);
            _subscriptions.Add(subscription);
            return subscription;
        }

        /// <summary>
        /// Update the options (called by RuntimeConfigurationService when device twin changes)
        /// </summary>
        public void UpdateOptions(CameraOptions newOptions)
        {
            var oldOptions = _currentOptions;
            _currentOptions = newOptions ?? new CameraOptions();

            // Log important changes for camera configuration
            if (oldOptions.Width != _currentOptions.Width || oldOptions.Height != _currentOptions.Height)
            {
                _logger.LogInformation("Camera resolution changed from {OldWidth}x{OldHeight} to {NewWidth}x{NewHeight}", 
                    oldOptions.Width, oldOptions.Height, _currentOptions.Width, _currentOptions.Height);
            }

            if (oldOptions.Gain != _currentOptions.Gain)
            {
                _logger.LogInformation("Camera gain changed from {OldGain} to {NewGain}", 
                    oldOptions.Gain, _currentOptions.Gain);
            }

            if (oldOptions.ShutterSpeedMicroseconds != _currentOptions.ShutterSpeedMicroseconds)
            {
                _logger.LogInformation("Camera shutter speed changed from {OldShutter}μs to {NewShutter}μs", 
                    oldOptions.ShutterSpeedMicroseconds, _currentOptions.ShutterSpeedMicroseconds);
            }

            if (oldOptions.AutoExposure != _currentOptions.AutoExposure)
            {
                _logger.LogInformation("Camera auto exposure changed from {OldAuto} to {NewAuto}", 
                    oldOptions.AutoExposure, _currentOptions.AutoExposure);
            }

            // Notify subscribers
            foreach (var subscription in _subscriptions.OfType<ChangeSubscription>())
            {
                try
                {
                    subscription.Listener(_currentOptions, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying camera options change subscriber");
                }
            }
        }

        private class ChangeSubscription : IDisposable
        {
            public Action<CameraOptions, string?> Listener { get; }

            public ChangeSubscription(Action<CameraOptions, string?> listener)
            {
                Listener = listener;
            }

            public void Dispose()
            {
                // No cleanup needed
            }
        }
    }
}
