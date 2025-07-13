using Microsoft.Extensions.Configuration;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Interface for secrets management services
    /// Implementation is now SimplifiedSecretsService for .env file support
    /// </summary>
    public interface ISecretsService
    {
        Task<string?> GetIotHubConnectionStringAsync();
        Task<string?> GetStorageConnectionStringAsync();
        Task<string?> GetLocalEncryptionKeyAsync();
        Task<string?> GetPowerAppApiKeyAsync();
        Task<string?> GetOcrApiKeyAsync();
    }

    /// <summary>
    /// Legacy basic secrets service - replaced by SimplifiedSecretsService
    /// Kept for compatibility but should not be used in new code
    /// </summary>
    [Obsolete("Use SimplifiedSecretsService instead - this class is kept for compatibility only")]
    public class SecretsService : ISecretsService
    {
        private readonly IConfiguration _configuration;
        
        public SecretsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<string?> GetIotHubConnectionStringAsync() => Task.FromResult(_configuration["IotHubConnectionString"]);
        public Task<string?> GetStorageConnectionStringAsync() => Task.FromResult(_configuration["AzureStorageConnectionString"]);
        public Task<string?> GetLocalEncryptionKeyAsync() => Task.FromResult(_configuration["LocalEncryptionKey"]);
        public Task<string?> GetPowerAppApiKeyAsync() => Task.FromResult(_configuration["PowerAppApiKey"]);
        public Task<string?> GetOcrApiKeyAsync() => Task.FromResult(_configuration["OcrApiKey"]);
    }
}
