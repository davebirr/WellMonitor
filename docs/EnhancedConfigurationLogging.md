# Enhanced Device Twin Configuration Logging

## Overview

This document describes the enhanced logging capabilities added to the WellMonitor device to provide comprehensive visibility into device twin configuration loading, settings application, and potential issues.

## Key Improvements

### 1. **Camera Configuration Logging**

#### Nested Property Support
- **NEW**: Support for nested `Camera` property structure (preferred)
  - `Camera.Gain` instead of `cameraGain`
  - `Camera.ShutterSpeedMicroseconds` instead of `cameraShutterSpeedMicroseconds`
  - `Camera.AutoExposure` instead of `cameraAutoExposure`
  - All other camera properties follow this pattern

#### Backward Compatibility
- **MAINTAINED**: Legacy flat properties still supported
  - `cameraGain`, `cameraShutterSpeedMicroseconds`, etc.
  - Automatic fallback when nested structure not found
  - Warning when both nested and legacy properties exist

#### Detailed Configuration Source Tracking
```csharp
// Example log output:
✅ Camera settings loaded from device twin: Gain=0.5, ShutterSpeedMicroseconds=5000, AutoExposure=false
🔸 Camera settings using default values: Width=1920 (default), Height=1080 (default)
```

### 2. **Configuration Validation and Warnings**

#### Problematic Setting Detection
- **High Gain Warning**: Gain > 2.0 may cause overexposure with LEDs
- **Long Shutter Speed Warning**: > 20000μs may cause motion blur/overexposure  
- **Auto-Exposure Warning**: May cause inconsistent exposure with LED displays
- **Unusual Threshold Warnings**: Current/cycle time outside typical ranges

#### Sample Warning Output
```
⚠️ High camera gain (12.0) detected - may cause overexposure with bright LEDs
⚠️ Auto-exposure enabled - may cause inconsistent exposure with LED displays
```

### 3. **Comprehensive Configuration Summary**

#### Startup Configuration Logging
```
📸 Final Camera Configuration:
   Image: 1280x720, Quality: 85%, Rotation: 0°
   Timing: Timeout: 5000ms, Warmup: 3000ms
   Visual: Brightness: 30, Contrast: 20, Saturation: 20
   Exposure: Gain: 0.5, Shutter: 5000μs, AutoExposure: false, AutoWhiteBalance: false
   Debug: Preview: false, DebugImagePath: '/var/lib/wellmonitor/debug_images'
```

#### Well Monitor Settings Summary
```
⚙️ Final Well Monitor Configuration:
   Pump Monitoring: CurrentThreshold: 4.5A, CycleTimeThreshold: 30s
   System: RelayDebounce: 500ms, SyncInterval: 5min, LogRetention: 14d
   OCR: Mode: tesseract, PowerApp: true
```

### 4. **Periodic Configuration Monitoring**

#### Hourly Summary Reports
- **Automatic**: Every 60 minutes (12 telemetry cycles)
- **Device Twin Status**: Version tracking and last update time
- **Configuration Source Analysis**: Nested vs legacy vs missing
- **Active Settings Verification**: Current vs device twin comparison

#### Sample Periodic Report
```
📊 Periodic Configuration Summary Report:
🔗 Device Twin Status:
   Desired Properties Version: 17
   Reported Properties Version: 1
   Last Updated: 2025-07-13T15:16:36.5543127Z
   Camera Configuration Source: nested structure
```

### 5. **Device Twin Version Tracking**

#### Version Monitoring
- Track desired vs reported property versions
- Detect configuration drift
- Log device twin last update timestamp
- Identify mixed configuration sources

#### Configuration Drift Detection
```
🔄 Both nested Camera and legacy camera properties found - nested takes precedence
🚨 No camera configuration found in device twin - using all default values!
```

## Implementation Details

### DeviceTwinService Enhancements

#### New Methods
- `UpdateCameraFromNestedConfig()`: Handle nested Camera properties
- `UpdateCameraFromLegacyConfig()`: Backward compatibility for flat properties
- `LogCameraConfiguration()`: Comprehensive camera settings logging
- `LogWellMonitorConfiguration()`: System settings logging
- `LogPeriodicConfigurationSummaryAsync()`: Hourly summary reports

#### Helper Methods
- `UpdateCameraSetting<T>()`: Generic setting update with source tracking
- `LoadConfigValue<T>()`: Generic configuration loading with fallback tracking
- `HasLegacyCameraProperties()`: Detect legacy property usage

### TelemetryBackgroundService Integration

#### Periodic Logging
- Integrated with existing 5-minute telemetry cycle
- Every 12th cycle (60 minutes) triggers configuration summary
- Uses scoped services to access device twin and camera options
- Creates temporary DeviceClient for device twin queries

## Usage Examples

### Monitoring Configuration Changes

#### Real-time Log Monitoring
```bash
# Monitor all logs
ssh pi@raspberrypi.local "sudo journalctl -u wellmonitor -f"

# Filter for configuration-related logs
ssh pi@raspberrypi.local "sudo journalctl -u wellmonitor -f | grep -E '📸|⚙️|📊|✅|🔸|⚠️'"
```

#### Searching for Specific Issues
```bash
# Look for configuration warnings
sudo journalctl -u wellmonitor --since "1 hour ago" | grep "⚠️"

# Check camera configuration sources
sudo journalctl -u wellmonitor --since "1 hour ago" | grep "Camera Configuration Source"

# Find device twin version updates
sudo journalctl -u wellmonitor --since "1 day ago" | grep "Desired Properties Version"
```

### Troubleshooting Common Issues

#### Overexposed Camera Images
1. **Check for warnings**: Look for high gain or long shutter speed warnings
2. **Verify device twin**: Ensure Camera.Gain ≤ 2.0 for LED environments
3. **Monitor application**: Check logs for "Camera settings loaded from device twin"

#### Configuration Not Applied
1. **Check device twin version**: Verify version increments after updates
2. **Source verification**: Ensure using "nested structure" not "legacy flat properties"
3. **Connection verification**: Confirm Azure IoT Hub connection string valid

#### Missing Camera Settings
1. **Default value warnings**: Look for "🔸 Camera settings using default values"
2. **Device twin structure**: Verify Camera object exists in desired properties
3. **Property names**: Ensure using correct nested property names

## Configuration Property Mapping

### New Nested Structure (Preferred)
```json
{
  "properties": {
    "desired": {
      "Camera": {
        "Gain": 0.5,
        "ShutterSpeedMicroseconds": 5000,
        "AutoExposure": false,
        "AutoWhiteBalance": false,
        "Brightness": 30,
        "Contrast": 20,
        "Width": 1280,
        "Height": 720,
        "Quality": 85,
        "Rotation": 0,
        "TimeoutMs": 5000,
        "WarmupTimeMs": 3000,
        "EnablePreview": false,
        "DebugImagePath": "/var/lib/wellmonitor/debug_images"
      }
    }
  }
}
```

### Legacy Flat Structure (Backward Compatible)
```json
{
  "properties": {
    "desired": {
      "cameraGain": 0.5,
      "cameraShutterSpeedMicroseconds": 5000,
      "cameraAutoExposure": false,
      "cameraAutoWhiteBalance": false,
      "cameraBrightness": 30,
      "cameraContrast": 20,
      "cameraWidth": 1280,
      "cameraHeight": 720
    }
  }
}
```

## Deployment

### Using PowerShell (Windows)
```powershell
.\scripts\deployment\deploy-improved-logging.ps1
```

### Using Bash (Linux/macOS)
```bash
./scripts/deployment/deploy-improved-logging.sh
```

Both scripts will:
1. Build the project for ARM64
2. Deploy to Raspberry Pi
3. Restart the service
4. Show recent logs with new configuration logging

## Benefits

### For Troubleshooting
- **Immediate Visibility**: See exactly which settings come from device twin vs defaults
- **Warning System**: Proactive alerts for problematic configurations  
- **Version Tracking**: Detect when device twin updates aren't being applied
- **Source Analysis**: Understand configuration precedence and conflicts

### For Optimization
- **Red LED Environment**: Specific warnings for LED display reading scenarios
- **Performance Monitoring**: Track configuration changes over time
- **Validation Feedback**: Real-time feedback on setting effectiveness

### For Maintenance
- **Hourly Reports**: Automated configuration health checks
- **Drift Detection**: Identify when settings deviate from expected values
- **Backward Compatibility**: Smooth migration from legacy configurations
- **Comprehensive Logging**: Full audit trail of configuration changes
