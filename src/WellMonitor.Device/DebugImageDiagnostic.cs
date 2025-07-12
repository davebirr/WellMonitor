using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WellMonitor.Device.Models;

namespace WellMonitor.Device;

/// <summary>
/// Diagnostic tool to help troubleshoot debug image saving issues
/// </summary>
public class DebugImageDiagnostic
{
    private readonly ILogger<DebugImageDiagnostic> _logger;
    private readonly IOptionsMonitor<DebugOptions> _debugOptions;
    private readonly CameraOptions _cameraOptions;

    public DebugImageDiagnostic(
        ILogger<DebugImageDiagnostic> logger,
        IOptionsMonitor<DebugOptions> debugOptions,
        CameraOptions cameraOptions)
    {
        _logger = logger;
        _debugOptions = debugOptions;
        _cameraOptions = cameraOptions;
    }

    /// <summary>
    /// Diagnose debug image configuration and report issues
    /// </summary>
    public void DiagnoseDebugImageConfiguration()
    {
        _logger.LogInformation("=== DEBUG IMAGE CONFIGURATION DIAGNOSTIC ===");

        // Check DebugOptions
        var debugOptions = _debugOptions.CurrentValue;
        _logger.LogInformation("DebugOptions.ImageSaveEnabled: {Enabled}", debugOptions.ImageSaveEnabled);
        _logger.LogInformation("DebugOptions.DebugMode: {DebugMode}", debugOptions.DebugMode);
        _logger.LogInformation("DebugOptions.ImageRetentionDays: {RetentionDays}", debugOptions.ImageRetentionDays);

        // Check CameraOptions
        _logger.LogInformation("CameraOptions.DebugImagePath: '{DebugPath}'", _cameraOptions.DebugImagePath ?? "NULL");

        // Determine debug image path
        if (!string.IsNullOrEmpty(_cameraOptions.DebugImagePath))
        {
            var debugDirectory = Path.IsPathRooted(_cameraOptions.DebugImagePath) 
                ? _cameraOptions.DebugImagePath 
                : Path.Combine(AppContext.BaseDirectory, _cameraOptions.DebugImagePath);
            
            _logger.LogInformation("Resolved debug directory path: {DebugDirectory}", debugDirectory);
            _logger.LogInformation("Debug directory exists: {Exists}", Directory.Exists(debugDirectory));
            
            if (Directory.Exists(debugDirectory))
            {
                var files = Directory.GetFiles(debugDirectory, "*.jpg");
                _logger.LogInformation("Number of existing debug images: {Count}", files.Length);
                if (files.Length > 0)
                {
                    var latestFile = files.OrderByDescending(f => File.GetCreationTime(f)).First();
                    _logger.LogInformation("Latest debug image: {LatestFile} (Created: {CreatedTime})", 
                        Path.GetFileName(latestFile), File.GetCreationTime(latestFile));
                }
            }
            else
            {
                _logger.LogWarning("Debug directory does not exist: {DebugDirectory}", debugDirectory);
                try
                {
                    Directory.CreateDirectory(debugDirectory);
                    _logger.LogInformation("Created debug directory: {DebugDirectory}", debugDirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create debug directory: {DebugDirectory}", debugDirectory);
                }
            }
        }

        // Provide recommendations
        _logger.LogInformation("=== RECOMMENDATIONS ===");
        
        if (!debugOptions.ImageSaveEnabled)
        {
            _logger.LogWarning("❌ Debug image saving is DISABLED. Set 'debugImageSaveEnabled': true in device twin desired properties");
        }
        else
        {
            _logger.LogInformation("✅ Debug image saving is ENABLED");
        }

        if (string.IsNullOrEmpty(_cameraOptions.DebugImagePath))
        {
            _logger.LogWarning("❌ Debug image path is NOT SET. Set 'cameraDebugImagePath': 'debug_images' in device twin desired properties");
        }
        else
        {
            _logger.LogInformation("✅ Debug image path is configured: {DebugPath}", _cameraOptions.DebugImagePath);
        }

        bool canSaveDebugImages = debugOptions.ImageSaveEnabled && !string.IsNullOrEmpty(_cameraOptions.DebugImagePath);
        _logger.LogInformation("Can save debug images: {CanSave}", canSaveDebugImages ? "YES" : "NO");

        if (canSaveDebugImages)
        {
            _logger.LogInformation("✅ Debug image saving should work. Check monitoring logs for actual image capture attempts.");
            _logger.LogInformation("💡 Tip: Set log level to 'Debug' to see detailed camera capture logs");
        }

        _logger.LogInformation("=== END DIAGNOSTIC ===");
    }
}
