using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using WellMonitor.Device.Services;
using WellMonitor.Device.Models;

// 1. Dependency Injection: Register all services and logging
// Register options for GpioService (pattern can be repeated for other services)
var gpioOptions = new GpioOptions
{
    RelayDebounceMs = 500 // default, will be overwritten below
};

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register options pattern for GpioService
        services.AddSingleton(gpioOptions);
        services.AddSingleton<IGpioService, GpioService>();
        services.AddSingleton<ICameraService, CameraService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<ITelemetryService, TelemetryService>();
        services.AddSingleton<IDeviceTwinService, DeviceTwinService>();
        services.AddSingleton<ISecretsService, SecretsService>();

        // Logging is automatically registered with Host
        // Add other services as needed
    })
    .Build();

// 2. Configuration and Secrets Management
// IConfiguration is available via DI (from Host). Use it to load settings from appsettings.json, environment variables, or device twin.
// Securely load secrets from environment variables and secrets.json (not in source control)

var configBuilder = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("secrets.json", optional: true, reloadOnChange: true);
var configuration = configBuilder.Build();


// Use SecretsService to load secrets/config values
var secretsService = host.Services.GetRequiredService<ISecretsService>();
string? iotHubConnectionString = secretsService.GetIotHubConnectionString();
string? storageConnectionString = secretsService.GetStorageConnectionString();
string? localEncryptionKey = secretsService.GetLocalEncryptionKey();

// Example: Set up DeviceClient for device twin access (if using Azure IoT SDK)
if (!string.IsNullOrEmpty(iotHubConnectionString))
{
    var deviceClient = DeviceClient.CreateFromConnectionString(iotHubConnectionString, TransportType.Mqtt);
    var deviceTwinService = host.Services.GetRequiredService<IDeviceTwinService>();
    var logger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    // Fetch and apply config from device twin (async)
    await deviceTwinService.FetchAndApplyConfigAsync(deviceClient, configuration, gpioOptions, logger);
    // TODO: Pass these config values to your services as needed
}

// 3. Error Handling and Logging
// Logging is configured by default. For global error handling, consider using AppDomain.CurrentDomain.UnhandledException and TaskScheduler.UnobservedTaskException.
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var logger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception");
};
TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    var logger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    logger.LogCritical(e.Exception, "Unobserved task exception");
    e.SetObserved();
};

// 4. Unit Tests for Service Stubs
// TODO: Create unit tests for each service interface and implementation (see /tests folder).

// 5. Startup and Shutdown Process
// TODO: Document and implement orderly startup (e.g., initialize hardware, start background workers) and graceful shutdown (dispose resources, flush logs).

// 6. (Optional) Health Check/Status Endpoint
// TODO: Add a health/status endpoint or periodic status reporting for diagnostics.

// --- Main App Logic Entry Point ---
// TODO: Add main app logic here (e.g., start background services, orchestrate device workflow)

await host.RunAsync();
