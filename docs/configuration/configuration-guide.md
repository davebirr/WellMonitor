# WellMonitor Configuration Guide

Complete guide for configuring the WellMonitor application through Azure IoT Hub device twin and environment variables.

## Configuration Overview

WellMonitor uses a two-tier configuration system:
1. **Environment Variables** - Essential connection strings and security keys
2. **Device Twin Properties** - 39+ configurable parameters for remote management

## Environment Variables

### Required Configuration

Set these essential environment variables in `/etc/wellmonitor/environment`:

```bash
# Required - Azure IoT Hub connection
WELLMONITOR_IOTHUB_CONNECTION_STRING="HostName=your-hub.azure-devices.net;DeviceId=your-device;SharedAccessKey=your-key"

# System configuration  
WELLMONITOR_SECRETS_MODE="environment"
```

### Optional Configuration

```bash
# Azure Storage for telemetry backup
WELLMONITOR_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"

# Azure Cognitive Services for OCR
WELLMONITOR_OCR_API_KEY="your-azure-cognitive-services-key"
WELLMONITOR_OCR_ENDPOINT="https://your-region.api.cognitive.microsoft.com/"

# PowerApp integration
WELLMONITOR_POWERAPP_API_KEY="your-powerapp-api-key"

# Local encryption for sensitive data
WELLMONITOR_LOCAL_ENCRYPTION_KEY="your-32-character-encryption-key-here"
```

### Updating Environment Variables

```bash
# Edit configuration
sudo nano /etc/wellmonitor/environment

# Apply changes
sudo systemctl restart wellmonitor

# Verify configuration
sudo systemctl show wellmonitor --property=Environment
```

## Device Twin Configuration

The device twin allows remote configuration of 39+ parameters without service restart. All settings are in the `properties.desired` section.

### Monitoring Settings

```json
{
  "properties": {
    "desired": {
      "monitoringIntervalSeconds": 30,
      "telemetryIntervalMinutes": 5,
      "syncIntervalHours": 1,
      "dataRetentionDays": 30
    }
  }
}
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `monitoringIntervalSeconds` | 30 | Frequency of camera captures and OCR processing |
| `telemetryIntervalMinutes` | 5 | Frequency of telemetry transmission to Azure |
| `syncIntervalHours` | 1 | Frequency of data synchronization |
| `dataRetentionDays` | 30 | Local database retention period |

### Camera Configuration

```json
{
  "cameraWidth": 1280,
  "cameraHeight": 720,
  "cameraQuality": 85,
  "cameraTimeoutMs": 5000,
  "cameraWarmupTimeMs": 3000,
  "cameraRotation": 0,
  "cameraBrightness": 70,
  "cameraContrast": 50,
  "cameraSaturation": 30,
  "cameraGain": 12.0,
  "cameraShutterSpeedMicroseconds": 50000,
  "cameraAutoExposure": false,
  "cameraAutoWhiteBalance": false,
  "cameraDebugImagePath": "debug_images"
}
```

**LED Display Optimization (Dark Basement):**
- `cameraGain: 12.0` - High gain for dark environments
- `cameraShutterSpeedMicroseconds: 50000` - 50ms exposure for red LED displays
- `cameraAutoExposure: false` - Manual control for consistent readings
- `cameraAutoWhiteBalance: false` - Prevents color shift with red displays

### OCR Configuration

```json
{
  "ocrProvider": "Tesseract",
  "ocrMinimumConfidence": 0.7,
  "ocrTesseractLanguage": "eng",
  "ocrTesseractEngineMode": 3,
  "ocrTesseractPageSegmentationMode": 7,
  "ocrTesseractCharWhitelist": "0123456789.DryAMPSrcyc ",
  "ocrImagePreprocessing": {
    "enableGrayscale": true,
    "enableContrastEnhancement": true,
    "contrastFactor": 1.5,
    "enableBrightnessAdjustment": true,
    "brightnessAdjustment": 10,
    "enableNoiseReduction": true,
    "enableScaling": true,
    "scaleFactor": 2.0,
    "enableBinaryThresholding": true,
    "binaryThreshold": 128
  }
}
```

| Parameter | Description |
|-----------|-------------|
| `ocrProvider` | "Tesseract" or "AzureCognitive" |
| `ocrMinimumConfidence` | Minimum confidence threshold (0.0-1.0) |
| `ocrTesseractPageSegmentationMode` | 7 = single text line |
| `ocrTesseractCharWhitelist` | Allowed characters for recognition |

### Pump Monitoring Thresholds

```json
{
  "pumpCurrentThresholds": {
    "offCurrentThreshold": 0.1,
    "idleCurrentThreshold": 0.5,
    "normalCurrentMin": 3.0,
    "normalCurrentMax": 8.0,
    "maxValidCurrent": 25.0,
    "highCurrentThreshold": 20.0
  },
  "statusDetection": {
    "dryKeywords": ["Dry", "No Water", "Empty", "Well Dry"],
    "rapidCycleKeywords": ["rcyc", "Rapid Cycle", "Cycling", "Fault", "Error"],
    "statusMessageCaseSensitive": false
  }
}
```

### Alert Configuration

```json
{
  "alertConfig": {
    "dryCount": 3,
    "rCYCCount": 2
  },
  "alertDryCountThreshold": 3,
  "alertRcycCountThreshold": 2,
  "alertMaxRetryAttempts": 5,
  "alertCooldownMinutes": 15
}
```

### Power Management

```json
{
  "powerManagement": {
    "enableAutoActions": true,
    "powerCycleDelaySeconds": 5,
    "minimumCycleIntervalMinutes": 30,
    "maxDailyCycles": 10,
    "enableDryConditionCycling": false
  }
}
```

### Debug and Logging

```json
{
  "debugMode": false,
  "debugImageSaveEnabled": true,
  "debugImageRetentionDays": 7,
  "logLevel": "Information",
  "enableVerboseOcrLogging": false,
  "cameraDebugImagePath": "debug_images"
}
```

## Device Twin Management

### Using PowerShell Scripts (Windows Development)

Update device twin configuration using the provided PowerShell scripts:

```powershell
# Set up Azure CLI (first time only)
.\scripts\Setup-AzureCli.ps1

# Update LED camera settings for dark basement
.\scripts\Update-LedCameraSettings.ps1

# Test camera optimization
.\scripts\Test-LedCameraOptimization.ps1
```

### Using Azure CLI (Manual)

```bash
# View current device twin
az iot hub device-twin show --device-id your-device-id --hub-name your-hub-name

# Update specific properties
az iot hub device-twin update --device-id your-device-id --hub-name your-hub-name \
  --set properties.desired.cameraGain=12.0 \
  --set properties.desired.cameraShutterSpeedMicroseconds=50000
```

### Configuration Validation

The application validates all device twin settings and provides safe fallbacks:

```json
{
  "properties": {
    "reported": {
      "configurationStatus": "Valid",
      "validationErrors": [],
      "lastConfigUpdate": "2025-07-12T10:30:00Z",
      "appliedSettings": {
        "cameraGain": 12.0,
        "ocrProvider": "Tesseract",
        "monitoringInterval": 30
      }
    }
  }
}
```

## Configuration Categories

### 1. Camera Settings (11 parameters)
- Resolution, quality, timeouts
- Manual exposure control for LED displays
- Debug image saving

### 2. OCR Settings (15+ parameters)
- Provider selection (Tesseract/Azure)
- Image preprocessing pipeline
- Confidence thresholds and retry logic

### 3. Monitoring Intervals (4 parameters)
- Capture frequency, telemetry, sync, retention

### 4. Image Quality Validation (5 parameters)
- Brightness, contrast, noise thresholds

### 5. Alert Thresholds (4 parameters)
- Dry count, cycle count, retry limits

### 6. Debug Options (5 parameters)
- Logging levels, debug image retention

## Hot Configuration Updates

Device twin changes apply immediately without service restart through the `DeviceTwinService`:

1. Device twin update received from Azure
2. Configuration validation performed
3. Settings applied to active services
4. Reported properties updated with status

Monitor configuration updates in logs:
```bash
sudo journalctl -u wellmonitor | grep -i "configuration\|device.*twin"
```

## Configuration Best Practices

### Camera Optimization for Red LED Displays
- Use manual exposure control (`cameraAutoExposure: false`)
- Set high gain for dark environments (`cameraGain: 12.0`)
- Use longer exposure for stable readings (`cameraShutterSpeedMicroseconds: 50000`)
- Enable debug images to verify capture quality

### OCR Reliability
- Start with Tesseract for offline processing
- Use character whitelist to limit false positives
- Enable image preprocessing for better recognition
- Monitor confidence scores and adjust thresholds

### Security Considerations
- Never put connection strings in device twin (use environment variables)
- Use relative paths for debug images (`debug_images` not `/home/...`)
- Keep API keys in environment variables, not device twin
- Regularly rotate access keys

### Performance Optimization
- Balance monitoring frequency with system load
- Use appropriate image resolution (1280x720 recommended)
- Configure retention periods based on available storage
- Enable debug images only when needed

For specific hardware setup, see [Camera & OCR Setup](camera-ocr-setup.md).
For Azure service configuration, see [Azure Integration](azure-integration.md).
