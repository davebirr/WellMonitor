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
    }

    /// <summary>
    /// Service that manages runtime configuration updates for OCR options
    /// This service allows the application to update OCR configuration at runtime
    /// without requiring a restart, typically from device twin updates.
    /// </summary>
    public class RuntimeConfigurationService : IRuntimeConfigurationService
    {
        private readonly ILogger<RuntimeConfigurationService> _logger;
        private readonly RuntimeOcrOptionsSource _runtimeOptionsSource;

        public RuntimeConfigurationService(
            ILogger<RuntimeConfigurationService> logger,
            RuntimeOcrOptionsSource runtimeOptionsSource)
        {
            _logger = logger;
            _runtimeOptionsSource = runtimeOptionsSource;
        }

        public Task UpdateOcrOptionsAsync(OcrOptions newOptions)
        {
            _logger.LogInformation("Updating OCR options at runtime: Provider={Provider}, MinConfidence={MinConfidence}", 
                newOptions.Provider, newOptions.MinimumConfidence);
            
            _runtimeOptionsSource.UpdateOptions(newOptions);
            return Task.CompletedTask;
        }

        public void SetInitialOcrOptions(OcrOptions options)
        {
            _logger.LogInformation("Setting initial OCR options: Provider={Provider}", options.Provider);
            _runtimeOptionsSource.UpdateOptions(options);
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
}
