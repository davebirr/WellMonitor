# OCR Implementation Documentation

## Overview

The Well Monitor OCR (Optical Character Recognition) system provides enterprise-grade text extraction from LED display images. It features dual-provider architecture with local Raspberry Pi processing and cloud-based Azure processing, dynamic configuration via device twin, and comprehensive quality validation.

## Architecture

### Dual-Provider System

The OCR system supports two providers that can be switched dynamically:

1. **Tesseract OCR Provider** (Local)
   - Runs entirely on Raspberry Pi
   - No network dependency
   - Optimized for LED display recognition
   - Lower processing power requirements
   - Faster response times

2. **Azure Cognitive Services Provider** (Cloud)
   - Advanced AI-powered text recognition
   - Higher accuracy for complex scenarios
   - Requires internet connectivity
   - More processing power and capabilities
   - Better for unclear or damaged displays

### Service Architecture

```
IDynamicOcrService (Device Twin Integration)
    ↓
IOcrService (Main OCR orchestration)
    ↓
IOcrProvider (Tesseract | Azure)
    ↓
Image Processing Pipeline
    ↓
PumpReading Result
```

## Key Features

### 1. Dynamic Provider Selection
- Remote configuration via Azure IoT Hub device twin
- Hot-swappable providers without restart
- Automatic fallback between providers
- Provider availability testing

### 2. Advanced Image Processing
- Grayscale conversion for LED displays
- Adaptive thresholding for contrast enhancement
- Noise reduction and image sharpening
- Configurable preprocessing parameters

### 3. Quality Validation
- Confidence scoring for OCR results
- Pattern matching for expected formats
- Retry logic with different preprocessing
- Statistics tracking for performance monitoring

### 4. Enterprise Reliability
- Comprehensive error handling
- Detailed logging with structured data
- Performance metrics and telemetry
- Graceful degradation on failures

## Configuration

### Device Twin Properties

Configure OCR settings remotely via Azure IoT Hub device twin:

```json
{
  "desired": {
    "ocrProvider": "Tesseract",
    "ocrMinimumConfidence": 0.7,
    "ocrTesseractLanguage": "eng",
    "ocrAzureEndpoint": "https://your-region.api.cognitive.microsoft.com/",
    "ocrAzureKey": "your-key-here",
    "ocrImagePreprocessing": {
      "enableGrayscale": true,
      "enableThresholding": true,
      "thresholdValue": 128,
      "enableNoiseReduction": true,
      "enableSharpening": true
    },
    "ocrRetrySettings": {
      "maxRetries": 3,
      "retryDelayMs": 1000
    }
  }
}
```

### Local Configuration (appsettings.json)

```json
{
  "OCR": {
    "Provider": "Tesseract",
    "MinimumConfidence": 0.7,
    "TesseractLanguage": "eng",
    "AzureEndpoint": "",
    "AzureKey": "",
    "ImagePreprocessing": {
      "EnableGrayscale": true,
      "EnableThresholding": true,
      "ThresholdValue": 128,
      "EnableNoiseReduction": true,
      "EnableSharpening": true
    },
    "RetrySettings": {
      "MaxRetries": 3,
      "RetryDelayMs": 1000
    }
  }
}
```

## Usage

### Basic OCR Processing

```csharp
// Inject IDynamicOcrService
private readonly IDynamicOcrService _dynamicOcrService;

// Process image
var imageBytes = await _cameraService.CaptureImageAsync();
var result = await _dynamicOcrService.ProcessImageAsync(imageBytes, cancellationToken);

// Handle result
if (result.Success)
{
    _logger.LogInformation("Current draw: {CurrentDraw}A, Status: {Status}", 
        result.CurrentDrawAmps, result.Status);
}
```

### Dynamic Configuration Updates

```csharp
// Update OCR configuration from device twin
var newOptions = new OcrOptions
{
    Provider = "Azure",
    MinimumConfidence = 0.8,
    AzureEndpoint = "https://eastus.api.cognitive.microsoft.com/",
    AzureKey = "new-key"
};

var success = await _dynamicOcrService.UpdateConfigurationAsync(newOptions);
```

### Provider Availability Testing

```csharp
// Test which providers are available
var availability = await _dynamicOcrService.TestProviderAvailabilityAsync();
foreach (var (provider, available) in availability)
{
    _logger.LogInformation("Provider {Provider}: {Status}", 
        provider, available ? "Available" : "Unavailable");
}
```

## Data Models

### OcrOptions
Configuration options for OCR processing:
- `Provider`: "Tesseract" or "Azure"
- `MinimumConfidence`: Quality threshold (0.0-1.0)
- `TesseractLanguage`: Language code for Tesseract
- `AzureEndpoint`: Azure Cognitive Services endpoint
- `AzureKey`: Azure service key
- `ImagePreprocessing`: Preprocessing settings
- `RetrySettings`: Retry configuration

### PumpReading
Result of OCR processing:
- `Success`: Processing success flag
- `CurrentDrawAmps`: Extracted current reading
- `Status`: Pump status ("Normal", "Dry", "rcyc")
- `ConfidenceScore`: OCR confidence (0.0-1.0)
- `RawText`: Raw extracted text
- `ProcessingTimeMs`: Processing duration
- `Provider`: Provider used for processing

### OcrStatistics
Performance statistics:
- `TotalProcessed`: Total images processed
- `SuccessfulExtractions`: Successful extractions
- `FailedExtractions`: Failed extractions
- `AverageConfidence`: Average confidence score
- `AverageProcessingTime`: Average processing time
- `ProviderUsage`: Usage statistics by provider

## Error Handling

### Common Scenarios

1. **Provider Unavailable**
   - Automatic fallback to alternative provider
   - Logged with appropriate error level
   - Statistics updated

2. **Low Confidence Results**
   - Retry with different preprocessing
   - Fallback to alternative provider
   - Quality validation alerts

3. **Network Issues** (Azure Provider)
   - Automatic fallback to Tesseract
   - Offline mode handling
   - Connection retry logic

4. **Image Processing Errors**
   - Comprehensive error logging
   - Graceful degradation
   - Statistics tracking

## Performance Optimization

### Tesseract Optimization
- Preload language models
- Optimize image preprocessing
- Use appropriate page segmentation mode
- Limit search area for better performance

### Azure Optimization
- Batch processing for multiple images
- Optimize image size and format
- Use appropriate Azure SKU
- Implement connection pooling

## Monitoring and Telemetry

### Key Metrics
- OCR success/failure rates
- Processing times by provider
- Confidence score distributions
- Provider availability status
- Error rates and types

### Telemetry Integration
```csharp
// OCR statistics are included in telemetry
var telemetryData = new
{
    ocrProvider = currentProvider,
    ocrSuccessRate = statistics.SuccessRate,
    ocrAverageConfidence = statistics.AverageConfidence,
    ocrProcessingTime = statistics.AverageProcessingTime
};
```

## Best Practices

### 1. Provider Selection
- Use Tesseract for stable, fast processing
- Use Azure for complex or damaged displays
- Configure fallback chains appropriately
- Test provider availability regularly

### 2. Image Quality
- Ensure proper camera positioning
- Optimize lighting conditions
- Use appropriate image resolution
- Implement image quality validation

### 3. Configuration Management
- Use device twin for remote configuration
- Validate configuration changes
- Implement gradual rollouts
- Monitor configuration impact

### 4. Performance Monitoring
- Track OCR performance metrics
- Set up alerts for degradation
- Monitor provider availability
- Analyze failure patterns

## Troubleshooting

### Common Issues

1. **Low OCR Accuracy**
   - Check image quality and lighting
   - Adjust preprocessing parameters
   - Verify provider configuration
   - Test with different providers

2. **Provider Failures**
   - Verify network connectivity (Azure)
   - Check service credentials
   - Validate endpoint URLs
   - Test provider availability

3. **Performance Issues**
   - Monitor processing times
   - Check resource utilization
   - Optimize image preprocessing
   - Consider provider switching

### Debug Logging
Enable detailed logging for troubleshooting:
```json
{
  "Logging": {
    "LogLevel": {
      "WellMonitor.Device.Services": "Debug"
    }
  }
}
```

## Future Enhancements

### Planned Features
1. **Multi-language Support**
   - Dynamic language detection
   - Support for multiple OCR languages
   - Language-specific optimizations

2. **Advanced Image Processing**
   - Automatic image rotation
   - Perspective correction
   - Advanced noise reduction

3. **ML-based Optimization**
   - Automatic parameter tuning
   - Quality prediction models
   - Adaptive preprocessing

4. **Enhanced Monitoring**
   - Real-time performance dashboards
   - Predictive failure detection
   - Automated optimization recommendations

## Integration Points

### Camera Service Integration
```csharp
// Camera service provides images for OCR
var imageBytes = await _cameraService.CaptureImageAsync();
var result = await _ocrService.ProcessImageAsync(imageBytes);
```

### Telemetry Service Integration
```csharp
// OCR results are included in telemetry
await _telemetryService.SendReadingAsync(new Reading
{
    CurrentDrawAmps = result.CurrentDrawAmps,
    Status = result.Status,
    ConfidenceScore = result.ConfidenceScore,
    Provider = result.Provider
});
```

### Device Twin Integration
```csharp
// Device twin handles OCR configuration
await _deviceTwinService.FetchAndApplyOcrConfigAsync();
await _deviceTwinService.ReportOcrStatusAsync(statistics);
```

This comprehensive OCR implementation provides the foundation for reliable, enterprise-grade text extraction from LED displays with full remote configuration and monitoring capabilities.
- **Error Handling**: Comprehensive error recovery and logging
- **Statistics Tracking**: Performance metrics and accuracy monitoring
- **Configuration Flexibility**: Extensive configuration options

### 🚀 **Performance Features**
- **Concurrent Processing**: Multi-threaded OCR operations
- **Memory Optimization**: Efficient stream processing
- **Caching**: Provider initialization caching
- **Timeout Management**: Configurable operation timeouts

## Architecture

```
┌─────────────────────────────────────┐
│            OcrService               │
│  (Main orchestrator with fallback)  │
└─────────────────┬───────────────────┘
                  │
         ┌────────┴────────┐
         │                 │
┌────────▼────────┐ ┌──────▼──────────┐
│ TesseractOcr    │ │ AzureCognitive  │
│ Provider        │ │ ServicesOcr     │
│ (Primary)       │ │ Provider        │
│                 │ │ (Fallback)      │
└─────────────────┘ └─────────────────┘
```

## Configuration

### Basic Configuration (`appsettings.json`)

```json
{
  "OCR": {
    "Provider": "Tesseract",
    "MinimumConfidence": 0.7,
    "MaxRetryAttempts": 3,
    "TimeoutSeconds": 30,
    "EnablePreprocessing": true
  }
}
```

### Advanced Tesseract Configuration

```json
{
  "OCR": {
    "Tesseract": {
      "Language": "eng",
      "EngineMode": 3,
      "PageSegmentationMode": 7,
      "CustomConfig": {
        "tessedit_char_whitelist": "0123456789.DryAMPSrcyc ",
        "tessedit_unrej_any_wd": "1"
      }
    }
  }
}
```

### Azure Cognitive Services Configuration

```json
{
  "OCR": {
    "AzureCognitiveServices": {
      "Endpoint": "https://your-cognitive-service.cognitiveservices.azure.com/",
      "Region": "eastus",
      "UseReadApi": true,
      "MaxPollingAttempts": 10,
      "PollingIntervalMs": 500
    }
  }
}
```

## Usage

### Basic OCR Processing

```csharp
// Inject OCR service
private readonly IOcrService _ocrService;

// Process image from file
var result = await _ocrService.ExtractTextAsync(imagePath);

// Process image from bytes
var result = await _ocrService.ExtractTextAsync(imageBytes);

// Parse pump reading
var pumpReading = _ocrService.ParsePumpReading(result.ProcessedText);
```

### High-Level Processing

```csharp
// Process image and get pump reading in one call
var pumpReading = await _ocrService.ProcessImageAsync(imageBytes);

Console.WriteLine($"Status: {pumpReading.Status}");
Console.WriteLine($"Current: {pumpReading.CurrentAmps:F2}A");
Console.WriteLine($"Confidence: {pumpReading.Confidence:P2}");
```

### Quality Validation

```csharp
var ocrResult = await _ocrService.ExtractTextAsync(imageBytes);

if (_ocrService.ValidateOcrQuality(ocrResult))
{
    // Process high-quality result
    var pumpReading = _ocrService.ParsePumpReading(ocrResult.ProcessedText);
}
else
{
    // Handle low-quality result
    _logger.LogWarning("OCR quality below threshold");
}
```

## Pump Status Detection

The system recognizes the following pump conditions:

| Status | Recognition Pattern | Description |
|--------|-------------------|-------------|
| **Normal** | `3.5`, `4.2`, `5.8` | Numeric current values ≥ 0.1A |
| **Idle** | `0.00`, `0.01`, `0.02` | Near-zero current values |
| **Dry** | `"Dry"` | Well dry condition message |
| **RapidCycle** | `"rcyc"` | Rapid cycling message |
| **Off** | *(blank/dark)* | No power or dark display |
| **Unknown** | *(unrecognized)* | Unrecognized patterns |

## Testing

### OCR Test Program

Run the standalone OCR test program to validate accuracy:

```bash
dotnet run --project Testing/OcrTestProgram.cs
```

### Test Individual Image

```bash
dotnet run --project Testing/OcrTestProgram.cs -- /path/to/image.jpg
```

### Sample Images Structure

```
debug_images/samples/
├── normal/     # Normal operation images (3.5A, 4.2A, etc.)
├── idle/       # Idle state images (0.00A, 0.01A, etc.)
├── dry/        # Dry condition images ("Dry")
├── rcyc/       # Rapid cycling images ("rcyc")
├── off/        # Power off images (dark/blank)
└── live/       # Live capture images
```

## Installation

### Prerequisites

#### Tesseract OCR
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install tesseract-ocr tesseract-ocr-eng

# Windows (using Chocolatey)
choco install tesseract

# macOS (using Homebrew)
brew install tesseract
```

#### NuGet Packages
```xml
<PackageReference Include="Tesseract" Version="5.2.0" />
<PackageReference Include="Azure.AI.Vision.ImageAnalysis" Version="1.0.0-beta.3" />
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.5" />
```

### Environment Variables

```bash
# OCR provider selection
export WELLMONITOR_OCR_PROVIDER=Tesseract

# Azure Cognitive Services (if using)
export WELLMONITOR_AZURE_COGNITIVE_ENDPOINT=https://your-service.cognitiveservices.azure.com/
```

## Performance Optimization

### Tesseract Optimization
- **Character Whitelist**: Limit recognition to expected characters
- **Page Segmentation**: Use mode 7 for single text lines
- **Engine Mode**: Use LSTM engine (mode 3) for better accuracy

### Image Preprocessing
- **Scaling**: 2x scaling improves small text recognition
- **Binary Thresholding**: Enhances LED display contrast
- **Noise Reduction**: Removes capture artifacts

### Azure Cognitive Services
- **Read API**: Use for better printed text accuracy
- **Managed Identity**: Secure authentication without keys
- **Regional Deployment**: Deploy in same region as device

## Monitoring and Diagnostics

### OCR Statistics

```csharp
var stats = _ocrService.GetStatistics();
Console.WriteLine($"Success Rate: {stats.SuccessRate:P2}");
Console.WriteLine($"Avg Processing Time: {stats.AverageProcessingTimeMs}ms");
Console.WriteLine($"Avg Confidence: {stats.AverageConfidence:P2}");
```

### Error Handling

```csharp
try
{
    var result = await _ocrService.ExtractTextAsync(imageBytes);
    
    if (!result.Success)
    {
        _logger.LogWarning("OCR failed: {Error}", result.ErrorMessage);
        // Handle gracefully - maybe retry or use fallback
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "OCR processing exception");
    // Implement circuit breaker pattern
}
```

## Troubleshooting

### Common Issues

1. **Tesseract Not Found**
   - Ensure Tesseract is installed and in PATH
   - Check tessdata directory exists
   - Verify language data files are present

2. **Poor OCR Accuracy**
   - Adjust image preprocessing settings
   - Increase scaling factor
   - Improve image quality/lighting
   - Use binary thresholding for LED displays

3. **Azure Cognitive Services Errors**
   - Verify endpoint URL is correct
   - Check authentication (Managed Identity)
   - Ensure sufficient quota/limits
   - Check regional availability

### Debug Mode

Enable debug logging to see detailed OCR processing:

```json
{
  "Logging": {
    "LogLevel": {
      "WellMonitor.Device.Services.OcrService": "Debug",
      "WellMonitor.Device.Services.TesseractOcrProvider": "Debug"
    }
  }
}
```

## Enterprise Features

### Multi-Tenancy
- Provider-specific configuration per tenant
- Isolated processing pipelines
- Tenant-specific quality thresholds

### Compliance
- Audit logging for all OCR operations
- Data retention policies
- Privacy-compliant image processing

### Scalability
- Horizontal scaling with multiple providers
- Load balancing across OCR instances
- Auto-scaling based on demand

## Future Enhancements

- **Custom Model Training**: Train models for specific LED displays
- **Edge AI Integration**: On-device ML model inference
- **Real-time Streaming**: Live video OCR processing
- **Multi-language Support**: International deployment support
