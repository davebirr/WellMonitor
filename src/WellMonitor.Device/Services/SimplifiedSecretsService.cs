using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WellMonitor.Device.Services;

/// <summary>
/// Simplified secrets service for .env and environment variable configuration
/// Replaces SecretsService, EnvironmentSecretsService, HybridSecretsService
/// Since the project now uses only .env files and environment variables
/// </summary>
public class SimplifiedSecretsService : ISecretsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SimplifiedSecretsService> _logger;

    // Standard environment variable names for WellMonitor
    private static readonly Dictionary<string, string[]> SecretMappings = new()
    {
        ["IotHub"] = ["WELLMONITOR_IOTHUB_CONNECTION_STRING", "IotHubConnectionString", "AzureIoTDeviceConnectionString"],
        ["Storage"] = ["WELLMONITOR_STORAGE_CONNECTION_STRING", "AzureStorageConnectionString"],
        ["Encryption"] = ["WELLMONITOR_LOCAL_ENCRYPTION_KEY", "LocalEncryptionKey"],
        ["PowerApp"] = ["WELLMONITOR_POWERAPP_API_KEY", "PowerAppApiKey"],
        ["Ocr"] = ["WELLMONITOR_OCR_API_KEY", "OcrApiKey"]
    };

    public SimplifiedSecretsService(IConfiguration configuration, ILogger<SimplifiedSecretsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string?> GetIotHubConnectionStringAsync() => GetSecretAsync("IotHub");
    public Task<string?> GetStorageConnectionStringAsync() => GetSecretAsync("Storage");
    public Task<string?> GetLocalEncryptionKeyAsync() => GetSecretAsync("Encryption");
    public Task<string?> GetPowerAppApiKeyAsync() => GetSecretAsync("PowerApp");
    public Task<string?> GetOcrApiKeyAsync() => GetSecretAsync("Ocr");

    private Task<string?> GetSecretAsync(string secretType)
    {
        var possibleKeys = SecretMappings[secretType];
        
        // Try each possible key in order of preference (newer keys first)
        foreach (var key in possibleKeys)
        {
            // Try environment variable first
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(value))
            {
                _logger.LogDebug("Retrieved {SecretType} from environment variable {Key}", secretType, key);
                return Task.FromResult<string?>(value);
            }

            // Try configuration (includes .env file values loaded by Program.cs)
            value = _configuration[key];
            if (!string.IsNullOrEmpty(value))
            {
                _logger.LogDebug("Retrieved {SecretType} from configuration key {Key}", secretType, key);
                if (key != possibleKeys[0])
                {
                    _logger.LogWarning("Using legacy configuration key {LegacyKey} for {SecretType}. Consider using {PreferredKey}", 
                        key, secretType, possibleKeys[0]);
                }
                return Task.FromResult<string?>(value);
            }
        }

        _logger.LogWarning("Secret {SecretType} not found. Tried keys: {Keys}", 
            secretType, string.Join(", ", possibleKeys));
        return Task.FromResult<string?>(null);
    }
}
