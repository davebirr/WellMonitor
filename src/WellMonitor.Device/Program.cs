using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using WellMonitor.Device.Services;
using WellMonitor.Device.Models;
using WellMonitor.Device.Data;

// 1. Dependency Injection: Register all services and logging
// Register options for GpioService and CameraService
var gpioOptions = new GpioOptions
{
    RelayDebounceMs = 500 // default, will be overwritten below
};

var cameraOptions = new CameraOptions
{
    Width = 1920,
    Height = 1080,
    Quality = 85,
    TimeoutMs = 30000,
    WarmupTimeMs = 2000,
    Rotation = 0,
    Brightness = 50,
    Contrast = 0,
    Saturation = 0,
    EnablePreview = false,
    DebugImagePath = "debug_images" // Relative to application directory
};

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Load .env file if it exists (for development)
        LoadEnvironmentFile();
        
        // Add secrets.json file for local testing only
        config.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);
        
        // Add environment variables for production
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Register options pattern for services
        services.AddSingleton(gpioOptions);
        services.AddSingleton(cameraOptions);
        
        // Register additional options classes
        services.AddSingleton(new AlertOptions());
        services.AddSingleton(new MonitoringOptions());
        services.AddSingleton(new ImageQualityOptions());
        services.AddSingleton(new PumpAnalysisOptions());
        services.AddSingleton(new PowerManagementOptions());
        services.AddSingleton(new StatusDetectionOptions());
        
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
        
        // Register OCR services
        RegisterOcrServices(services, context.Configuration);
        
        // Register debug options with runtime configuration
        RegisterDebugOptions(services, context.Configuration);
        
        // Register pump analysis service
        services.AddSingleton<PumpStatusAnalyzer>();
        
        // Register secrets service based on environment
        RegisterSecretsService(services, context.Configuration);

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
// Note: For now, we'll check this synchronously since the main method isn't async
var secretsService = host.Services.GetRequiredService<ISecretsService>();
// We'll validate the connection string during the background service startup instead

logger.LogInformation("Secrets service configured for mode: {SecretsMode}", 
    Environment.GetEnvironmentVariable("WELLMONITOR_SECRETS_MODE") ?? "hybrid");
// Device twin configuration will be handled by background services

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

// Helper method to register the appropriate secrets service
static void RegisterSecretsService(IServiceCollection services, IConfiguration configuration)
{
    var secretsMode = configuration["SecretsMode"] ?? Environment.GetEnvironmentVariable("WELLMONITOR_SECRETS_MODE") ?? "hybrid";
    
    switch (secretsMode.ToLowerInvariant())
    {
        case "keyvault":
            services.AddSingleton<ISecretsService, KeyVaultSecretsService>();
            break;
        case "environment":
            services.AddSingleton<ISecretsService, EnvironmentSecretsService>();
            break;
        case "hybrid":
        default:
            services.AddSingleton<ISecretsService, HybridSecretsService>();
            break;
    }
}

// Helper method to register Debug options with runtime configuration
static void RegisterDebugOptions(IServiceCollection services, IConfiguration configuration)
{
    // Register runtime configuration source for Debug options
    services.AddSingleton<RuntimeDebugOptionsSource>(provider =>
    {
        var logger = provider.GetRequiredService<ILogger<RuntimeDebugOptionsSource>>();
        var source = new RuntimeDebugOptionsSource(logger);
        
        // Initialize with values from configuration
        var initialOptions = new DebugOptions();
        configuration.GetSection("Debug").Bind(initialOptions);
        source.UpdateOptions(initialOptions);
        
        return source;
    });
    
    // Register the runtime options source as the primary IOptionsMonitor<DebugOptions>
    services.AddSingleton<IOptionsMonitor<DebugOptions>>(provider => provider.GetRequiredService<RuntimeDebugOptionsSource>());
    
    // Register the runtime configuration service (needs both OCR and Debug sources)
    services.AddSingleton<IRuntimeConfigurationService, RuntimeConfigurationService>();
    
    // Configure Debug options from configuration as fallback
    services.Configure<DebugOptions>(configuration.GetSection("Debug"));
}

// Helper method to register OCR services
static void RegisterOcrServices(IServiceCollection services, IConfiguration configuration)
{
    // Register runtime configuration source for OCR options
    services.AddSingleton<RuntimeOcrOptionsSource>(provider =>
    {
        var logger = provider.GetRequiredService<ILogger<RuntimeOcrOptionsSource>>();
        var source = new RuntimeOcrOptionsSource(logger);
        
        // Initialize with values from configuration
        var initialOptions = new OcrOptions();
        configuration.GetSection("OCR").Bind(initialOptions);
        source.UpdateOptions(initialOptions);
        
        return source;
    });
    
    // Register the runtime options source as the primary IOptionsMonitor<OcrOptions>
    services.AddSingleton<IOptionsMonitor<OcrOptions>>(provider => provider.GetRequiredService<RuntimeOcrOptionsSource>());
    
    // Configure OCR options from configuration as fallback
    services.Configure<OcrOptions>(configuration.GetSection("OCR"));
    
    // Register OCR providers
    services.AddSingleton<IOcrProvider, TesseractOcrProvider>();
    services.AddSingleton<IOcrProvider, AzureCognitiveServicesOcrProvider>();
    services.AddSingleton<IOcrProvider, PythonOcrProvider>();
    services.AddSingleton<IOcrProvider, NullOcrProvider>();
    
    // Register main OCR service (now handles live configuration updates)
    services.AddSingleton<IOcrService, OcrService>();
    
    // Register pump status analyzer for interpreting OCR results
    services.AddSingleton<PumpStatusAnalyzer>();
    
    // Register OCR testing service
    services.AddSingleton<OcrTestingService>();
    
    // Register OCR diagnostics service
    services.AddSingleton<OcrDiagnosticsService>();
}

// Simple .env file loader
static void LoadEnvironmentFile()
{
    try
    {
        var envPaths = new[] 
        {
            ".env",                    // Current directory
            "../../../.env",           // Project root from bin/Debug/net8.0
            "../../../../.env"         // Project root from src/WellMonitor.Device
        };
        
        foreach (var envPath in envPaths)
        {
            if (File.Exists(envPath))
            {
                var lines = File.ReadAllLines(envPath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                        continue;
                        
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        
                        // Remove quotes if present
                        if (value.StartsWith('"') && value.EndsWith('"'))
                            value = value[1..^1];
                            
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }
                
                Console.WriteLine($"Loaded environment variables from: {envPath}");
                break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not load .env file: {ex.Message}");
    }
}
await host.RunAsync();
