using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace WellMonitor.Device.Services
{
    public class HybridSecretsService : ISecretsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HybridSecretsService> _logger;
        private readonly KeyVaultSecretsService? _keyVaultService;
        private readonly EnvironmentSecretsService _environmentService;

        public HybridSecretsService(
            IConfiguration configuration, 
            ILogger<HybridSecretsService> logger,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Create environment service with a compatible logger
            var environmentLogger = serviceProvider.GetRequiredService<ILogger<EnvironmentSecretsService>>();
            _environmentService = new EnvironmentSecretsService(configuration, environmentLogger);
            
            // Try to initialize Key Vault service if configured
            try
            {
                var keyVaultUri = configuration["KeyVault:Uri"];
                if (!string.IsNullOrEmpty(keyVaultUri))
                {
                    var keyVaultLogger = serviceProvider.GetRequiredService<ILogger<KeyVaultSecretsService>>();
                    _keyVaultService = new KeyVaultSecretsService(configuration, keyVaultLogger);
                    _logger.LogInformation("Key Vault service initialized successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Key Vault service, falling back to environment variables");
            }
        }

        public async Task<string?> GetIotHubConnectionStringAsync()
        {
            return await GetSecretWithFallbackAsync(
                () => _keyVaultService?.GetIotHubConnectionStringAsync(),
                () => _environmentService.GetIotHubConnectionStringAsync(),
                "IotHubConnectionString");
        }

        public async Task<string?> GetStorageConnectionStringAsync()
        {
            return await GetSecretWithFallbackAsync(
                () => _keyVaultService?.GetStorageConnectionStringAsync(),
                () => _environmentService.GetStorageConnectionStringAsync(),
                "AzureStorageConnectionString");
        }

        public async Task<string?> GetLocalEncryptionKeyAsync()
        {
            return await GetSecretWithFallbackAsync(
                () => _keyVaultService?.GetLocalEncryptionKeyAsync(),
                () => _environmentService.GetLocalEncryptionKeyAsync(),
                "LocalEncryptionKey");
        }

        public async Task<string?> GetPowerAppApiKeyAsync()
        {
            return await GetSecretWithFallbackAsync(
                () => _keyVaultService?.GetPowerAppApiKeyAsync(),
                () => _environmentService.GetPowerAppApiKeyAsync(),
                "PowerAppApiKey");
        }

        public async Task<string?> GetOcrApiKeyAsync()
        {
            return await GetSecretWithFallbackAsync(
                () => _keyVaultService?.GetOcrApiKeyAsync(),
                () => _environmentService.GetOcrApiKeyAsync(),
                "OcrApiKey");
        }

        private async Task<string?> GetSecretWithFallbackAsync(
            Func<Task<string?>?> primarySource,
            Func<Task<string?>> fallbackSource,
            string secretName)
        {
            try
            {
                // Try Key Vault first if available
                if (primarySource != null)
                {
                    var primaryTask = primarySource();
                    if (primaryTask != null)
                    {
                        var primaryResult = await primaryTask;
                        if (!string.IsNullOrEmpty(primaryResult))
                        {
                            _logger.LogDebug("Retrieved secret {SecretName} from Key Vault", secretName);
                            return primaryResult;
                        }
                    }
                }

                // Fall back to environment variables
                var fallbackResult = await fallbackSource();
                if (!string.IsNullOrEmpty(fallbackResult))
                {
                    _logger.LogDebug("Retrieved secret {SecretName} from environment variables", secretName);
                    return fallbackResult;
                }

                // Final fallback to configuration (for local development)
                var configResult = _configuration[secretName];
                if (!string.IsNullOrEmpty(configResult))
                {
                    _logger.LogDebug("Retrieved secret {SecretName} from configuration", secretName);
                    return configResult;
                }

                _logger.LogWarning("Unable to retrieve secret {SecretName} from any source", secretName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {SecretName}", secretName);
                return null;
            }
        }
    }
}
