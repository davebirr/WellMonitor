using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WellMonitor.Device.Models;
using System.Diagnostics;
using System.Text.Json;

namespace WellMonitor.Device.Services;

/// <summary>
/// OCR provider that uses Python with Tesseract via subprocess for ARM64 compatibility
/// </summary>
public class PythonOcrProvider : IOcrProvider
{
    private readonly ILogger<PythonOcrProvider> _logger;
    private readonly IOptionsMonitor<OcrOptions> _ocrOptionsMonitor;
    private bool _isInitialized = false;
    private string? _pythonScriptPath;

    public string Name => "Python";
    public bool IsAvailable => _isInitialized;

    public PythonOcrProvider(ILogger<PythonOcrProvider> logger, IOptionsMonitor<OcrOptions> ocrOptionsMonitor)
    {
        _logger = logger;
        _ocrOptionsMonitor = ocrOptionsMonitor;
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing Python OCR provider...");

            // Create the Python OCR script
            _pythonScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ocr_service.py");
            await CreatePythonOcrScript(_pythonScriptPath);

            // Test Python and required packages
            if (!await TestPythonEnvironment())
            {
                _logger.LogError("Python environment test failed");
                return false;
            }

            _isInitialized = true;
            _logger.LogInformation("Python OCR provider initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Python OCR provider");
            return false;
        }
    }

    public async Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            return new OcrResult
            {
                Success = false,
                RawText = "",
                ProcessedText = "",
                Confidence = 0.0,
                Provider = Name,
                ErrorMessage = "Python OCR provider not initialized"
            };
        }

        try
        {
            // Save image to temporary file
            var tempImagePath = Path.GetTempFileName() + ".jpg";
            var tempOutputPath = Path.GetTempFileName() + ".json";

            try
            {
                // Save the image
                using (var fileStream = File.Create(tempImagePath))
                {
                    imageStream.Position = 0;
                    await imageStream.CopyToAsync(fileStream, cancellationToken);
                }

                // Run Python OCR script
                var result = await RunPythonOcrScript(tempImagePath, tempOutputPath, cancellationToken);
                
                if (result.IsSuccess && File.Exists(tempOutputPath))
                {
                    var json = await File.ReadAllTextAsync(tempOutputPath, cancellationToken);
                    var ocrData = JsonSerializer.Deserialize<PythonOcrResult>(json);
                    
                    return new OcrResult
                    {
                        Success = !string.IsNullOrWhiteSpace(ocrData?.Text),
                        RawText = ocrData?.Text ?? "",
                        ProcessedText = ocrData?.Text?.Trim() ?? "",
                        Confidence = ocrData?.Confidence ?? 0.0,
                        Provider = Name,
                        ProcessingDurationMs = result.ProcessingTimeMs
                    };
                }
                else
                {
                    _logger.LogWarning("Python OCR script failed: {Error}", result.Error);
                    return new OcrResult
                    {
                        Success = false,
                        RawText = "",
                        ProcessedText = "",
                        Confidence = 0.0,
                        Provider = Name,
                        ProcessingDurationMs = result.ProcessingTimeMs,
                        ErrorMessage = result.Error
                    };
                }
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
                    if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temporary files");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text using Python OCR");
            return new OcrResult
            {
                Success = false,
                RawText = "",
                ProcessedText = "",
                Confidence = 0.0,
                Provider = Name,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task CreatePythonOcrScript(string scriptPath)
    {
        var ocrOptions = _ocrOptionsMonitor.CurrentValue;
        var enablePreprocessing = ocrOptions.EnablePreprocessing.ToString().ToLower();
        var scaleFactor = ocrOptions.ImagePreprocessing.ScaleFactor.ToString("F1");
        var engineMode = ocrOptions.Tesseract.EngineMode.ToString();
        var psmMode = ocrOptions.Tesseract.PageSegmentationMode.ToString();
        var charWhitelist = ocrOptions.Tesseract.CustomConfig.GetValueOrDefault("tessedit_char_whitelist", "");
        var language = ocrOptions.Tesseract.Language;

        var pythonScript = @"#!/usr/bin/env python3
""""""
Python OCR Service for WellMonitor
Provides ARM64-compatible OCR using Python Tesseract bindings
""""""

import sys
import json
import time
import os
from pathlib import Path

try:
    import pytesseract
    from PIL import Image, ImageEnhance, ImageFilter
    import cv2
    import numpy as np
except ImportError as e:
    error_msg = f""Required Python package not installed: {e}""
    result = {
        ""text"": """",
        ""confidence"": 0.0,
        ""error"": error_msg,
        ""processing_time_ms"": 0
    }
    print(json.dumps(result))
    sys.exit(1)

def preprocess_image(image_path, output_path):
    """"""Preprocess image for better OCR results""""""
    try:
        # Read image with OpenCV
        img = cv2.imread(image_path)
        if img is None:
            raise Exception(f""Could not load image: {image_path}"")
        
        # Convert to grayscale
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        
        # Apply Gaussian blur to remove noise
        blurred = cv2.GaussianBlur(gray, (3, 3), 0)
        
        # Apply threshold to get binary image
        _, thresh = cv2.threshold(blurred, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
        
        # Morphological operations to clean up
        kernel = np.ones((2,2), np.uint8)
        cleaned = cv2.morphologyEx(thresh, cv2.MORPH_CLOSE, kernel)
        
        # Scale image for better OCR
        height, width = cleaned.shape
        scale_factor = " + scaleFactor + @"
        new_width = int(width * scale_factor)
        new_height = int(height * scale_factor)
        
        if scale_factor != 1.0:
            cleaned = cv2.resize(cleaned, (new_width, new_height), interpolation=cv2.INTER_CUBIC)
        
        # Save preprocessed image
        cv2.imwrite(output_path, cleaned)
        return output_path
        
    except Exception as e:
        # If preprocessing fails, use original image
        return image_path

def extract_text_with_tesseract(image_path):
    """"""Extract text using Tesseract OCR""""""
    try:
        # Preprocess image if enabled
        processed_image_path = image_path
        if " + enablePreprocessing + @":
            temp_processed = image_path.replace('.jpg', '_processed.jpg')
            processed_image_path = preprocess_image(image_path, temp_processed)
        
        # Configure Tesseract
        custom_config = r'--oem " + engineMode + " --psm " + psmMode + @"'
        
        char_whitelist = """ + (string.IsNullOrEmpty(charWhitelist) ? "None" : $"'{charWhitelist.Replace("'", "\\'")}'") + @"""
        if char_whitelist is not None:
            custom_config += f' -c tessedit_char_whitelist={char_whitelist}'
        
        # Extract text
        image = Image.open(processed_image_path)
        text = pytesseract.image_to_string(
            image, 
            lang='" + language + @"',
            config=custom_config
        ).strip()
        
        # Get confidence data
        data = pytesseract.image_to_data(
            image, 
            lang='" + language + @"',
            config=custom_config,
            output_type=pytesseract.Output.DICT
        )
        
        # Calculate average confidence (excluding -1 values)
        confidences = [int(conf) for conf in data['conf'] if int(conf) > 0]
        avg_confidence = sum(confidences) / len(confidences) if confidences else 0.0
        
        # Clean up preprocessed file
        if processed_image_path != image_path and os.path.exists(processed_image_path):
            os.remove(processed_image_path)
            
        return text, avg_confidence / 100.0  # Convert to 0-1 range
        
    except Exception as e:
        raise Exception(f""Tesseract OCR failed: {e}"")

def main():
    if len(sys.argv) != 3:
        print(""Usage: python ocr_service.py <image_path> <output_json_path>"")
        sys.exit(1)
    
    image_path = sys.argv[1]
    output_path = sys.argv[2]
    
    start_time = time.time()
    
    try:
        # Extract text
        text, confidence = extract_text_with_tesseract(image_path)
        
        processing_time = (time.time() - start_time) * 1000  # Convert to milliseconds
        
        # Prepare result
        result = {
            ""text"": text,
            ""confidence"": confidence,
            ""processing_time_ms"": round(processing_time, 2),
            ""python_version"": f""{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}"",
            ""tesseract_version"": str(pytesseract.get_tesseract_version())
        }
        
        # Write result to file
        with open(output_path, 'w') as f:
            json.dump(result, f, indent=2)
            
    except Exception as e:
        result = {
            ""text"": """",
            ""confidence"": 0.0,
            ""error"": str(e),
            ""processing_time_ms"": round((time.time() - start_time) * 1000, 2)
        }
        
        with open(output_path, 'w') as f:
            json.dump(result, f, indent=2)

if __name__ == ""__main__"":
    main()
";

        await File.WriteAllTextAsync(scriptPath, pythonScript);
        
        // Make script executable on Unix systems
        if (!OperatingSystem.IsWindows())
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x {scriptPath}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
        }
    }

    private async Task<bool> TestPythonEnvironment()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = "-c \"import pytesseract, PIL, cv2, numpy; print('OK')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && output.Trim() == "OK")
            {
                _logger.LogInformation("Python environment test passed");
                return true;
            }
            else
            {
                _logger.LogError("Python environment test failed. Output: {Output}, Error: {Error}", output, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Python environment");
            return false;
        }
    }

    private async Task<PythonOcrScriptResult> RunPythonOcrScript(string imagePath, string outputPath, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var ocrOptions = _ocrOptionsMonitor.CurrentValue;
        
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"\"{_pythonScriptPath}\" \"{imagePath}\" \"{outputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            
            // Simple timeout handling
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(ocrOptions.TimeoutSeconds), cancellationToken);
            var processTask = process.WaitForExitAsync(cancellationToken);
            
            var completedTask = await Task.WhenAny(processTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                process.Kill();
                return new PythonOcrScriptResult
                {
                    IsSuccess = false,
                    Error = $"OCR script timed out after {ocrOptions.TimeoutSeconds} seconds",
                    ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            if (process.ExitCode == 0)
            {
                return new PythonOcrScriptResult
                {
                    IsSuccess = true,
                    ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
            else
            {
                return new PythonOcrScriptResult
                {
                    IsSuccess = false,
                    Error = $"Script failed with exit code {process.ExitCode}. Error: {error}",
                    ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        }
        catch (Exception ex)
        {
            return new PythonOcrScriptResult
            {
                IsSuccess = false,
                Error = ex.Message,
                ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    public void Dispose()
    {
        // Clean up Python script file if needed
        try
        {
            if (!string.IsNullOrEmpty(_pythonScriptPath) && File.Exists(_pythonScriptPath))
            {
                File.Delete(_pythonScriptPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up Python script file");
        }
    }
}

/// <summary>
/// Result from Python OCR script execution
/// </summary>
internal class PythonOcrScriptResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public int ProcessingTimeMs { get; set; }
}

/// <summary>
/// JSON result from Python OCR script
/// </summary>
internal class PythonOcrResult
{
    public string Text { get; set; } = "";
    public double Confidence { get; set; }
    public string? Error { get; set; }
    public double ProcessingTimeMs { get; set; }
    public string? PythonVersion { get; set; }
    public string? TesseractVersion { get; set; }
}
