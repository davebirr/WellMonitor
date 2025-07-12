using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WellMonitor.Device.Services;

/// <summary>
/// Service for diagnosing OCR installation and configuration issues
/// </summary>
public class OcrDiagnosticsService
{
    private readonly ILogger<OcrDiagnosticsService> _logger;

    public OcrDiagnosticsService(ILogger<OcrDiagnosticsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Run comprehensive OCR diagnostics
    /// </summary>
    public async Task RunDiagnosticsAsync()
    {
        _logger.LogInformation("Starting OCR diagnostics...");

        await CheckSystemInfoAsync();
        await CheckTesseractInstallationAsync();
        await CheckTessDataDirectoriesAsync();
        await CheckTesseractVersionAsync();
        await CheckTesseractLanguageDataAsync();

        _logger.LogInformation("OCR diagnostics completed");
    }

    private async Task CheckSystemInfoAsync()
    {
        try
        {
            var os = RuntimeInformation.OSDescription;
            var architecture = RuntimeInformation.OSArchitecture;
            var framework = RuntimeInformation.FrameworkDescription;

            _logger.LogInformation("System Information:");
            _logger.LogInformation("  OS: {OS}", os);
            _logger.LogInformation("  Architecture: {Architecture}", architecture);
            _logger.LogInformation("  Framework: {Framework}", framework);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system information");
        }

        await Task.CompletedTask;
    }

    private async Task CheckTesseractInstallationAsync()
    {
        try
        {
            _logger.LogInformation("Checking Tesseract installation...");

            var tesseractPaths = new[]
            {
                "tesseract",
                "/usr/bin/tesseract",
                "/usr/local/bin/tesseract",
                "/opt/homebrew/bin/tesseract"
            };

            foreach (var tesseractPath in tesseractPaths)
            {
                if (await CheckExecutableAsync(tesseractPath))
                {
                    _logger.LogInformation("  Found Tesseract at: {Path}", tesseractPath);
                    return;
                }
            }

            _logger.LogWarning("  Tesseract executable not found in standard locations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Tesseract installation");
        }
    }

    private async Task CheckTessDataDirectoriesAsync()
    {
        try
        {
            _logger.LogInformation("Checking tessdata directories...");

            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "tessdata"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tesseract", "tessdata"),
                "/usr/share/tesseract-ocr/5/tessdata",
                "/usr/share/tesseract-ocr/4.00/tessdata",
                "/usr/share/tesseract-ocr/tessdata",
                "/usr/share/tessdata",
                "/opt/homebrew/share/tessdata",
                "/usr/local/share/tessdata"
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "*.traineddata");
                    _logger.LogInformation("  Found tessdata directory: {Path} ({FileCount} files)", path, files.Length);
                    
                    if (files.Length > 0)
                    {
                        _logger.LogInformation("    Language files: {Languages}", 
                            string.Join(", ", files.Select(f => Path.GetFileNameWithoutExtension(f))));
                    }
                }
                else
                {
                    _logger.LogDebug("  tessdata directory not found: {Path}", path);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking tessdata directories");
        }

        await Task.CompletedTask;
    }

    private async Task CheckTesseractVersionAsync()
    {
        try
        {
            _logger.LogInformation("Checking Tesseract version...");

            var result = await RunCommandAsync("tesseract", "--version");
            if (result.Success)
            {
                _logger.LogInformation("  Tesseract version output: {Output}", result.Output.Trim());
            }
            else
            {
                _logger.LogWarning("  Failed to get Tesseract version: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Tesseract version");
        }
    }

    private async Task CheckTesseractLanguageDataAsync()
    {
        try
        {
            _logger.LogInformation("Checking Tesseract language data...");

            var result = await RunCommandAsync("tesseract", "--list-langs");
            if (result.Success)
            {
                _logger.LogInformation("  Available languages: {Languages}", result.Output.Trim());
            }
            else
            {
                _logger.LogWarning("  Failed to list Tesseract languages: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Tesseract language data");
        }
    }

    private async Task<bool> CheckExecutableAsync(string executable)
    {
        try
        {
            var result = await RunCommandAsync(executable, "--version");
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    private async Task<(bool Success, string Output, string Error)> RunCommandAsync(string command, string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            var output = await outputTask;
            var error = await errorTask;

            return (process.ExitCode == 0, output, error);
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }
}
