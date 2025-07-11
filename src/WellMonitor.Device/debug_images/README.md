# Debug Images Directory

This directory is used for storing debug images captured by the camera service during development and testing.

## Features

- **Automatic Creation**: Directory is created automatically when the application starts
- **Relative Path Support**: Paths can be relative to the application directory or absolute
- **Cross-Platform**: Works on Windows, Linux, and macOS
- **Git Tracking**: Directory structure is tracked in git, but image files are ignored

## Configuration

The debug image path can be configured in several ways:

1. **Default**: `debug_images` (relative to application directory)
2. **Device Twin**: Set `cameraDebugImagePath` property in Azure IoT Hub
3. **Environment Variable**: Set via camera configuration
4. **Absolute Path**: Use full path like `/home/pi/wellmonitor-debug`

## Examples

```json
{
  "cameraDebugImagePath": "debug_images",           // Relative to app
  "cameraDebugImagePath": "./custom_debug",         // Relative to app  
  "cameraDebugImagePath": "/var/log/wellmonitor"    // Absolute path
}
```

## File Naming

Debug images are saved with timestamp format:
- `pump_reading_20250711_143022.jpg`
- `pump_reading_YYYYMMDD_HHMMSS.jpg`
