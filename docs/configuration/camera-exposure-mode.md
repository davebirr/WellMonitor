# Camera Exposure Mode Configuration

## Overview

The WellMonitor system now supports configurable camera exposure modes to optimize image capture for different lighting conditions. This feature allows users to select the most appropriate exposure mode through the web interface or device twin configuration.

## Features

### Supported Exposure Modes

The system supports 17 different exposure modes, each optimized for specific lighting conditions:

| Mode | Description | Best Use Case |
|------|-------------|---------------|
| `Auto` | Automatic exposure mode selection | General purpose, default mode |
| `Normal` | Standard exposure mode | Well-lit environments |
| `Sport` | Fast shutter speed for moving subjects | Active environments |
| `Night` | Enhanced low-light performance | Dark environments |
| `Backlight` | Compensates for bright background | Backlit subjects |
| `Spotlight` | Optimized for bright spot lighting | Focused lighting |
| `Beach` | Optimized for bright beach/sand conditions | High-reflectance surfaces |
| `Snow` | Optimized for bright snow conditions | Snow/ice environments |
| `Fireworks` | Long exposure for fireworks | Special events |
| `Party` | Indoor party lighting | Mixed indoor lighting |
| `Candlelight` | Warm, low-light conditions | Intimate lighting |
| `Barcode` | High contrast for barcode/LED reading | **LED displays (recommended)** |
| `Macro` | Close-up photography | Detailed close-ups |
| `Landscape` | Wide depth of field | Outdoor landscapes |
| `Portrait` | Shallow depth of field | People/portraits |
| `Antishake` | Reduced camera shake | Handheld shots |
| `FixedFps` | Fixed frame rate mode | Consistent timing |

### Recommended Mode for WellMonitor

For LED display monitoring (the primary use case for WellMonitor), the **`Barcode`** mode is recommended as it provides:
- High contrast optimization
- Sharp edge detection
- Optimal LED digit recognition
- Reduced noise in bright display conditions

## Configuration Methods

### 1. Web Interface

The camera exposure mode can be configured through the web interface:

1. Navigate to **Camera Setup** tab
2. Find the **Camera Exposure Mode** section
3. Select the desired mode from the dropdown
4. Use quick selection buttons for common scenarios:
   - **LED Display** (Barcode mode)
   - **Normal Lighting** (Normal mode)
   - **Low Light** (Night mode)
   - **Auto** (Automatic mode)
5. Click **Apply Mode** to save the configuration
6. Click **Test Capture** to verify the setting

### 2. Device Twin Configuration

#### Nested Configuration Format (Recommended)

```json
{
  "properties": {
    "desired": {
      "Camera": {
        "ExposureMode": "Barcode"
      }
    }
  }
}
```

#### Legacy Configuration Format

```json
{
  "properties": {
    "desired": {
      "cameraExposureMode": "Barcode"
    }
  }
}
```

### 3. PowerShell Script

Use the provided PowerShell script to update exposure mode:

```powershell
# Update to Barcode mode (recommended for LED displays)
./scripts/configuration/update-camera-exposure-mode.ps1 -DeviceId "your-device-id" -HubName "your-hub-name" -ExposureMode "Barcode"

# Update to Auto mode with nested configuration
./scripts/configuration/update-camera-exposure-mode.ps1 -DeviceId "your-device-id" -HubName "your-hub-name" -ExposureMode "Auto" -UseNestedConfig
```

### 4. Shell Script

Use the provided shell script for Linux environments:

```bash
# Update to Barcode mode (recommended for LED displays)
./scripts/configuration/update-camera-exposure-mode.sh -d "your-device-id" -h "your-hub-name" -e "Barcode"

# Update to Auto mode with nested configuration
./scripts/configuration/update-camera-exposure-mode.sh -d "your-device-id" -h "your-hub-name" -e "Auto" -n
```

## API Endpoints

### Get Current Camera Configuration

```http
GET /api/camera/configuration
```

Response:
```json
{
  "exposureMode": "Barcode",
  "autoWhiteBalance": false,
  "enablePreview": false,
  "debugImagePath": "/tmp/wellmonitor-debug"
}
```

### Update Camera Exposure Mode

```http
POST /api/camera/exposure-mode
Content-Type: application/json

{
  "exposureMode": "Barcode"
}
```

Response:
```json
{
  "message": "Camera exposure mode updated to Barcode",
  "exposureMode": "Barcode"
}
```

### Capture Test Image

```http
POST /api/camera/test-capture
```

Response:
```json
{
  "message": "Test image captured successfully",
  "imagePath": "/tmp/wellmonitor-debug/test_exposure_20250118_144512.jpg",
  "timestamp": "2025-01-18T14:45:12Z"
}
```

### Get Available Exposure Modes

```http
GET /api/camera/exposure-modes
```

Response:
```json
[
  {
    "value": "Auto",
    "displayName": "Auto",
    "description": "Automatic exposure mode selection",
    "recommended": false
  },
  {
    "value": "Barcode",
    "displayName": "Barcode (LED Display)",
    "description": "High contrast for barcode/LED reading",
    "recommended": true
  }
]
```

## Technical Implementation

### Camera Command Integration

The exposure mode is integrated into the camera capture commands:

```bash
# Example with Barcode mode
libcamera-still --immediate --nopreview --width 1920 --height 1080 --awb auto --exposure barcode --output /tmp/image.jpg

# Example with Auto mode
libcamera-still --immediate --nopreview --width 1920 --height 1080 --awb auto --exposure auto --output /tmp/image.jpg
```

### Fallback Behavior

The system implements a comprehensive fallback chain:

1. **User-configured exposure mode** (e.g., "Barcode")
2. **Default mode for LED displays** ("Barcode")
3. **General default mode** ("Normal")
4. **Camera auto mode** ("Auto")

### Special Handling

- **FixedFps mode**: Mapped to "fixedfps" for libcamera-still compatibility
- **Invalid modes**: Automatically fall back to default with warning logs
- **Legacy compatibility**: Supports both new enum-based and legacy string-based configuration

## Monitoring and Diagnostics

### Debug Information

The system logs detailed information about exposure mode configuration:

```
[INFO] Updating camera exposure mode to Barcode
[INFO] Camera exposure mode applied: Barcode
[INFO] Test image captured successfully: /tmp/wellmonitor-debug/test_exposure_20250118_144512.jpg
```

### Test Capture Feature

The web interface includes a **Test Capture** button that:
- Captures an image with the current exposure mode
- Saves it to the debug images directory
- Provides immediate feedback on the mode's effectiveness
- Allows comparison between different exposure modes

## Migration Guide

### From Previous Versions

If you're upgrading from a previous version that used `--exposure off`:

1. The system will automatically use **Barcode** mode instead of the invalid "off" mode
2. Existing configurations will be preserved
3. The web interface will show the current effective mode
4. No manual migration is required

### Configuration Verification

To verify your exposure mode configuration:

1. Check the web interface **Camera Setup** section
2. Review device twin desired properties
3. Monitor application logs for exposure mode messages
4. Use the test capture feature to validate image quality

## Best Practices

### For LED Display Monitoring

1. **Use Barcode mode** for optimal LED digit recognition
2. **Test capture** after changing modes to verify effectiveness
3. **Monitor image quality** in debug images section
4. **Adjust based on lighting conditions** (try Night mode for dim displays)

### For General Use

1. **Start with Auto mode** for general purpose monitoring
2. **Use Normal mode** for well-lit environments
3. **Switch to Night mode** for low-light conditions
4. **Test different modes** to find the optimal setting for your specific environment

### Configuration Management

1. **Use nested configuration format** for new deployments
2. **Test changes** with the test capture feature before applying to production
3. **Monitor device logs** for exposure mode application confirmations
4. **Document** your optimal exposure mode settings for future reference

## Troubleshooting

### Common Issues

1. **Exposure mode not applied**: Check device twin synchronization
2. **Invalid mode errors**: Verify the mode name is spelled correctly
3. **Poor image quality**: Try different exposure modes for your lighting conditions
4. **API errors**: Ensure the mode value is valid and properly formatted

### Debug Steps

1. Check the web interface for current configuration
2. Review device twin desired properties
3. Monitor application logs for error messages
4. Use the test capture feature to verify camera functionality
5. Try different exposure modes to find the optimal setting

## Future Enhancements

Potential future improvements include:

1. **Automatic mode detection** based on lighting conditions
2. **Custom exposure values** for fine-tuning
3. **Scheduled mode changes** based on time of day
4. **AI-powered mode recommendations** based on image analysis
5. **Integration with ambient light sensors** for automatic adjustments
