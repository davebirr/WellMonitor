using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using WellMonitor.Device.Services;
using WellMonitor.Device.Models;
using WellMonitor.Device.Data;

// 1. Dependency Injection: Register all services and logging
// Register options for GpioService (pattern can be repeated for other services)
var gpioOptions = new GpioOptions
{
    RelayDebounceMs = 500 // default, will be overwritten below
};

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Add secrets.json file for local testing
        config.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Register options pattern for GpioService
        services.AddSingleton(gpioOptions);
        
        // Register Entity Framework DbContext
        services.AddDbContext<WellMonitorDbContext>(options =>
            options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=wellmonitor.db"));
        
        // Register core services
        services.AddSingleton<IGpioService, GpioService>();
        services.AddSingleton<ICameraService, CameraService>();
        services.AddScoped<IDatabaseService, DatabaseService>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<ITelemetryService, TelemetryService>();
        services.AddSingleton<IDeviceTwinService, DeviceTwinService>();
        services.AddSingleton<ISecretsService, SecretsService>();

        // Register hosted services for orderly startup
        // Order matters: Dependencies first, then hardware, then workers
        services.AddHostedService<DependencyValidationService>();
        services.AddHostedService<HardwareInitializationService>();
        
        // Register background workers
        services.AddHostedService<MonitoringBackgroundService>();
        services.AddHostedService<TelemetryBackgroundService>();
        services.AddHostedService<SyncBackgroundService>();

        // Logging is automatically registered with Host
    })
    .Build();

// 2. Configuration and Secrets Management
// IConfiguration is available via DI (from Host). Use it to load settings from appsettings.json, environment variables, or device twin.
// Note: Configuration is now handled automatically by the Host and accessed via ISecretsService

// 3. Azure IoT Device Twin Configuration
// Device twin configuration is handled by the DependencyValidationService and will be applied after startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Example: Set up DeviceClient for device twin access (if using Azure IoT SDK)
// This is now handled in the background after dependency validation
var secretsService = host.Services.GetRequiredService<ISecretsService>();
string? iotHubConnectionString = secretsService.GetIotHubConnectionString();

if (!string.IsNullOrEmpty(iotHubConnectionString))
{
    logger.LogInformation("Azure IoT Hub connection configured");
    // Device twin configuration will be handled by background services
}
else
{
    logger.LogWarning("Azure IoT Hub connection string not found - some features may be limited");
}

// 4. Global Error Handling and Logging
// Logging is configured by default. Global error handling for unhandled exceptions.
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception occurred");
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(e.Exception, "Unobserved task exception occurred");
    e.SetObserved();
};

// 5. Startup Process Documentation
// The application now follows an orderly startup process:
// 1. DependencyValidationService - Validates all required configuration and secrets
// 2. HardwareInitializationService - Initializes GPIO and camera hardware
// 3. Background Services - Start monitoring, telemetry, and sync workers
// 
// If any critical component fails during startup, the application will fail fast with detailed logging.

logger.LogInformation("Well Monitor Device starting up...");
logger.LogInformation("Startup process: Dependencies → Hardware → Background Workers");

// 6. Main Application Entry Point
// The Host.RunAsync() method will start all hosted services in the correct order
await host.RunAsync();
