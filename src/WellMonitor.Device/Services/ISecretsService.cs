namespace WellMonitor.Device.Services;

/// <summary>
/// Interface for accessing configuration secrets and connection strings
/// </summary>
public interface ISecretsService
{
    /// <summary>
    /// Gets the IoT Hub device connection string
    /// </summary>
    Task<string?> GetIotHubConnectionStringAsync();

    /// <summary>
    /// Gets the Azure Storage connection string
    /// </summary>
    Task<string?> GetStorageConnectionStringAsync();

    /// <summary>
    /// Gets the local encryption key for sensitive data
    /// </summary>
    Task<string?> GetLocalEncryptionKeyAsync();

    /// <summary>
    /// Gets the PowerApps API key
    /// </summary>
    Task<string?> GetPowerAppApiKeyAsync();

    /// <summary>
    /// Gets the OCR service API key
    /// </summary>
    Task<string?> GetOcrApiKeyAsync();
}
