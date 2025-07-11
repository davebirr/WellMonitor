# Camera Service Implementation

## Overview

The `CameraService` has been implemented to capture images from the Raspberry Pi Camera V2 using the modern `libcamera-still` command. This replaces the older `raspistill` command and provides better compatibility with newer Raspberry Pi OS versions.

## Key Features

✅ **Modern libcamera Integration**: Uses `libcamera-still` for Pi Camera V2 support
✅ **Configurable Options**: Comprehensive camera settings via `CameraOptions` class
✅ **Timeout Protection**: Prevents hanging on camera issues
✅ **Error Handling**: Graceful handling of missing camera or command failures
✅ **Debug Support**: Optional image saving for troubleshooting OCR processing
✅ **Process Management**: Proper cleanup of temporary files and processes

## Camera Configuration

The `CameraOptions` class provides comprehensive configuration:

```csharp
public class CameraOptions
{
    public int Width { get; set; } = 1920;           // Image width in pixels
    public int Height { get; set; } = 1080;          // Image height in pixels
    public int Quality { get; set; } = 85;           // JPEG quality (0-100)
    public int TimeoutMs { get; set; } = 30000;      // Process timeout in milliseconds
    public int WarmupTimeMs { get; set; } = 2000;    // Camera warmup time
    public int Rotation { get; set; } = 0;           // Image rotation (0, 90, 180, 270)
    public int Brightness { get; set; } = 50;        // Brightness adjustment (0-100)
    public int Contrast { get; set; } = 0;           // Contrast adjustment (-100 to 100)
    public int Saturation { get; set; } = 0;         // Saturation adjustment (-100 to 100)
    public bool EnablePreview { get; set; } = false; // Enable camera preview
    public string? DebugImagePath { get; set; }      // Path to save debug images
}
```

## Usage Example

The camera service is already integrated into the application startup in `Program.cs`:

```csharp
// Camera options are configured with sensible defaults
var cameraOptions = new CameraOptions
{
    Width = 1920,
    Height = 1080,
    Quality = 85,
    TimeoutMs = 30000,
    WarmupTimeMs = 2000,
    Rotation = 0,
    Brightness = 50,
    Contrast = 0,
    Saturation = 0,
    EnablePreview = false,
    DebugImagePath = "./debug_images" // Save debug images for troubleshooting
};

// Service is registered in DI container
services.AddSingleton(cameraOptions);
services.AddSingleton<ICameraService, CameraService>();
```

## Current Status

🎯 **READY FOR TESTING**: The camera service is fully implemented and ready for testing on the Raspberry Pi.

### What's Implemented:
- ✅ Complete `CameraService` with `libcamera-still` integration
- ✅ `CameraOptions` configuration class with all settings
- ✅ Proper dependency injection registration in `Program.cs`
- ✅ Error handling and timeout protection
- ✅ Debug image saving capability
- ✅ Unit tests for service construction and configuration validation
- ✅ Cross-platform build support (Windows development, Linux ARM64 deployment)

### What's Next:
- 🎯 **Test on RPi**: Deploy and test actual image capture on Raspberry Pi
- 🎯 **Camera positioning**: Adjust camera angle and position to capture pump display
- 🎯 **Image quality tuning**: Adjust brightness, contrast, and focus for best OCR results
- 🎯 **OCR Integration**: Implement text extraction from captured images
- 🎯 **Pump reading parsing**: Parse OCR results to extract current draw and status

## Testing on Raspberry Pi

To test the camera service on your Raspberry Pi:

1. **Deploy the application**:
   ```bash
   # Copy the published files to your RPi
   scp -r ./bin/Release/net8.0/linux-arm64/publish/* pi@your-pi-ip:/home/pi/wellmonitor/
   ```

2. **Run the application**:
   ```bash
   # SSH to your RPi and run
   cd /home/pi/wellmonitor
   ./WellMonitor.Device
   ```

3. **Check debug images**:
   ```bash
   # Debug images will be saved to ./debug_images/ if configured
   ls -la ./debug_images/
   ```

## Hardware Requirements

- Raspberry Pi 4B with Raspberry Pi OS (Bullseye or later)
- Raspberry Pi Camera V2 connected to camera port
- `libcamera-still` command available (included in modern Raspberry Pi OS)

## Troubleshooting

### Common Issues:

1. **Camera not detected**:
   - Check camera cable connection
   - Verify camera is enabled: `sudo raspi-config` → Interface Options → Camera
   - Test with: `libcamera-still --list-cameras`

2. **Permission issues**:
   - Add user to video group: `sudo usermod -a -G video $USER`
   - Restart or log out/in for changes to take effect

3. **libcamera-still not found**:
   - Update Raspberry Pi OS: `sudo apt update && sudo apt upgrade`
   - Install if missing: `sudo apt install libcamera-apps`

4. **Low image quality**:
   - Adjust `CameraOptions.Quality` (higher = better quality)
   - Tune `Brightness` and `Contrast` settings
   - Ensure good lighting on pump display

## Debug Mode

Set `CameraOptions.DebugImagePath` to save captured images for troubleshooting:
- Images saved with timestamp: `pump_reading_20250106_143022.jpg`
- Useful for tuning OCR processing without camera hardware
- Can be analyzed later for optimal OCR settings

This implementation provides a solid foundation for the pump monitoring system. The next step is to test image capture on the actual Raspberry Pi hardware and then implement OCR processing for pump reading extraction.
