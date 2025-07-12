# Debug Images Directory

This directory is used for storing debug images captured by the camera service during development and testing.

## Directory Structure

```
debug_images/
├── samples/           # Sample images for OCR development and testing
│   ├── normal/        # Pump running normally (e.g., 3.5-6.0 A)
│   ├── idle/          # Pump powered but not running (0.00-0.05 A)
│   ├── dry/           # Pump running but well is dry ("Dry" message)
│   ├── rcyc/          # Pump cycling rapidly ("rcyc" message)
│   ├── off/           # No power to pump (dark/blank display)
└── pump_reading_*.jpg # Live debug images with timestamps
```

## Pump Status Conditions

| Status | Current Range | Display Shows | Description |
|--------|---------------|---------------|-------------|
| **Normal** | 3.0-8.0 A | "4.2", "5.5", etc. | Pump running normally |
| **Idle** | 0.00-0.05 A | "0.00", "0.01", "0.02" | Pump powered but not running |
| **Dry** | Variable | "Dry" | Well is dry, pump protection active |
| **rcyc** | Variable | "rcyc" | Rapid cycling detected |
| **Off** | N/A | Dark/blank | No power to pump or display |

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

## OCR Development

Sample images in the `samples/` directory are used for:
- Testing OCR accuracy across different conditions
- Developing image preprocessing algorithms
- Training text recognition patterns
- Validating pump status detection logic
