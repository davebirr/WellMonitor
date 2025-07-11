# Device Twin OCR Configuration Guide

## Overview
This guide shows how to configure your Azure IoT Hub device twin for OCR settings, starting with Tesseract provider configuration.

## Step 1: Basic Tesseract Configuration

Here's the minimal device twin configuration for Tesseract OCR:

```json
{
  "properties": {
    "desired": {
      "ocrProvider": "Tesseract",
      "ocrMinimumConfidence": 0.7,
      "ocrTesseractLanguage": "eng"
    }
  }
}
```

## Step 2: Complete Tesseract Configuration

For optimal performance with LED displays, use this comprehensive configuration:

```json
{
  "properties": {
    "desired": {
      "ocrProvider": "Tesseract",
      "ocrMinimumConfidence": 0.7,
      "ocrMaxRetryAttempts": 3,
      "ocrTimeoutSeconds": 30,
      "ocrEnablePreprocessing": true,
      "ocrTesseractLanguage": "eng",
      "ocrTesseractEngineMode": 3,
      "ocrTesseractPageSegmentationMode": 7,
      "ocrTesseractCharWhitelist": "0123456789.DryAMPSrcyc ",
      "ocrImagePreprocessing": {
        "enableGrayscale": true,
        "enableThresholding": true,
        "thresholdValue": 128,
        "enableNoiseReduction": true,
        "enableSharpening": true,
        "enableScaling": true,
        "scaleFactor": 2.0,
        "contrastFactor": 1.5,
        "brightnessAdjustment": 10
      },
      "ocrRetrySettings": {
        "maxRetries": 3,
        "retryDelayMs": 1000
      }
    }
  }
}
```

## Step 3: Configuration Parameters Explained

### Core OCR Settings
- **ocrProvider**: Set to "Tesseract" for local processing
- **ocrMinimumConfidence**: Confidence threshold (0.0-1.0). Higher values = more accurate but may reject valid readings
- **ocrMaxRetryAttempts**: Number of retry attempts if OCR fails
- **ocrTimeoutSeconds**: Maximum time to wait for OCR processing

### Tesseract-Specific Settings
- **ocrTesseractLanguage**: Language model ("eng" for English)
- **ocrTesseractEngineMode**: 
  - 0: Original Tesseract only
  - 1: Neural nets LSTM only
  - 2: Tesseract + LSTM
  - 3: Default (based on what's available)
- **ocrTesseractPageSegmentationMode**:
  - 6: Uniform block of text
  - 7: Single text line (recommended for LED displays)
  - 8: Single word
  - 13: Raw line (treat as single text line)
- **ocrTesseractCharWhitelist**: Restrict to expected characters for better accuracy

### Image Preprocessing Settings
- **enableGrayscale**: Convert to grayscale for better contrast
- **enableThresholding**: Apply binary thresholding for LED displays
- **thresholdValue**: Threshold value for binary conversion (0-255)
- **enableNoiseReduction**: Remove image noise
- **enableSharpening**: Enhance text edges
- **enableScaling**: Scale image for better recognition
- **scaleFactor**: Image scaling factor (1.0-4.0)
- **contrastFactor**: Contrast adjustment (1.0-3.0)
- **brightnessAdjustment**: Brightness adjustment (-100 to +100)

## Step 4: Apply Configuration via Azure Portal

1. **Navigate to Azure IoT Hub**
   - Go to your Azure IoT Hub in the portal
   - Select "IoT devices" from the left menu
   - Click on your device name

2. **Edit Device Twin**
   - Click on "Device twin" tab
   - In the "properties.desired" section, add/update the OCR configuration
   - Click "Save"

3. **Example Azure Portal Configuration**
```json
{
  "deviceId": "wellmonitor-device-01",
  "etag": "AAAAAAAAAAE=",
  "deviceEtag": "Mjg2NzY4MTE2",
  "status": "enabled",
  "statusUpdateTime": "0001-01-01T00:00:00Z",
  "connectionState": "Connected",
  "lastActivityTime": "2025-07-11T10:30:00Z",
  "cloudToDeviceMessageCount": 0,
  "authenticationType": "sas",
  "x509Thumbprint": {
    "primaryThumbprint": null,
    "secondaryThumbprint": null
  },
  "modelId": "",
  "version": 123,
  "properties": {
    "desired": {
      "ocrProvider": "Tesseract",
      "ocrMinimumConfidence": 0.7,
      "ocrTesseractLanguage": "eng",
      "ocrTesseractEngineMode": 3,
      "ocrTesseractPageSegmentationMode": 7,
      "ocrTesseractCharWhitelist": "0123456789.DryAMPSrcyc ",
      "ocrImagePreprocessing": {
        "enableGrayscale": true,
        "enableThresholding": true,
        "thresholdValue": 128,
        "enableNoiseReduction": true,
        "enableSharpening": true
      },
      "$metadata": {
        "$lastUpdated": "2025-07-11T10:30:00Z"
      },
      "$version": 1
    },
    "reported": {
      "ocrProvider": "Tesseract",
      "ocrStatus": "Active",
      "ocrLastUpdate": "2025-07-11T10:30:00Z",
      "ocrStatistics": {
        "totalProcessed": 145,
        "successfulExtractions": 138,
        "averageConfidence": 0.89,
        "averageProcessingTime": 1250
      }
    }
  }
}
```

## Step 5: Apply Configuration via Azure CLI

You can also update the device twin using Azure CLI:

```bash
# Update device twin with OCR configuration
az iot hub device-twin update \
  --device-id "wellmonitor-device-01" \
  --hub-name "your-iot-hub-name" \
  --set properties.desired.ocrProvider="Tesseract" \
  --set properties.desired.ocrMinimumConfidence=0.7 \
  --set properties.desired.ocrTesseractLanguage="eng" \
  --set properties.desired.ocrTesseractEngineMode=3 \
  --set properties.desired.ocrTesseractPageSegmentationMode=7 \
  --set properties.desired.ocrTesseractCharWhitelist="0123456789.DryAMPSrcyc "
```

## Step 6: Verify Configuration

After applying the configuration:

1. **Check Device Logs**: Monitor your device logs for OCR configuration updates
2. **Check Reported Properties**: Verify the device reports back the OCR status
3. **Test OCR**: Capture a test image to verify OCR is working with new settings

## Step 7: Fine-Tuning for Your LED Display

### For Bright LED Displays
```json
{
  "ocrImagePreprocessing": {
    "enableGrayscale": true,
    "enableThresholding": true,
    "thresholdValue": 140,
    "enableNoiseReduction": false,
    "enableSharpening": true,
    "contrastFactor": 2.0,
    "brightnessAdjustment": -10
  }
}
```

### For Dim LED Displays
```json
{
  "ocrImagePreprocessing": {
    "enableGrayscale": true,
    "enableThresholding": true,
    "thresholdValue": 100,
    "enableNoiseReduction": true,
    "enableSharpening": true,
    "contrastFactor": 1.8,
    "brightnessAdjustment": 20
  }
}
```

### For High-Resolution Displays
```json
{
  "ocrImagePreprocessing": {
    "enableScaling": true,
    "scaleFactor": 1.5,
    "enableGrayscale": true,
    "enableThresholding": true,
    "thresholdValue": 128
  }
}
```

## Troubleshooting

### Low OCR Accuracy
- Decrease `ocrMinimumConfidence` temporarily
- Adjust `thresholdValue` (try 100-150 range)
- Increase `scaleFactor` for small text
- Use `ocrTesseractPageSegmentationMode: 6` for multi-line text

### OCR Too Slow
- Disable `enableNoiseReduction`
- Reduce `scaleFactor`
- Use `ocrTesseractEngineMode: 0` for faster processing

### OCR Fails Completely
- Check `ocrTesseractCharWhitelist` includes all needed characters
- Try `ocrTesseractPageSegmentationMode: 13` for simple text
- Verify image preprocessing isn't over-processing the image

## Next Steps

Once Tesseract is working well, you can:
1. Configure Azure Cognitive Services as a fallback provider
2. Set up automated switching based on conditions
3. Monitor OCR performance through Azure dashboards
4. Fine-tune settings based on real-world performance data

This configuration provides a solid foundation for reliable OCR processing on your Raspberry Pi with the flexibility to adjust settings remotely through Azure IoT Hub.
