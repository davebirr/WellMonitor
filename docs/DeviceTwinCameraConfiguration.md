# Device Twin Camera Configuration

## Overview

The camera service now supports dynamic configuration through Azure IoT Hub Device Twin. This allows you to remotely adjust camera settings without deploying new code or accessing the device directly.

## Device Twin Configuration

### Sample Device Twin Desired Properties

```json
{
  "properties": {
    "desired": {
      "currentThreshold": 4.5,
      "cycleTimeThreshold": 30,
      "relayDebounceMs": 500,
      "syncIntervalMinutes": 5,
      "logRetentionDays": 14,
      "ocrMode": "tesseract",
      "powerAppEnabled": true,
      
      // Camera Configuration
      "cameraWidth": 1920,
      "cameraHeight": 1080,
      "cameraQuality": 95,
      "cameraTimeoutMs": 5000,
      "cameraWarmupTimeMs": 2000,
      "cameraRotation": 0,
      "cameraBrightness": 50,
      "cameraContrast": 10,
      "cameraSaturation": 0,
      "cameraEnablePreview": false,
      "cameraDebugImagePath": "/home/pi/wellmonitor/debug_images"
    }
  }
}
```

### Camera Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `cameraWidth` | int | 1920 | Image width in pixels |
| `cameraHeight` | int | 1080 | Image height in pixels |
| `cameraQuality` | int | 95 | JPEG quality (0-100) |
| `cameraTimeoutMs` | int | 5000 | Command timeout in milliseconds |
| `cameraWarmupTimeMs` | int | 2000 | Camera warmup time before capture |
| `cameraRotation` | int | 0 | Image rotation (0, 90, 180, 270) |
| `cameraBrightness` | int | 50 | Brightness adjustment (0-100) |
| `cameraContrast` | int | 0 | Contrast adjustment (-100 to 100) |
| `cameraSaturation` | int | 0 | Saturation adjustment (-100 to 100) |
| `cameraEnablePreview` | bool | false | Enable camera preview |
| `cameraDebugImagePath` | string | null | Path to save debug images |

## How It Works

1. **Startup Configuration**: Device twin configuration is loaded during the `DependencyValidationService` startup phase
2. **Fallback Values**: If device twin properties are not available, the service uses default values from the `CameraOptions` class
3. **Live Updates**: Changes to device twin desired properties are applied immediately during startup
4. **Logging**: All configuration changes are logged for debugging

## Usage Examples

### Adjusting for Better OCR Quality

```json
{
  "properties": {
    "desired": {
      "cameraQuality": 100,
      "cameraBrightness": 60,
      "cameraContrast": 15,
      "cameraDebugImagePath": "/home/pi/wellmonitor/debug_images"
    }
  }
}
```

### Rotating Camera for Different Mounting

```json
{
  "properties": {
    "desired": {
      "cameraRotation": 180,
      "cameraBrightness": 45
    }
  }
}
```

### Reducing Image Size for Faster Processing

```json
{
  "properties": {
    "desired": {
      "cameraWidth": 1280,
      "cameraHeight": 720,
      "cameraQuality": 85
    }
  }
}
```

## Implementation Details

### Service Integration

The camera configuration is loaded in the `DependencyValidationService` during startup:

```csharp
private async Task LoadDeviceTwinConfigurationAsync()
{
    // Load device twin configuration including camera settings
    var deviceTwinConfig = await deviceTwinService.FetchAndApplyConfigAsync(
        deviceClient, 
        configuration, 
        gpioOptions, 
        cameraOptions,  // Camera options are updated here
        _logger);
}
```

### Configuration Flow

1. **Device Twin Fetch**: `DeviceTwinService.FetchAndApplyConfigAsync()` retrieves desired properties
2. **Camera Options Update**: `UpdateCameraOptionsFromDeviceTwin()` applies camera settings
3. **Logging**: Configuration changes are logged for troubleshooting
4. **Service Usage**: Updated camera options are used by `CameraService` for image capture

## Benefits

✅ **Remote Configuration**: No need to SSH into the device or redeploy code
✅ **Real-time Tuning**: Adjust settings based on lighting conditions or pump display changes
✅ **A/B Testing**: Test different camera settings across multiple devices
✅ **Troubleshooting**: Enable debug images remotely when OCR quality degrades
✅ **Seasonal Adjustments**: Adapt to changing environmental conditions
✅ **Fallback Safety**: Always falls back to sensible defaults if device twin is unavailable

## Troubleshooting

### Common Configuration Issues

1. **Camera Settings Not Applied**:
   - Check device twin desired properties are set correctly
   - Verify IoT Hub connection string is configured
   - Review application logs for device twin loading errors

2. **Invalid Camera Values**:
   - Ensure quality is between 0-100
   - Verify rotation is 0, 90, 180, or 270
   - Check brightness/contrast values are within valid ranges

3. **Debug Images Not Saving**:
   - Verify `cameraDebugImagePath` directory exists
   - Check file permissions on the debug path
   - Ensure adequate disk space

### Monitoring Configuration Changes

The application logs all configuration changes:

```
info: WellMonitor.Device.Services.DeviceTwinService[0]
      Camera config: width=1920, height=1080, quality=95, brightness=60, contrast=15, rotation=0
```

This enhancement provides powerful remote configuration capabilities for fine-tuning camera settings based on real-world conditions without requiring device access.
