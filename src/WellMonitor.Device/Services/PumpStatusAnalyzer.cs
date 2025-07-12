using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Devices.Client;
using WellMonitor.Shared.Models;
using WellMonitor.Device.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace WellMonitor.Device.Services;

/// <summary>
/// Service for analyzing pump readings and determining pump status
/// </summary>
public partial class PumpStatusAnalyzer(
    ILogger<PumpStatusAnalyzer> logger, 
    AlertOptions alertOptions, 
    IDeviceTwinService deviceTwinService,
    IConfiguration configuration)
{
    private readonly ILogger<PumpStatusAnalyzer> _logger = logger;
    private readonly AlertOptions _alertOptions = alertOptions;
    private readonly IDeviceTwinService _deviceTwinService = deviceTwinService;
    private readonly IConfiguration _configuration = configuration;
    
    // Cached configuration options (updated via device twin)
    private PumpAnalysisOptions _pumpAnalysisOptions = new();
    private PowerManagementOptions _powerManagementOptions = new();
    private StatusDetectionOptions _statusDetectionOptions = new();
    private DateTime _lastConfigUpdate = DateTime.MinValue;

    /// <summary>
    /// Updates configuration from device twin (called periodically by monitoring service)
    /// </summary>
    public async Task UpdateConfigurationAsync(DeviceClient deviceClient)
    {
        try
        {
            // Only update if enough time has passed (avoid too frequent calls)
            if (DateTime.UtcNow - _lastConfigUpdate < TimeSpan.FromMinutes(5))
                return;

            _pumpAnalysisOptions = await _deviceTwinService.FetchPumpAnalysisConfigAsync(deviceClient, _configuration, _logger);
            _powerManagementOptions = await _deviceTwinService.FetchPowerManagementConfigAsync(deviceClient, _configuration, _logger);
            _statusDetectionOptions = await _deviceTwinService.FetchStatusDetectionConfigAsync(deviceClient, _configuration, _logger);
            
            _lastConfigUpdate = DateTime.UtcNow;
            _logger.LogDebug("Pump analyzer configuration updated from device twin");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pump analyzer configuration from device twin");
        }
    }

    /// <summary>
    /// Gets current power management options (for monitoring service)
    /// </summary>
    public PowerManagementOptions GetPowerManagementOptions() => _powerManagementOptions;

    /// <summary>
    /// Analyzes OCR text and determines pump status and current reading
    /// </summary>
    public PumpReading AnalyzePumpReading(string ocrText, double ocrConfidence)
    {
        var pumpReading = new PumpReading
        {
            RawText = ocrText,
            Confidence = ocrConfidence,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>()
        };

        try
        {
            // Check for status messages first
            if (ContainsStatusMessage(ocrText, _statusDetectionOptions.DryKeywords))
            {
                pumpReading.Status = PumpStatus.Dry;
                pumpReading.CurrentAmps = null;
                pumpReading.IsValid = true;
                _logger.LogWarning("Dry status detected from OCR: {Text}", ocrText);
                return pumpReading;
            }

            if (ContainsStatusMessage(ocrText, _statusDetectionOptions.RapidCycleKeywords))
            {
                pumpReading.Status = PumpStatus.RapidCycle;
                pumpReading.CurrentAmps = null;
                pumpReading.IsValid = true;
                _logger.LogWarning("Rapid cycling status detected from OCR: {Text}", ocrText);
                return pumpReading;
            }

            // Try to extract numeric current reading
            var currentAmps = ExtractCurrentReading(ocrText);
            if (currentAmps.HasValue)
            {
                pumpReading.CurrentAmps = currentAmps.Value;
                pumpReading.Status = DetermineStatusFromCurrent(currentAmps.Value);
                pumpReading.IsValid = true;
                
                _logger.LogDebug("Current reading extracted: {Current}A, Status: {Status}", 
                    currentAmps.Value, pumpReading.Status);
            }
            else
            {
                // Could not extract meaningful data
                pumpReading.Status = PumpStatus.Unknown;
                pumpReading.IsValid = false;
                _logger.LogWarning("Could not extract current reading from OCR text: {Text}", ocrText);
            }

            return pumpReading;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing pump reading from OCR text: {Text}", ocrText);
            
            return new PumpReading
            {
                RawText = ocrText,
                Status = PumpStatus.Unknown,
                IsValid = false,
                Confidence = ocrConfidence,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { { "Error", ex.Message } }
            };
        }
    }

    private bool ContainsStatusMessage(string text, string[] keywords)
    {
        var comparison = _statusDetectionOptions.StatusMessageCaseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        return keywords.Any(keyword => text.Contains(keyword, comparison));
    }

    private double? ExtractCurrentReading(string text)
    {
        // Use compiled regex patterns for better performance
        Regex[] regexes = [TwoDecimalRegex(), OneDecimalRegex(), TrailingDotRegex(), WholeNumberRegex()];

        foreach (var regex in regexes)
        {
            var matches = regex.Matches(text);
            
            foreach (Match match in matches)
            {
                if (double.TryParse(match.Groups[1].Value, out double current))
                {
                    // Validate that the current reading is in a reasonable range
                    if (current >= 0 && current <= _pumpAnalysisOptions.MaxValidCurrent)
                    {
                        return current;
                    }
                }
            }
        }

        return null;
    }

    private PumpStatus DetermineStatusFromCurrent(double currentAmps)
    {
        // Use configurable thresholds from device twin
        if (currentAmps < _pumpAnalysisOptions.OffCurrentThreshold)
            return PumpStatus.Off;
        
        if (currentAmps < _pumpAnalysisOptions.IdleCurrentThreshold)
            return PumpStatus.Idle;
        
        if (currentAmps >= _pumpAnalysisOptions.NormalCurrentMin && 
            currentAmps <= _pumpAnalysisOptions.NormalCurrentMax)
            return PumpStatus.Normal;
        
        if (currentAmps > _pumpAnalysisOptions.HighCurrentThreshold)
            return PumpStatus.Unknown; // Potential overload - needs investigation
        
        return PumpStatus.Unknown;
    }
}

/// <summary>
/// Service for analyzing pump status from current readings and OCR text
/// </summary>
public partial class PumpStatusAnalyzer
{
    // Compiled regex patterns for better performance
    [GeneratedRegex(@"\b(\d{1,2}\.\d{1,2})\b", RegexOptions.Compiled)]
    private static partial Regex TwoDecimalRegex();
    
    [GeneratedRegex(@"\b(\d{1,2}\.\d)\b", RegexOptions.Compiled)]
    private static partial Regex OneDecimalRegex();
    
    [GeneratedRegex(@"\b(\d{1,2})\.\b", RegexOptions.Compiled)]
    private static partial Regex TrailingDotRegex();
    
    [GeneratedRegex(@"\b(\d{1,2})\b", RegexOptions.Compiled)]
    private static partial Regex WholeNumberRegex();
}
