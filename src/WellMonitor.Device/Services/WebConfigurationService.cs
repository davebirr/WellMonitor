using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Service that dynamically configures web server settings based on device twin configuration
    /// </summary>
    public class WebConfigurationService : IHostedService
    {
        private readonly ILogger<WebConfigurationService> _logger;
        private readonly IOptionsMonitor<WebOptions> _webOptions;
        private readonly IServer _server;
        private readonly IWebHostEnvironment _environment;

        public WebConfigurationService(
            ILogger<WebConfigurationService> logger,
            IOptionsMonitor<WebOptions> webOptions,
            IServer server,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _webOptions = webOptions;
            _server = server;
            _environment = environment;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = _webOptions.CurrentValue;
            
            _logger.LogInformation("Configuring web dashboard...");
            _logger.LogInformation("Port: {Port}", options.Port);
            _logger.LogInformation("Network Access: {NetworkAccess}", options.AllowNetworkAccess);
            _logger.LogInformation("Bind Address: {BindAddress}", options.BindAddress);
            _logger.LogInformation("HTTPS Enabled: {HttpsEnabled}", options.EnableHttps);

            // Log the actual addresses the server is listening on
            await LogServerAddresses();

            // Set up monitoring for configuration changes
            _webOptions.OnChange(OnWebOptionsChanged);
            
            _logger.LogInformation("Web configuration service startup completed");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Web configuration service stopped");
            return Task.CompletedTask;
        }

        private void OnWebOptionsChanged(WebOptions newOptions)
        {
            _logger.LogInformation("Web configuration changed - restart required for some changes to take effect");
            _logger.LogInformation("New Port: {Port}", newOptions.Port);
            _logger.LogInformation("New Network Access: {NetworkAccess}", newOptions.AllowNetworkAccess);
            _logger.LogInformation("New Bind Address: {BindAddress}", newOptions.BindAddress);
            
            // Note: Kestrel configuration changes require application restart
            // In a production environment, you might want to implement graceful restart
            _logger.LogWarning("Web server configuration changes require application restart to take effect");
        }

        private async Task LogServerAddresses()
        {
            try
            {
                // Wait a bit longer for the server to start
                await Task.Delay(2000);

                var addressFeature = _server.Features.Get<IServerAddressesFeature>();
                if (addressFeature?.Addresses?.Any() == true)
                {
                    _logger.LogInformation("✅ Web dashboard is successfully listening on:");
                    foreach (var address in addressFeature.Addresses)
                    {
                        _logger.LogInformation("  🌐 {Address}", address);
                    }
                }
                else
                {
                    var options = _webOptions.CurrentValue;
                    var protocol = options.EnableHttps ? "https" : "http";
                    var host = options.AllowNetworkAccess ? options.BindAddress : "localhost";
                    _logger.LogWarning("⚠️  Could not detect server addresses, but should be accessible at: {Protocol}://{Host}:{Port}", 
                        protocol, host, options.Port);
                    
                    // Additional debugging
                    _logger.LogInformation("Server features available: {FeatureCount}", _server.Features.Count());
                    _logger.LogInformation("IServerAddressesFeature available: {Available}", addressFeature != null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking server addresses");
            }
        }
    }
}
