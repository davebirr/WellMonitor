# Debug Image Troubleshooting Guide

## Issue: Debug images not being saved despite `"debugImageSaveEnabled": true`

### Fixed Issues ✅
1. **CameraService Logic**: Fixed to check both `DebugOptions.ImageSaveEnabled` AND `CameraOptions.DebugImagePath`
2. **Enhanced Logging**: Added detailed debug logging to help identify configuration issues

### Required Device Twin Configuration
Your device twin's `desired` properties must include **BOTH** settings:

```json
{
  "properties": {
    "desired": {
      "debugImageSaveEnabled": true,
      "cameraDebugImagePath": "debug_images"
    }
  }
}
```

### Troubleshooting Steps

#### 1. Verify Device Twin Configuration
Check your Azure IoT Hub device twin has both settings:
- `"debugImageSaveEnabled": true` 
- `"cameraDebugImagePath": "debug_images"`

#### 2. Check Application Logs
Look for these log messages in your application output:

**Good signs:**
```
Debug image check: ImageSaveEnabled=True, DebugImagePath='debug_images'
Saving debug image...
Debug image saved to: /path/to/debug_images/pump_reading_20250712_143022.jpg
```

**Problem indicators:**
```
Debug image saving is disabled (debugImageSaveEnabled=false)
Debug image saving is enabled but cameraDebugImagePath is not configured in device twin
```

#### 3. Set Debug Logging Level
To see detailed camera capture logs, set your log level to `Debug` in device twin:
```json
{
  "logLevel": "Debug"
}
```

#### 4. Check Directory Permissions (Raspberry Pi)
Ensure the application can write to the debug_images directory:
```bash
# Check if directory exists and is writable
ls -la debug_images/
chmod 755 debug_images/
```

#### 5. Manual Test
You can run the diagnostic tool by adding this to your Program.cs temporarily:
```csharp
// Add after service registration
var diagnostic = serviceProvider.GetRequiredService<DebugImageDiagnostic>();
diagnostic.DiagnoseDebugImageConfiguration();
```

### Expected Behavior After Fix
1. Application loads device twin configuration at startup
2. Every 30 seconds (default monitoring interval), camera captures image
3. If both `debugImageSaveEnabled=true` AND `cameraDebugImagePath` is set, saves debug image
4. Debug images saved as: `pump_reading_YYYYMMDD_HHMMSS.jpg`
5. Images saved to: `{app_directory}/debug_images/` (or absolute path if specified)

### Common Issues
- **Only set `debugImageSaveEnabled`**: Won't work, needs both settings
- **Path permissions**: App can't write to debug directory
- **Log level too high**: Debug messages not visible
- **Device twin not synced**: Changes haven't reached device yet

### Quick Device Twin Update
Use Azure CLI to update device twin:
```bash
az iot hub device-twin update \
  --device-id your-device-id \
  --hub-name your-iot-hub \
  --set properties.desired.debugImageSaveEnabled=true \
  --set properties.desired.cameraDebugImagePath="debug_images"
```
