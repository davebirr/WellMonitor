using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using WellMonitor.Device.Services;
using WellMonitor.Device.Models;
using WellMonitor.Device.Data;
using WellMonitor.Device.Hubs;

// 1. Dependency Injection: Register all services and logging
var gpioOptions = new GpioOptions
{
    RelayDebounceMs = 500 // default, will be overwritten by device twin
};

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.ConfigureKestrel((context, options) =>
        {
            // Get web configuration from environment/config
            var config = context.Configuration;
            
            var webPort = config.GetValue<int>("Web:Port", 5000);
            var allowNetworkAccess = config.GetValue<bool>("Web:AllowNetworkAccess", false);
            var bindAddress = config.GetValue<string>("Web:BindAddress", "127.0.0.1");
            var enableHttps = config.GetValue<bool>("Web:EnableHttps", false);
            var httpsPort = config.GetValue<int>("Web:HttpsPort", 5001);

            // Configure HTTP endpoint
            if (allowNetworkAccess)
            {
                if (bindAddress == "0.0.0.0")
                {
                    options.ListenAnyIP(webPort);
                }
                else
                {
                    options.Listen(System.Net.IPAddress.Parse(bindAddress), webPort);
                }
            }
            else
            {
                options.ListenLocalhost(webPort);
            }

            // Configure HTTPS if enabled
            if (enableHttps)
            {
                if (allowNetworkAccess && bindAddress == "0.0.0.0")
                {
                    options.ListenAnyIP(httpsPort, listenOptions =>
                    {
                        listenOptions.UseHttps();
                    });
                }
                else
                {
                    options.ListenLocalhost(httpsPort, listenOptions =>
                    {
                        listenOptions.UseHttps();
                    });
                }
            }
        });
        webBuilder.UseWebRoot("wwwroot");
        webBuilder.Configure((context, app) =>
        {
            var env = context.HostingEnvironment;
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Get web options for CORS configuration
            var webOptionsSource = app.ApplicationServices.GetService<RuntimeWebOptionsSource>();
            var webOptions = webOptionsSource?.CurrentValue ?? new WebOptions();

            // Configure CORS if origins are specified
            if (!string.IsNullOrEmpty(webOptions.CorsOrigins))
            {
                app.UseCors(builder =>
                {
                    var origins = webOptions.CorsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.Trim()).ToArray();
                    builder.WithOrigins(origins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            }

            app.UseRouting();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<DeviceStatusHub>("/devicestatushub");
                
                // Add a simple health check at root for easy testing
                endpoints.MapGet("/health", () => 
                {
                    return Results.Ok(new { 
                        status = "healthy", 
                        timestamp = DateTime.UtcNow,
                        service = "WellMonitor",
                        version = "1.0.0"
                    });
                });
            });
        });
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        // Load .env file if it exists (for development)
        LoadEnvironmentFile();
        
        // Add environment variables (primary configuration method)
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Web API services
        services.AddControllers();
        services.AddSignalR();
        services.AddCors();
        
        // Register web options with runtime configuration
        RegisterWebOptions(services, context.Configuration);
        
        // Register options pattern for services
        services.AddSingleton(gpioOptions);
        
        // Register camera options with runtime configuration
        RegisterCameraOptions(services, context.Configuration);
        
        // Register additional options classes
        services.AddSingleton(new AlertOptions());
        services.AddSingleton(new MonitoringOptions());
        services.AddSingleton(new ImageQualityOptions());
        services.AddSingleton(new PumpAnalysisOptions());
        services.AddSingleton(new PowerManagementOptions());
        services.AddSingleton(new StatusDetectionOptions());
        services.AddSingleton(new RegionOfInterestOptions
        {
            RoiPercent = new RoiCoordinates { X = 10, Y = 10, Width = 80, Height = 80 }
        });
        
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
        
        // Register web dashboard services (implemented as hosted service)
        services.AddHostedService<RealtimeUpdateService>();
        services.AddHostedService<WebConfigurationService>();
        
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

// Helper method to register web options with runtime configuration
static void RegisterWebOptions(IServiceCollection services, IConfiguration configuration)
{
    // Register runtime configuration source for Web options
    services.AddSingleton<RuntimeWebOptionsSource>(provider =>
    {
        var logger = provider.GetRequiredService<ILogger<RuntimeWebOptionsSource>>();
        var source = new RuntimeWebOptionsSource(logger);
        
        // Initialize with values from configuration
        var initialOptions = new WebOptions();
        configuration.GetSection("Web").Bind(initialOptions);
        source.UpdateOptions(initialOptions);
        
        return source;
    });
    
    // Register the runtime options source as the primary IOptionsMonitor<WebOptions>
    services.AddSingleton<IOptionsMonitor<WebOptions>>(provider => 
        provider.GetRequiredService<RuntimeWebOptionsSource>());
    
    // Configure Web options from configuration as fallback
    services.Configure<WebOptions>(configuration.GetSection("Web"));
}

// Helper method to register the secrets service
static void RegisterSecretsService(IServiceCollection services, IConfiguration configuration)
{
    // Use simplified secrets service that handles .env files and environment variables
    // This replaces the complex KeyVault/Environment/Hybrid setup since we now use .env files only
    services.AddSingleton<ISecretsService, SimplifiedSecretsService>();
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
    
    // Register the runtime configuration service (needs OCR, Debug, Web, and Camera sources)
    services.AddSingleton<IRuntimeConfigurationService>(provider => 
    {
        var logger = provider.GetRequiredService<ILogger<RuntimeConfigurationService>>();
        var ocrSource = provider.GetRequiredService<RuntimeOcrOptionsSource>();
        var debugSource = provider.GetRequiredService<RuntimeDebugOptionsSource>();
        var webSource = provider.GetRequiredService<RuntimeWebOptionsSource>();
        var cameraSource = provider.GetRequiredService<RuntimeCameraOptionsSource>();
        
        return new RuntimeConfigurationService(logger, ocrSource, debugSource, webSource, cameraSource);
    });
    
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
    
    // Register OCR diagnostics service
    services.AddSingleton<OcrDiagnosticsService>();
}

// Helper method to register camera options with runtime configuration
static void RegisterCameraOptions(IServiceCollection services, IConfiguration configuration)
{
    // Register runtime configuration source for Camera options
    services.AddSingleton<RuntimeCameraOptionsSource>(provider =>
    {
        var logger = provider.GetRequiredService<ILogger<RuntimeCameraOptionsSource>>();
        var source = new RuntimeCameraOptionsSource(logger);
        
        // Initialize with safe default values for newer camera stack
        var initialOptions = new CameraOptions
        {
            Width = 1920,
            Height = 1080,
            Quality = 85,
            TimeoutMs = 15000,
            WarmupTimeMs = 2000,
            Rotation = 0,
            Brightness = 50,
            Contrast = 0,
            Saturation = 0,
            EnablePreview = false,
            DebugImagePath = "debug_images",
            // Safe defaults to avoid conflicts with newer camera stack
            Gain = 1.0,
            ShutterSpeedMicroseconds = 0,
            AutoExposure = true,
            AutoWhiteBalance = true
        };
        
        // Override with configuration values if present
        configuration.GetSection("Camera").Bind(initialOptions);
        source.UpdateOptions(initialOptions);
        
        return source;
    });
    
    // Register the runtime options source as the primary IOptionsMonitor<CameraOptions>
    services.AddSingleton<IOptionsMonitor<CameraOptions>>(provider => 
        provider.GetRequiredService<RuntimeCameraOptionsSource>());
    
    // Configure Camera options from configuration as fallback
    services.Configure<CameraOptions>(configuration.GetSection("Camera"));
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
