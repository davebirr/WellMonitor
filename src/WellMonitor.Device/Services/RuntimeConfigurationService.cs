using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services
{
    public interface IRuntimeConfigurationService
    {
        Task UpdateOcrOptionsAsync(OcrOptions newOptions);
        void SetInitialOcrOptions(OcrOptions options);
        Task UpdateDebugOptionsAsync(DebugOptions newOptions);
        void SetInitialDebugOptions(DebugOptions options);
        Task UpdateWebOptionsAsync(WebOptions newOptions);
        void SetInitialWebOptions(WebOptions options);
        Task UpdateCameraOptionsAsync(CameraOptions newOptions);
        void SetInitialCameraOptions(CameraOptions options);
    }

    /// <summary>
    /// Service that manages runtime configuration updates for OCR and Debug options
    /// This service allows the application to update configuration at runtime
    /// without requiring a restart, typically from device twin updates.
    /// </summary>
    public class RuntimeConfigurationService : IRuntimeConfigurationService
    {
        private readonly ILogger<RuntimeConfigurationService> _logger;
        private readonly RuntimeOcrOptionsSource _runtimeOcrOptionsSource;
        private readonly RuntimeDebugOptionsSource _runtimeDebugOptionsSource;
        private readonly RuntimeWebOptionsSource _runtimeWebOptionsSource;
        private readonly RuntimeCameraOptionsSource _runtimeCameraOptionsSource;

        public RuntimeConfigurationService(
            ILogger<RuntimeConfigurationService> logger,
            RuntimeOcrOptionsSource runtimeOcrOptionsSource,
            RuntimeDebugOptionsSource runtimeDebugOptionsSource,
            RuntimeWebOptionsSource runtimeWebOptionsSource,
            RuntimeCameraOptionsSource runtimeCameraOptionsSource)
        {
            _logger = logger;
            _runtimeOcrOptionsSource = runtimeOcrOptionsSource;
            _runtimeDebugOptionsSource = runtimeDebugOptionsSource;
            _runtimeWebOptionsSource = runtimeWebOptionsSource;
            _runtimeCameraOptionsSource = runtimeCameraOptionsSource;
        }

        public Task UpdateOcrOptionsAsync(OcrOptions newOptions)
        {
            _logger.LogInformation("Updating OCR options at runtime: Provider={Provider}, MinConfidence={MinConfidence}", 
                newOptions.Provider, newOptions.MinimumConfidence);
            _runtimeOcrOptionsSource.UpdateOptions(newOptions);
            return Task.CompletedTask;
        }

        public void SetInitialOcrOptions(OcrOptions options)
        {
            _logger.LogInformation("Setting initial OCR options: Provider={Provider}", options.Provider);
            _runtimeOcrOptionsSource.UpdateOptions(options);
        }

        public Task UpdateWebOptionsAsync(WebOptions newOptions)
        {
            _logger.LogInformation("Updating Web options at runtime: Port={Port}, NetworkAccess={NetworkAccess}, BindAddress={BindAddress}",
                newOptions.Port, newOptions.AllowNetworkAccess, newOptions.BindAddress);
            _runtimeWebOptionsSource.UpdateOptions(newOptions);
            return Task.CompletedTask;
        }

        public void SetInitialWebOptions(WebOptions options)
        {
            _logger.LogInformation("Setting initial Web options: Port={Port}, NetworkAccess={NetworkAccess}, BindAddress={BindAddress}",
                options.Port, options.AllowNetworkAccess, options.BindAddress);
            _runtimeWebOptionsSource.UpdateOptions(options);
        }

        public Task UpdateDebugOptionsAsync(DebugOptions newOptions)
        {
            _logger.LogInformation("Updating Debug options at runtime: DebugMode={DebugMode}, ImageSaveEnabled={ImageSaveEnabled}", 
                newOptions.DebugMode, newOptions.ImageSaveEnabled);
            
            _runtimeDebugOptionsSource.UpdateOptions(newOptions);
            return Task.CompletedTask;
        }

        public void SetInitialDebugOptions(DebugOptions options)
        {
            _logger.LogInformation("Setting initial Debug options: ImageSaveEnabled={ImageSaveEnabled}", options.ImageSaveEnabled);
            _runtimeDebugOptionsSource.UpdateOptions(options);
        }

        public Task UpdateCameraOptionsAsync(CameraOptions newOptions)
        {
            _logger.LogInformation("Updating Camera options at runtime: Resolution={Width}x{Height}, Gain={Gain}, ShutterSpeed={ShutterSpeed}μs, AutoExposure={AutoExposure}",
                newOptions.Width, newOptions.Height, newOptions.Gain, newOptions.ShutterSpeedMicroseconds, newOptions.AutoExposure);
            _runtimeCameraOptionsSource.UpdateOptions(newOptions);
            return Task.CompletedTask;
        }

        public void SetInitialCameraOptions(CameraOptions options)
        {
            _logger.LogInformation("Setting initial Camera options: Resolution={Width}x{Height}, Gain={Gain}, AutoExposure={AutoExposure}",
                options.Width, options.Height, options.Gain, options.AutoExposure);
            _runtimeCameraOptionsSource.UpdateOptions(options);
        }
    }

    /// <summary>
    /// Custom options source that allows runtime updates to OCR options
    /// This class works with the .NET Options pattern to provide dynamic configuration updates
    /// </summary>
    public class RuntimeOcrOptionsSource : IOptionsMonitor<OcrOptions>
    {
        private readonly ILogger<RuntimeOcrOptionsSource> _logger;
        private OcrOptions _currentOptions;
        private readonly List<IDisposable> _changeTokens = new();
        private readonly object _lock = new();

        public RuntimeOcrOptionsSource(ILogger<RuntimeOcrOptionsSource> logger)
        {
            _logger = logger;
            // Set reasonable defaults
            _currentOptions = new OcrOptions
            {
                Provider = "Tesseract",
                MinimumConfidence = 0.7,
                MaxRetryAttempts = 3,
                TimeoutSeconds = 30,
                EnablePreprocessing = true
            };
        }

        public OcrOptions CurrentValue => _currentOptions;

        public OcrOptions Get(string? name) => _currentOptions;

        public IDisposable? OnChange(Action<OcrOptions, string?> listener)
        {
            var disposable = new ChangeToken(() => listener(_currentOptions, null));
            lock (_lock)
            {
                _changeTokens.Add(disposable);
            }
            return disposable;
        }

        public void UpdateOptions(OcrOptions newOptions)
        {
            lock (_lock)
            {
                _currentOptions = newOptions;
                _logger.LogInformation("OCR options updated: Provider={Provider}, MinConfidence={MinConfidence}", 
                    newOptions.Provider, newOptions.MinimumConfidence);
                
                // Notify all listeners of the change
                foreach (var token in _changeTokens.ToList())
                {
                    if (token is ChangeToken changeToken)
                    {
                        changeToken.NotifyChange();
                    }
                }
            }
        }

        private class ChangeToken : IDisposable
        {
            private readonly Action _onChange;
            private bool _disposed;

            public ChangeToken(Action onChange)
            {
                _onChange = onChange;
            }

            public void NotifyChange()
            {
                if (!_disposed)
                {
                    _onChange?.Invoke();
                }
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Runtime source for DebugOptions that can be updated from device twin
    /// This class works with the .NET Options pattern to provide dynamic configuration updates
    /// </summary>
    public class RuntimeDebugOptionsSource : IOptionsMonitor<DebugOptions>
    {
        private readonly ILogger<RuntimeDebugOptionsSource> _logger;
        private DebugOptions _currentOptions;
        private readonly List<IDisposable> _changeTokens = new();
        private readonly object _lock = new();

        public RuntimeDebugOptionsSource(ILogger<RuntimeDebugOptionsSource> logger)
        {
            _logger = logger;
            // Set reasonable defaults
            _currentOptions = new DebugOptions
            {
                DebugMode = false,
                ImageSaveEnabled = false,
                ImageRetentionDays = 7,
                LogLevel = "Information",
                EnableVerboseOcrLogging = false
            };
        }

        public DebugOptions CurrentValue => _currentOptions;

        public DebugOptions Get(string? name) => _currentOptions;

        public IDisposable? OnChange(Action<DebugOptions, string?> listener)
        {
            var disposable = new ChangeToken(() => listener(_currentOptions, null));
            lock (_lock)
            {
                _changeTokens.Add(disposable);
            }
            return disposable;
        }

        public void UpdateOptions(DebugOptions newOptions)
        {
            lock (_lock)
            {
                _currentOptions = newOptions;
                _logger.LogInformation("Debug options updated: DebugMode={DebugMode}, ImageSaveEnabled={ImageSaveEnabled}, LogLevel={LogLevel}", 
                    newOptions.DebugMode, newOptions.ImageSaveEnabled, newOptions.LogLevel);
                
                // Notify all listeners of the change
                foreach (var token in _changeTokens.ToList())
                {
                    if (token is ChangeToken changeToken)
                    {
                        changeToken.NotifyChange();
                    }
                }
            }
        }

        private class ChangeToken : IDisposable
        {
            private readonly Action _onChange;
            private bool _disposed;

            public ChangeToken(Action onChange)
            {
                _onChange = onChange;
            }

            public void NotifyChange()
            {
                if (!_disposed)
                {
                    _onChange?.Invoke();
                }
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }
    }
}
