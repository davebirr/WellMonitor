using Microsoft.Extensions.Configuration;

namespace WellMonitor.Device.Services
{
    public interface ISecretsService
    {
        string? GetIotHubConnectionString();
        string? GetStorageConnectionString();
        string? GetLocalEncryptionKey();
    }

    public class SecretsService : ISecretsService
    {
        private readonly IConfiguration _configuration;
        public SecretsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string? GetIotHubConnectionString() => _configuration["IotHubConnectionString"];
        public string? GetStorageConnectionString() => _configuration["AzureStorageConnectionString"];
        public string? GetLocalEncryptionKey() => _configuration["LocalEncryptionKey"];
    }
}
