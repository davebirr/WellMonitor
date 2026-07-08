using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WellMonitor.Device.Models;
using WellMonitor.Device.Controllers;

namespace WellMonitor.Device.Services
{
    /// <summary>
    /// Service for capturing images from the Raspberry Pi camera
    /// </summary>
    public class CameraService : ICameraService
    {
        private readonly ILogger<CameraService> _logger;
        private readonly IOptionsMonitor<CameraOptions> _cameraOptions;
        private readonly IOptionsMonitor<DebugOptions> _debugOptions;

        public CameraService(ILogger<CameraService> logger, IOptionsMonitor<CameraOptions> cameraOptions, IOptionsMonitor<DebugOptions> debugOptions)
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
                    // Build the camera command arguments
                    var arguments = BuildCameraArguments(tempImagePath);
                    
                    // Try libcamera-still first, then rpicam-still as fallback
                    var success = await TryCameraCommand("libcamera-still", arguments, tempImagePath);
                    if (!success)
                    {
                        _logger.LogWarning("libcamera-still failed, trying rpicam-still as fallback...");
                        success = await TryCameraCommand("rpicam-still", arguments, tempImagePath);
                        if (!success)
                        {
                            throw new InvalidOperationException("Both libcamera-still and rpicam-still failed to capture image");
                        }
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
                    var cameraOptions = _cameraOptions.CurrentValue;
                    _logger.LogInformation("Debug image check: ImageSaveEnabled={Enabled}, DebugImagePath='{Path}'", 
                        debugOptions.ImageSaveEnabled, cameraOptions.DebugImagePath ?? "NULL");
                    
                    if (debugOptions.ImageSaveEnabled && !string.IsNullOrEmpty(cameraOptions.DebugImagePath))
                    {
                        _logger.LogInformation("Saving debug image...");
                        await SaveDebugImageAsync(imageBytes);
                    }
                    else if (debugOptions.ImageSaveEnabled && string.IsNullOrEmpty(cameraOptions.DebugImagePath))
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
            var cameraOptions = _cameraOptions.CurrentValue;
            
            var args = new List<string>
            {
                "--output", $"\"{outputPath}\"",
                "--width", cameraOptions.Width.ToString(),
                "--height", cameraOptions.Height.ToString(),
                "--quality", cameraOptions.Quality.ToString(),
                "--timeout", cameraOptions.WarmupTimeMs.ToString(),
                "--encoding", "jpg"
            };

            // Add immediate flag only if warmup time is short (reduces grey square issues)
            if (cameraOptions.WarmupTimeMs <= 2000)
            {
                args.Add("--immediate");
                _logger.LogDebug("Using immediate capture (warmup: {WarmupMs}ms)", cameraOptions.WarmupTimeMs);
            }
            else
            {
                _logger.LogDebug("Using normal capture with warmup: {WarmupMs}ms", cameraOptions.WarmupTimeMs);
            }

            // Add rotation if specified
            if (cameraOptions.Rotation != 0)
            {
                args.Add("--rotation");
                args.Add(cameraOptions.Rotation.ToString());
            }

            // Add brightness adjustment
            if (cameraOptions.Brightness != 50)
            {
                args.Add("--brightness");
                args.Add((cameraOptions.Brightness / 100.0).ToString("F2"));
            }

            // Add contrast adjustment
            if (cameraOptions.Contrast != 0)
            {
                args.Add("--contrast");
                args.Add((cameraOptions.Contrast / 100.0).ToString("F2"));
            }

            // Add saturation adjustment
            if (cameraOptions.Saturation != 0)
            {
                args.Add("--saturation");
                args.Add((cameraOptions.Saturation / 100.0).ToString("F2"));
            }

            // Add gain/ISO for low light situations
            if (cameraOptions.Gain > 1.0)
            {
                args.Add("--gain");
                args.Add(cameraOptions.Gain.ToString("F1"));
                _logger.LogDebug("Using high gain for low light: {Gain}", cameraOptions.Gain);
            }

            // Add shutter speed for low light (manual exposure)
            if (cameraOptions.ShutterSpeedMicroseconds > 0)
            {
                args.Add("--shutter");
                args.Add(cameraOptions.ShutterSpeedMicroseconds.ToString());
                _logger.LogDebug("Using manual shutter speed: {ShutterSpeed}μs", cameraOptions.ShutterSpeedMicroseconds);
            }

            // Handle exposure mode based on configuration
            if (cameraOptions.ExposureMode != CameraExposureMode.Auto)
            {
                // Use the configured exposure mode
                var exposureMode = cameraOptions.ExposureMode.ToString().ToLowerInvariant();
                
                // Handle special case for FixedFps (needs to be "fixedfps" not "fixedfps")
                if (cameraOptions.ExposureMode == CameraExposureMode.FixedFps)
                    exposureMode = "fixedfps";
                
                args.Add("--exposure");
                args.Add(exposureMode);
                _logger.LogDebug("Using configured exposure mode: {ExposureMode}", exposureMode);
            }
            else if (cameraOptions.ShutterSpeedMicroseconds > 0)
            {
                // When using manual shutter speed, use 'normal' mode instead of 'barcode' for compatibility
                args.Add("--exposure");
                args.Add("normal");
                _logger.LogDebug("Manual shutter speed set, using normal exposure mode for LED displays");
            }
            else if (!cameraOptions.AutoExposure)
            {
                // For non-auto exposure, use 'normal' mode as fallback
                args.Add("--exposure");
                args.Add("normal");
                _logger.LogDebug("Auto exposure disabled, using normal exposure mode as fallback");
            }

            // Disable auto white balance if specified (useful for LED color consistency)
            if (!cameraOptions.AutoWhiteBalance)
            {
                args.Add("--awb");
                args.Add("auto");  // Use 'auto' instead of 'off' for compatibility
                _logger.LogDebug("Auto white balance set to auto mode for LED color consistency");
            }

            // Disable preview unless explicitly enabled
            if (!cameraOptions.EnablePreview)
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
                var cameraOptions = _cameraOptions.CurrentValue;
                if (string.IsNullOrEmpty(cameraOptions.DebugImagePath))
                    return;

                // Make path relative to application directory
                var debugDirectory = Path.IsPathRooted(cameraOptions.DebugImagePath) 
                    ? cameraOptions.DebugImagePath 
                    : Path.Combine(AppContext.BaseDirectory, cameraOptions.DebugImagePath);

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

        /// <summary>
        /// Tries to execute a camera command with the given arguments
        /// </summary>
        /// <param name="command">Camera command (libcamera-still or rpicam-still)</param>
        /// <param name="arguments">Command arguments</param>
        /// <param name="expectedOutputPath">Expected output file path</param>
        /// <returns>True if successful, false otherwise</returns>
        private async Task<bool> TryCameraCommand(string command, string arguments, string expectedOutputPath)
        {
            try
            {
                var cameraOptions = _cameraOptions.CurrentValue;
                _logger.LogDebug("Executing camera command: {Command} {Arguments}", command, arguments);
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                var startTime = DateTime.UtcNow;
                process.Start();
                
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cameraOptions.TimeoutMs));
                
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError("{Command} capture timed out after {TimeoutMs}ms", command, cameraOptions.TimeoutMs);
                    process.Kill();
                    return false;
                }

                var duration = DateTime.UtcNow - startTime;
                _logger.LogDebug("{Command} capture completed in {Duration}ms", command, duration.TotalMilliseconds);

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning("{Command} failed with exit code {ExitCode}: {Error}", command, process.ExitCode, error);
                    return false;
                }

                // Check if image file was created
                if (!File.Exists(expectedOutputPath))
                {
                    _logger.LogWarning("{Command} completed but no image file was created", command);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Command} execution failed", command);
                return false;
            }
        }

        /// <summary>
        /// Gets the current camera configuration
        /// </summary>
        /// <returns>Current camera options</returns>
        public async Task<CameraOptions?> GetCurrentConfigurationAsync()
        {
            try
            {
                await Task.CompletedTask; // Async signature for future extensibility
                return _cameraOptions.CurrentValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get camera configuration");
                return null;
            }
        }

        /// <summary>
        /// Captures a test image with current camera settings
        /// </summary>
        /// <returns>Result of the capture operation</returns>
        public async Task<CameraOperationResult> CaptureTestImageAsync()
        {
            try
            {
                _logger.LogInformation("Capturing test image with current camera settings");

                // Ensure debug images directory exists
                var cameraOptions = _cameraOptions.CurrentValue;
                var debugPath = cameraOptions.DebugImagePath ?? "/tmp/wellmonitor-debug";
                
                if (!Directory.Exists(debugPath))
                {
                    Directory.CreateDirectory(debugPath);
                }

                // Generate a filename for the test image
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var testImagePath = Path.Combine(debugPath, $"test_exposure_{timestamp}.jpg");

                // Build the camera command arguments
                var arguments = BuildCameraArguments(testImagePath);

                // Try libcamera-still first, then rpicam-still as fallback
                var success = await TryCameraCommand("libcamera-still", arguments, testImagePath);
                if (!success)
                {
                    _logger.LogWarning("libcamera-still failed, trying rpicam-still as fallback...");
                    success = await TryCameraCommand("rpicam-still", arguments, testImagePath);
                }

                if (success)
                {
                    _logger.LogInformation("Test image captured successfully: {ImagePath}", testImagePath);
                    return new CameraOperationResult
                    {
                        Success = true,
                        ImagePath = testImagePath
                    };
                }
                else
                {
                    var errorMessage = "Failed to capture test image with both libcamera-still and rpicam-still";
                    _logger.LogError(errorMessage);
                    return new CameraOperationResult
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing test image");
                return new CameraOperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
