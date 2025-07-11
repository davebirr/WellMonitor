using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WellMonitor.Device.Services
{
    public class KeyVaultSecretsService : ISecretsService
    {
        private readonly SecretClient _secretClient;
        private readonly ILogger<KeyVaultSecretsService> _logger;
        private readonly Dictionary<string, string> _secretCache = new();
        private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);

        public KeyVaultSecretsService(IConfiguration configuration, ILogger<KeyVaultSecretsService> logger)
        {
            _logger = logger;
            var keyVaultUri = configuration["KeyVault:Uri"];
            
            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new InvalidOperationException("KeyVault:Uri configuration is required");
            }

            // Use DefaultAzureCredential for secure authentication
            // This supports Managed Identity, Service Principal, and other credential types
            var credential = new DefaultAzureCredential();
            _secretClient = new SecretClient(new Uri(keyVaultUri), credential);
        }

        public async Task<string?> GetIotHubConnectionStringAsync()
        {
            return await GetSecretAsync("WellMonitor-IoTHub-ConnectionString");
        }

        public async Task<string?> GetStorageConnectionStringAsync()
        {
            return await GetSecretAsync("WellMonitor-Storage-ConnectionString");
        }

        public async Task<string?> GetLocalEncryptionKeyAsync()
        {
            return await GetSecretAsync("WellMonitor-LocalEncryption-Key");
        }

        public async Task<string?> GetPowerAppApiKeyAsync()
        {
            return await GetSecretAsync("WellMonitor-PowerApp-ApiKey");
        }

        public async Task<string?> GetOcrApiKeyAsync()
        {
            return await GetSecretAsync("WellMonitor-OCR-ApiKey");
        }

        private async Task<string?> GetSecretAsync(string secretName)
        {
            await _cacheSemaphore.WaitAsync();
            try
            {
                // Check cache first (secrets are cached for performance)
                if (_secretCache.TryGetValue(secretName, out var cachedValue))
                {
                    return cachedValue;
                }

                // Retrieve from Key Vault with retry logic
                var maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        _logger.LogDebug("Retrieving secret {SecretName} from Key Vault (attempt {Attempt})", secretName, attempt);
                        
                        var response = await _secretClient.GetSecretAsync(secretName);
                        var secretValue = response.Value.Value;
                        
                        // Cache the secret for 1 hour
                        _secretCache[secretName] = secretValue;
                        
                        _logger.LogInformation("Successfully retrieved secret {SecretName} from Key Vault", secretName);
                        return secretValue;
                    }
                    catch (Exception ex) when (attempt < maxRetries)
                    {
                        _logger.LogWarning(ex, "Failed to retrieve secret {SecretName} from Key Vault (attempt {Attempt}). Retrying...", secretName, attempt);
                        
                        // Exponential backoff
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                    }
                }
                
                _logger.LogError("Failed to retrieve secret {SecretName} from Key Vault after {MaxRetries} attempts", secretName, maxRetries);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {SecretName} from Key Vault", secretName);
                return null;
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }
    }
}
