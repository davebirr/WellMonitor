using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WellMonitor.Device.Services
{
    public class EnvironmentSecretsService : ISecretsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EnvironmentSecretsService> _logger;

        public EnvironmentSecretsService(IConfiguration configuration, ILogger<EnvironmentSecretsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task<string?> GetIotHubConnectionStringAsync()
        {
            return Task.FromResult(GetEnvironmentSecret("WELLMONITOR_IOTHUB_CONNECTION_STRING"));
        }

        public Task<string?> GetStorageConnectionStringAsync()
        {
            return Task.FromResult(GetEnvironmentSecret("WELLMONITOR_STORAGE_CONNECTION_STRING"));
        }

        public Task<string?> GetLocalEncryptionKeyAsync()
        {
            return Task.FromResult(GetEnvironmentSecret("WELLMONITOR_LOCAL_ENCRYPTION_KEY"));
        }

        public Task<string?> GetPowerAppApiKeyAsync()
        {
            return Task.FromResult(GetEnvironmentSecret("WELLMONITOR_POWERAPP_API_KEY"));
        }

        public Task<string?> GetOcrApiKeyAsync()
        {
            return Task.FromResult(GetEnvironmentSecret("WELLMONITOR_OCR_API_KEY"));
        }

        private string? GetEnvironmentSecret(string environmentVariableName)
        {
            var secret = _configuration[environmentVariableName] ?? Environment.GetEnvironmentVariable(environmentVariableName);
            
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("Environment variable {EnvironmentVariable} is not set", environmentVariableName);
                return null;
            }

            _logger.LogDebug("Successfully retrieved secret from environment variable {EnvironmentVariable}", environmentVariableName);
            return secret;
        }
    }
}
