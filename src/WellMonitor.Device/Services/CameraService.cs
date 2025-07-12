using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Service for capturing images from the Raspberry Pi camera
    /// </summary>
    public class CameraService : ICameraService
    {
        private readonly ILogger<CameraService> _logger;
        private readonly CameraOptions _cameraOptions;
        private readonly IOptionsMonitor<DebugOptions> _debugOptions;

        public CameraService(ILogger<CameraService> logger, CameraOptions cameraOptions, IOptionsMonitor<DebugOptions> debugOptions)
        {
            _logger = logger;
            _cameraOptions = cameraOptions;
            _debugOptions = debugOptions;
        }

        /// <summary>
        /// Captures an image from the Raspberry Pi camera using libcamera-still
        /// </summary>
        /// <returns>Image data as byte array</returns>
        public async Task<byte[]> CaptureImageAsync()
        {
            try
            {
                _logger.LogDebug("Starting camera capture...");
                
                // Generate a temporary filename for the image
                var tempImagePath = Path.GetTempFileName() + ".jpg";
                
                try
                {
                    // Build the libcamera-still command
                    var arguments = BuildCameraArguments(tempImagePath);
                    
                    _logger.LogDebug("Executing camera command: libcamera-still {Arguments}", arguments);
                    
                    // Execute the camera command
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "libcamera-still",
                            Arguments = arguments,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    var startTime = DateTime.UtcNow;
                    process.Start();
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_cameraOptions.TimeoutMs));
                    
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogError("Camera capture timed out after {TimeoutMs}ms", _cameraOptions.TimeoutMs);
                        process.Kill();
                        throw new TimeoutException($"Camera capture timed out after {_cameraOptions.TimeoutMs}ms");
                    }

                    var duration = DateTime.UtcNow - startTime;
                    _logger.LogDebug("Camera capture completed in {Duration}ms", duration.TotalMilliseconds);

                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        _logger.LogError("Camera capture failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                        throw new InvalidOperationException($"Camera capture failed: {error}");
                    }

                    // Check if image file was created
                    if (!File.Exists(tempImagePath))
                    {
                        _logger.LogError("Camera capture completed but no image file was created");
                        throw new InvalidOperationException("Camera capture completed but no image file was created");
                    }

                    // Read the captured image
                    var imageBytes = await File.ReadAllBytesAsync(tempImagePath);
                    
                    if (imageBytes.Length == 0)
                    {
                        _logger.LogError("Camera capture created empty image file");
                        throw new InvalidOperationException("Camera capture created empty image file");
                    }

                    _logger.LogInformation("Successfully captured image: {Size} bytes", imageBytes.Length);

                    // Validate image quality to detect potential issues
                    await ValidateImageQuality(imageBytes, tempImagePath);

                    // Save debug copy if both debug mode is enabled AND debug path is configured
                    var debugOptions = _debugOptions.CurrentValue;
                    _logger.LogInformation("Debug image check: ImageSaveEnabled={Enabled}, DebugImagePath='{Path}'", 
                        debugOptions.ImageSaveEnabled, _cameraOptions.DebugImagePath ?? "NULL");
                    
                    if (debugOptions.ImageSaveEnabled && !string.IsNullOrEmpty(_cameraOptions.DebugImagePath))
                    {
                        _logger.LogInformation("Saving debug image...");
                        await SaveDebugImageAsync(imageBytes);
                    }
                    else if (debugOptions.ImageSaveEnabled && string.IsNullOrEmpty(_cameraOptions.DebugImagePath))
                    {
                        _logger.LogWarning("Debug image saving is enabled but cameraDebugImagePath is not configured in device twin");
                    }
                    else if (!debugOptions.ImageSaveEnabled)
                    {
                        _logger.LogInformation("Debug image saving is disabled (debugImageSaveEnabled=false)");
                    }

                    return imageBytes;
                }
                finally
                {
                    // Clean up temporary file
                    if (File.Exists(tempImagePath))
                    {
                        try
                        {
                            File.Delete(tempImagePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete temporary image file: {TempPath}", tempImagePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Camera capture failed");
                throw;
            }
        }

        /// <summary>
        /// Builds the command line arguments for libcamera-still
        /// </summary>
        private string BuildCameraArguments(string outputPath)
        {
            var args = new List<string>
            {
                "--output", $"\"{outputPath}\"",
                "--width", _cameraOptions.Width.ToString(),
                "--height", _cameraOptions.Height.ToString(),
                "--quality", _cameraOptions.Quality.ToString(),
                "--timeout", _cameraOptions.WarmupTimeMs.ToString(),
                "--encoding", "jpg"
            };

            // Add immediate flag only if warmup time is short (reduces grey square issues)
            if (_cameraOptions.WarmupTimeMs <= 2000)
            {
                args.Add("--immediate");
                _logger.LogDebug("Using immediate capture (warmup: {WarmupMs}ms)", _cameraOptions.WarmupTimeMs);
            }
            else
            {
                _logger.LogDebug("Using normal capture with warmup: {WarmupMs}ms", _cameraOptions.WarmupTimeMs);
            }

            // Add rotation if specified
            if (_cameraOptions.Rotation != 0)
            {
                args.Add("--rotation");
                args.Add(_cameraOptions.Rotation.ToString());
            }

            // Add brightness adjustment
            if (_cameraOptions.Brightness != 50)
            {
                args.Add("--brightness");
                args.Add((_cameraOptions.Brightness / 100.0).ToString("F2"));
            }

            // Add contrast adjustment
            if (_cameraOptions.Contrast != 0)
            {
                args.Add("--contrast");
                args.Add((_cameraOptions.Contrast / 100.0).ToString("F2"));
            }

            // Add saturation adjustment
            if (_cameraOptions.Saturation != 0)
            {
                args.Add("--saturation");
                args.Add((_cameraOptions.Saturation / 100.0).ToString("F2"));
            }

            // Disable preview unless explicitly enabled
            if (!_cameraOptions.EnablePreview)
            {
                args.Add("--nopreview");
            }

            return string.Join(" ", args);
        }

        /// <summary>
        /// Saves a debug copy of the captured image
        /// </summary>
        private async Task SaveDebugImageAsync(byte[] imageBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(_cameraOptions.DebugImagePath))
                    return;

                // Make path relative to application directory
                var debugDirectory = Path.IsPathRooted(_cameraOptions.DebugImagePath) 
                    ? _cameraOptions.DebugImagePath 
                    : Path.Combine(AppContext.BaseDirectory, _cameraOptions.DebugImagePath);

                // Create debug directory if it doesn't exist
                Directory.CreateDirectory(debugDirectory);

                // Create filename with timestamp
                var fileName = $"pump_reading_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
                var debugPath = Path.Combine(debugDirectory, fileName);

                await File.WriteAllBytesAsync(debugPath, imageBytes);
                _logger.LogInformation("Debug image saved to: {DebugPath}", debugPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save debug image");
            }
        }

        /// <summary>
        /// Validates image quality to detect common issues like grey squares
        /// </summary>
        private async Task ValidateImageQuality(byte[] imageBytes, string imagePath)
        {
            try
            {
                // Basic size validation
                if (imageBytes.Length < 5000) // Very small images are likely problematic
                {
                    _logger.LogWarning("Image quality warning: Very small image size ({Size} bytes) - may be grey square or minimal content", imageBytes.Length);
                }
                else if (imageBytes.Length > 1000000) // Unexpectedly large
                {
                    _logger.LogInformation("Image quality info: Large image size ({Size} bytes) - good detail capture", imageBytes.Length);
                }
                else
                {
                    _logger.LogDebug("Image quality info: Normal image size ({Size} bytes)", imageBytes.Length);
                }

                // Try to detect grey square by checking file header and basic properties
                if (imageBytes.Length >= 10)
                {
                    // Check JPEG header
                    if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                    {
                        _logger.LogDebug("Image validation: Valid JPEG header detected");
                    }
                    else
                    {
                        _logger.LogWarning("Image validation: Invalid or unexpected image format");
                    }
                }

                // Advanced validation using ImageSharp (if available) or system tools
                await ValidateImageContent(imagePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Image quality validation failed - continuing with capture");
            }
        }

        /// <summary>
        /// Performs advanced image content validation
        /// </summary>
        private async Task ValidateImageContent(string imagePath)
        {
            try
            {
                // Use 'file' command to get detailed image info if available
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "file",
                        Arguments = $"\"{imagePath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    _logger.LogDebug("Image file info: {FileInfo}", output.Trim());
                    
                    // Check for common indicators of issues
                    if (output.Contains("very short") || output.Contains("truncated"))
                    {
                        _logger.LogWarning("Image quality issue: File appears truncated or corrupted");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Advanced image validation unavailable - skipping");
            }
        }
    }
}
