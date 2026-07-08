using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace WellMonitor.Device.Models
{
    /// <summary>
    /// Configuration options for the web dashboard
    /// </summary>
    public class WebOptions
    {
        /// <summary>
        /// Port for the web dashboard (default: 5000)
        /// </summary>
        [Range(1024, 65535, ErrorMessage = "Port must be between 1024 and 65535")]
        public int Port { get; set; } = 5000;

        /// <summary>
        /// Whether to allow network access (default: false for security)
        /// </summary>
        public bool AllowNetworkAccess { get; set; } = false;

        /// <summary>
        /// Specific IP address to bind to (default: localhost)
        /// Set to "0.0.0.0" for all interfaces when AllowNetworkAccess is true
        /// </summary>
        public string BindAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// Enable HTTPS (requires certificate configuration)
        /// </summary>
        public bool EnableHttps { get; set; } = false;

        /// <summary>
        /// HTTPS port (default: 5001)
        /// </summary>
        [Range(1024, 65535, ErrorMessage = "HTTPS Port must be between 1024 and 65535")]
        public int HttpsPort { get; set; } = 5001;

        /// <summary>
        /// CORS origins for cross-origin requests (comma-separated)
        /// </summary>
        public string CorsOrigins { get; set; } = string.Empty;

        /// <summary>
        /// Enable authentication for the web dashboard
        /// </summary>
        public bool EnableAuthentication { get; set; } = false;

        /// <summary>
        /// Basic authentication username (if EnableAuthentication is true)
        /// </summary>
        public string AuthUsername { get; set; } = "admin";

        /// <summary>
        /// Basic authentication password (if EnableAuthentication is true)
        /// </summary>
        public string AuthPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Runtime configuration source for WebOptions that can be updated via device twin
    /// </summary>
    public class RuntimeWebOptionsSource : IOptionsMonitor<WebOptions>
    {
        private readonly ILogger<RuntimeWebOptionsSource> _logger;
        private WebOptions _currentOptions;
        private readonly List<IDisposable> _subscriptions = new();

        public RuntimeWebOptionsSource(ILogger<RuntimeWebOptionsSource> logger)
        {
            _logger = logger;
            _currentOptions = new WebOptions();
        }

        public WebOptions CurrentValue => _currentOptions;

        public WebOptions Get(string? name) => _currentOptions;

        public IDisposable OnChange(Action<WebOptions, string?> listener)
        {
            var subscription = new ChangeSubscription(listener);
            _subscriptions.Add(subscription);
            return subscription;
        }

        /// <summary>
        /// Update the options (called by RuntimeConfigurationService when device twin changes)
        /// </summary>
        public void UpdateOptions(WebOptions newOptions)
        {
            var oldOptions = _currentOptions;
            _currentOptions = newOptions ?? new WebOptions();

            // Log important changes
            if (oldOptions.Port != _currentOptions.Port)
            {
                _logger.LogInformation("Web dashboard port changed from {OldPort} to {NewPort}", 
                    oldOptions.Port, _currentOptions.Port);
            }

            if (oldOptions.AllowNetworkAccess != _currentOptions.AllowNetworkAccess)
            {
                _logger.LogInformation("Web dashboard network access changed from {OldAccess} to {NewAccess}", 
                    oldOptions.AllowNetworkAccess, _currentOptions.AllowNetworkAccess);
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
                    _logger.LogError(ex, "Error notifying web options change subscriber");
                }
            }
        }

        private class ChangeSubscription : IDisposable
        {
            public readonly Action<WebOptions, string?> Listener;

            public ChangeSubscription(Action<WebOptions, string?> listener)
            {
                Listener = listener;
            }

            public void Dispose()
            {
                // Subscription cleanup handled by the parent
            }
        }
    }
}
