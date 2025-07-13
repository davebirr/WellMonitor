using Microsoft.Extensions.Logging;
using WellMonitor.Device.Models;
using WellMonitor.Device.Services;

namespace WellMonitor.Device.Utilities;

/// <summary>
/// OCR testing service to validate OCR functionality with sample images
/// Provides diagnostic capabilities for OCR accuracy and performance
/// </summary>
public class OcrTestingService
{
    private readonly ILogger<OcrTestingService> _logger;
    private readonly IOcrService _ocrService;
    private readonly string _sampleImagesPath;

    public OcrTestingService(ILogger<OcrTestingService> logger, IOcrService ocrService)
    {
        _logger = logger;
        _ocrService = ocrService;
        _sampleImagesPath = Path.Combine(AppContext.BaseDirectory, "debug_images", "samples");
    }

    /// <summary>
    /// Test OCR accuracy against all sample images
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OCR test results</returns>
    public async Task<OcrTestResults> RunOcrTestsAsync(CancellationToken cancellationToken = default)
    {
        var results = new OcrTestResults();
        var testConditions = new[] { "normal", "idle", "dry", "rcyc", "off" };

        _logger.LogInformation("Starting OCR accuracy tests...");

        foreach (var condition in testConditions)
        {
            var conditionPath = Path.Combine(_sampleImagesPath, condition);
            
            if (!Directory.Exists(conditionPath))
            {
                _logger.LogWarning("Sample images directory not found: {Path}", conditionPath);
                continue;
            }

            var conditionResults = await TestConditionAsync(condition, conditionPath, cancellationToken);
            results.ConditionResults[condition] = conditionResults;
        }

        // Calculate overall statistics
        CalculateOverallStatistics(results);

        _logger.LogInformation("OCR accuracy tests completed. Overall accuracy: {Accuracy:P2}", 
            results.OverallAccuracy);

        return results;
    }

    /// <summary>
    /// Test OCR on a single image file
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OCR test result for the image</returns>
    public async Task<OcrTestResult> TestSingleImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        var result = new OcrTestResult
        {
            ImagePath = imagePath,
            FileName = Path.GetFileName(imagePath),
            TestTime = DateTime.UtcNow
        };

        try
        {
            // Extract text using OCR
            var ocrResult = await _ocrService.ExtractTextAsync(imagePath, cancellationToken);
            result.OcrResult = ocrResult;

            // Parse pump reading
            if (ocrResult.Success)
            {
                result.PumpReading = _ocrService.ParsePumpReading(ocrResult.ProcessedText);
                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = ocrResult.ErrorMessage;
            }

            _logger.LogDebug("OCR test for {FileName}: Success={Success}, Text='{Text}', Status={Status}",
                result.FileName, result.Success, ocrResult.ProcessedText, result.PumpReading?.Status);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "OCR test failed for {FileName}", result.FileName);
        }

        return result;
    }

    /// <summary>
    /// Generate OCR accuracy report
    /// </summary>
    /// <param name="results">OCR test results</param>
    /// <returns>Formatted report string</returns>
    public string GenerateAccuracyReport(OcrTestResults results)
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== OCR ACCURACY REPORT ===");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Overall Accuracy: {results.OverallAccuracy:P2}");
        report.AppendLine($"Total Images Tested: {results.TotalImagesProcessed}");
        report.AppendLine($"Average Processing Time: {results.AverageProcessingTime:F2}ms");
        report.AppendLine($"Average Confidence: {results.AverageConfidence:P2}");
        report.AppendLine();

        foreach (var condition in results.ConditionResults)
        {
            var conditionData = condition.Value;
            report.AppendLine($"--- {condition.Key.ToUpper()} CONDITION ---");
            report.AppendLine($"Images Tested: {conditionData.ImagesProcessed}");
            report.AppendLine($"Successful: {conditionData.SuccessfulTests}");
            report.AppendLine($"Failed: {conditionData.FailedTests}");
            report.AppendLine($"Accuracy: {conditionData.Accuracy:P2}");
            report.AppendLine($"Avg Processing Time: {conditionData.AverageProcessingTime:F2}ms");
            report.AppendLine($"Avg Confidence: {conditionData.AverageConfidence:P2}");
            
            if (conditionData.FailedTests > 0)
            {
                report.AppendLine("Failed Images:");
                foreach (var failedTest in conditionData.TestResults.Where(t => !t.Success))
                {
                    report.AppendLine($"  - {failedTest.FileName}: {failedTest.ErrorMessage}");
                }
            }
            
            report.AppendLine();
        }

        return report.ToString();
    }

    #region Private Methods

    /// <summary>
    /// Test OCR on all images in a condition directory
    /// </summary>
    private async Task<OcrConditionResults> TestConditionAsync(string condition, string conditionPath, CancellationToken cancellationToken)
    {
        var results = new OcrConditionResults { Condition = condition };
        var imageFiles = Directory.GetFiles(conditionPath, "*.jpg", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(conditionPath, "*.png", SearchOption.TopDirectoryOnly))
            .Concat(Directory.GetFiles(conditionPath, "*.jpeg", SearchOption.TopDirectoryOnly))
            .ToArray();

        _logger.LogInformation("Testing OCR on {Count} images for condition: {Condition}", imageFiles.Length, condition);

        foreach (var imageFile in imageFiles)
        {
            var testResult = await TestSingleImageAsync(imageFile, cancellationToken);
            results.TestResults.Add(testResult);
            
            results.ImagesProcessed++;
            if (testResult.Success)
            {
                results.SuccessfulTests++;
            }
            else
            {
                results.FailedTests++;
            }
        }

        // Calculate condition statistics
        if (results.TestResults.Count > 0)
        {
            results.Accuracy = (double)results.SuccessfulTests / results.ImagesProcessed;
            results.AverageProcessingTime = results.TestResults
                .Where(t => t.OcrResult != null)
                .Average(t => t.OcrResult!.ProcessingDurationMs);
            results.AverageConfidence = results.TestResults
                .Where(t => t.OcrResult != null && t.Success)
                .Average(t => t.OcrResult!.Confidence);
        }

        return results;
    }

    /// <summary>
    /// Calculate overall statistics from condition results
    /// </summary>
    private void CalculateOverallStatistics(OcrTestResults results)
    {
        if (results.ConditionResults.Count == 0)
        {
            return;
        }

        results.TotalImagesProcessed = results.ConditionResults.Values.Sum(c => c.ImagesProcessed);
        results.TotalSuccessfulTests = results.ConditionResults.Values.Sum(c => c.SuccessfulTests);
        results.TotalFailedTests = results.ConditionResults.Values.Sum(c => c.FailedTests);

        if (results.TotalImagesProcessed > 0)
        {
            results.OverallAccuracy = (double)results.TotalSuccessfulTests / results.TotalImagesProcessed;
        }

        var allProcessingTimes = results.ConditionResults.Values
            .SelectMany(c => c.TestResults)
            .Where(t => t.OcrResult != null)
            .Select(t => t.OcrResult!.ProcessingDurationMs)
            .ToArray();

        if (allProcessingTimes.Length > 0)
        {
            results.AverageProcessingTime = allProcessingTimes.Average();
        }

        var allConfidences = results.ConditionResults.Values
            .SelectMany(c => c.TestResults)
            .Where(t => t.OcrResult != null && t.Success)
            .Select(t => t.OcrResult!.Confidence)
            .ToArray();

        if (allConfidences.Length > 0)
        {
            results.AverageConfidence = allConfidences.Average();
        }
    }

    #endregion
}

/// <summary>
/// Overall OCR test results
/// </summary>
public class OcrTestResults
{
    public Dictionary<string, OcrConditionResults> ConditionResults { get; set; } = new();
    public int TotalImagesProcessed { get; set; }
    public int TotalSuccessfulTests { get; set; }
    public int TotalFailedTests { get; set; }
    public double OverallAccuracy { get; set; }
    public double AverageProcessingTime { get; set; }
    public double AverageConfidence { get; set; }
    public DateTime TestTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// OCR test results for a specific condition
/// </summary>
public class OcrConditionResults
{
    public string Condition { get; set; } = string.Empty;
    public List<OcrTestResult> TestResults { get; set; } = new();
    public int ImagesProcessed { get; set; }
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
    public double Accuracy { get; set; }
    public double AverageProcessingTime { get; set; }
    public double AverageConfidence { get; set; }
}

/// <summary>
/// OCR test result for a single image
/// </summary>
public class OcrTestResult
{
    public string ImagePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public OcrResult? OcrResult { get; set; }
    public PumpReading? PumpReading { get; set; }
    public DateTime TestTime { get; set; }
}
