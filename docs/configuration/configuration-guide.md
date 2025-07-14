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

## Configuration Logging and Validation

### Enhanced Logging Features

The WellMonitor device provides comprehensive logging for configuration management:

#### Nested Property Support
- **Preferred Structure**: Use nested properties (e.g., `Camera.Gain`)
- **Legacy Support**: Flat properties still supported (e.g., `cameraGain`)
- **Automatic Fallback**: Falls back to legacy if nested not found
- **Conflict Detection**: Warns when both nested and legacy exist

#### Configuration Source Tracking
```
✅ Camera settings loaded from device twin: Gain=0.5, ShutterSpeedMicroseconds=5000
🔸 Camera settings using default values: Width=1920 (default), Height=1080 (default)
⚠️ Using legacy property 'cameraGain' - consider migrating to 'Camera.Gain'
```

#### Validation Warnings
The system automatically detects problematic configurations:

```
⚠️ High camera gain (12.0) detected - may cause overexposure with bright LEDs
⚠️ Auto-exposure enabled - may cause inconsistent exposure with LED displays
⚠️ Long shutter speed (50000μs) - may cause motion blur or overexposure
⚠️ Debug image path uses absolute path - consider relative path for portability
```

#### Startup Configuration Summary
```
📸 Final Camera Configuration:
   Image: 1280x720, Quality: 85%, Rotation: 0°
   Exposure: Manual (Gain=0.5, Shutter=5000μs)
   Processing: AutoExposure=false, AutoWhiteBalance=false
   Debug: Enabled (Path=debug_images)

🔍 Final OCR Configuration:
   Provider: Tesseract (Fallback: Azure Cognitive Services)
   Confidence: 60% minimum, Preprocessing: Enabled
   Language: English, Character Set: 0123456789.DryAMPSrcyc
   Performance: ~95% success rate, ~250ms average processing
```

### Configuration Validation Rules

#### Camera Settings
- **Gain Range**: 0.1 - 16.0 (warn if > 2.0 for LED displays)
- **Shutter Speed**: 100 - 100000 microseconds (warn if > 20000)
- **Auto Features**: Warn if auto-exposure/white-balance enabled for LED monitoring
- **Resolution**: Validate width/height are supported by camera

#### OCR Settings
- **Confidence**: 0.0 - 100.0 (recommend 60-80 for pump displays)
- **Provider**: Validate provider is available and configured
- **Character Whitelist**: Ensure includes digits and expected status text
- **Processing**: Validate image preprocessing parameters are reasonable

#### Monitoring Settings
- **Intervals**: Minimum 10 seconds between captures (prevent overload)
- **Retention**: Maximum 365 days for local storage
- **Paths**: Prefer relative paths for portability
- **Thresholds**: Validate current ranges match pump specifications

### Configuration Migration

#### From Legacy to Nested Properties
Use the PowerShell script to migrate configurations:

```powershell
# Run property name migration script
./scripts/diagnostics/fix-camera-property-names.ps1 -DeviceId "your-device-id"
```

#### Manual Migration Example
```json
// Old (Legacy) - Still Supported
{
  "cameraGain": 0.5,
  "cameraShutterSpeedMicroseconds": 5000,
  "cameraAutoExposure": false
}

// New (Preferred) - Better Organization
{
  "Camera": {
    "Gain": 0.5,
    "ShutterSpeedMicroseconds": 5000,
    "AutoExposure": false
  }
}
```

### Performance Optimization
- Balance monitoring frequency with system load
- Use appropriate image resolution (1280x720 recommended)
- Configure retention periods based on available storage
- Enable debug images only when needed

For specific hardware setup, see [Camera & OCR Setup](camera-ocr-setup.md).
For Azure service configuration, see [Azure Integration](azure-integration.md).

## Region of Interest (ROI) Configuration

### Overview
ROI (Region of Interest) processing focuses OCR analysis on just the 7-segment LED display area, eliminating interference from switches, labels, and background elements. This dramatically improves accuracy and processing speed.

### ROI Benefits
- **3-5x Faster Processing**: Smaller images process much faster
- **Higher Accuracy**: Eliminates false positives from irrelevant text
- **Better LED Recognition**: Higher pixel density for digits
- **Reduced CPU Usage**: Less data to process
- **Remote Calibration**: Adjustable via device twin

### ROI Configuration Options

#### Basic ROI Setup
```json
{
  "RegionOfInterest": {
    "EnableRoi": true,
    "RoiPercent": {
      "X": 0.30,
      "Y": 0.35, 
      "Width": 0.40,
      "Height": 0.25
    },
    "EnableAutoDetection": false,
    "ExpansionMargin": 15
  }
}
```

#### ROI Coordinate System
- **X**: Horizontal start position (0.0 = left edge, 1.0 = right edge)
- **Y**: Vertical start position (0.0 = top edge, 1.0 = bottom edge)  
- **Width**: ROI width as percentage of image width
- **Height**: ROI height as percentage of image height

Example: `X=0.30, Y=0.35, Width=0.40, Height=0.25`
- Starts 30% from left, 35% from top
- Covers 40% of image width, 25% of image height
- Results in ROI at center-left of image

### ROI Calibration Process

#### Step 1: Capture Full Images
```bash
# Enable debug images to see full captures
# Set via device twin: "debugImageSaveEnabled": true

# Restart service to capture new images
sudo systemctl restart wellmonitor

# Wait for several captures
sleep 60

# Check captured images
ls -la /var/lib/wellmonitor/debug_images/
```

#### Step 2: Analyze LED Display Position
```bash
# Copy latest image to analyze
scp pi@your-pi-ip:/var/lib/wellmonitor/debug_images/pump_reading_*.jpg ./

# View image and identify LED display location
# Note the approximate coordinates of the 7-segment display
```

#### Step 3: Calculate ROI Coordinates
For a 1920x1080 image with LED display at:
- **Left edge**: 600 pixels from left (600/1920 = 0.31)
- **Top edge**: 400 pixels from top (400/1080 = 0.37)  
- **Display width**: 720 pixels (720/1920 = 0.375)
- **Display height**: 200 pixels (200/1080 = 0.185)

**Resulting ROI Configuration:**
```json
{
  "RoiPercent": {
    "X": 0.31,
    "Y": 0.37,
    "Width": 0.375, 
    "Height": 0.185
  }
}
```

#### Step 4: Update Device Twin
```powershell
# Use PowerShell script to update ROI settings
.\scripts\configuration\update-device-twin.ps1 `
  -IoTHubName "YourHub" `
  -DeviceId "YourDevice" `
  -ConfigType "roi" `
  -RoiX 0.31 `
  -RoiY 0.37 `
  -RoiWidth 0.375 `
  -RoiHeight 0.185
```

#### Step 5: Validate ROI Results
```bash
# Monitor OCR results after ROI configuration
sudo journalctl -u wellmonitor -f | grep -i "ocr\|confidence\|roi"

# Check for ROI debug images
ls -la /var/lib/wellmonitor/debug_images/roi_*

# Verify improved OCR confidence scores
sudo journalctl -u wellmonitor | grep "confidence" | tail -10
```

### ROI Debug Images

When ROI is enabled, additional debug images are saved:

```
debug_images/
├── pump_reading_20250713_143022.jpg      # Original full image
├── roi_extracted_20250713_143022.jpg     # Cropped ROI area only
├── roi_overlay_20250713_143022.jpg       # Full image with ROI boundary shown
└── roi_processed_20250713_143022.jpg     # ROI after preprocessing
```

### Advanced ROI Features

#### Automatic LED Detection (Experimental)
```json
{
  "RegionOfInterest": {
    "EnableAutoDetection": true,
    "LedBrightnessThreshold": 180,
    "ExpansionMargin": 20
  }
}
```

This attempts to automatically find bright LED displays, but manual configuration is more reliable.

#### Multiple ROI Profiles
```json
{
  "RoiProfiles": {
    "Standard": { "X": 0.30, "Y": 0.35, "Width": 0.40, "Height": 0.25 },
    "HighMount": { "X": 0.25, "Y": 0.20, "Width": 0.50, "Height": 0.30 },
    "CloseUp": { "X": 0.15, "Y": 0.25, "Width": 0.70, "Height": 0.50 }
  },
  "ActiveProfile": "Standard"
}
```

### Troubleshooting ROI Issues

#### ROI Too Small - Missing LED Digits
**Symptoms**: OCR confidence drops, missing digits
**Solution**: Increase Width and Height values

#### ROI Too Large - Background Interference  
**Symptoms**: False positives, incorrect readings
**Solution**: Decrease Width and Height to focus on LED area only

#### ROI Misaligned - No LED Content
**Symptoms**: Very low confidence, no text detected
**Solution**: Adjust X and Y coordinates to center on LED display

#### Camera Movement - ROI No Longer Valid
**Symptoms**: Sudden drop in OCR accuracy after working well
**Solution**: Recalibrate ROI coordinates for new camera position

### Performance Optimization
