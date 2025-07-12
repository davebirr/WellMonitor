# Device Twin Camera Configuration - Testing Guide

## ✅ **Current Status**

Your Azure IoT Hub device twin is **perfectly configured** with all camera settings:

### 📊 **Device Twin Configuration (Azure IoT Hub)**
- **Device ID**: `rpi4b-1407well01`
- **Status**: Enabled
- **Last Activity**: 2025-07-10T21:22:20.9440947Z
- **Configuration Version**: 3

### 🎯 **Camera Settings in Device Twin**
```json
{
  "cameraBrightness": 50,
  "cameraContrast": 10,
  "cameraDebugImagePath": "debug_images",
  "cameraEnablePreview": false,
  "cameraHeight": 1080,
  "cameraQuality": 95,
  "cameraRotation": 0,
  "cameraSaturation": 0,
  "cameraTimeoutMs": 5000,
  "cameraWarmupTimeMs": 2000,
  "cameraWidth": 1920
}
```

### 🔧 **Other Well Monitor Settings**
```json
{
  "currentThreshold": 4.5,
  "cycleTimeThreshold": 30,
  "relayDebounceMs": 500,
  "syncIntervalMinutes": 5,
  "logRetentionDays": 14,
  "ocrMode": "tesseract",
  "powerAppEnabled": true
}
```

## 🚀 **Testing Device Twin Configuration**

### **Step 1: Deploy Updated Application**

Since the device twin integration is implemented, deploy the latest version:

```bash
# Build for Raspberry Pi
cd /c/Users/davidb/1Repositories/wellmonitor
dotnet publish src/WellMonitor.Device -c Release -r linux-arm64 --self-contained false -p:PublishSingleFile=true

# Deploy to Raspberry Pi
scp -r src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/publish/* pi@your-pi-ip:/home/pi/wellmonitor/
```

### **Step 2: Test Device Twin Loading**

Run the application and look for these log messages:

```bash
# SSH to your Raspberry Pi
ssh pi@your-pi-ip
cd /home/pi/wellmonitor
./WellMonitor.Device
```

**Expected Log Messages:**
```
info: WellMonitor.Device.Services.DependencyValidationService[0]
      Starting dependency validation...
info: WellMonitor.Device.Services.DependencyValidationService[0]
      Azure IoT Hub connection string found
info: WellMonitor.Device.Services.DependencyValidationService[0]
      Device twin configuration loaded successfully
info: WellMonitor.Device.Services.DependencyValidationService[0]
      Camera config: width=1920, height=1080, quality=95, brightness=50, contrast=10, rotation=0
```

### **Step 3: Test Camera Configuration Changes**

Update camera settings in Azure IoT Hub and verify they take effect:

#### **Via Azure Portal:**
1. Navigate to IoT Hub → Devices → `rpi4b-1407well01`
2. Click "Device Twin"
3. Modify camera settings (e.g., change `cameraBrightness` from 50 to 75)
4. Click "Save"

#### **Via Azure CLI:**
```bash
# Update camera brightness
az iot hub device-twin update \
  --device-id rpi4b-1407well01 \
  --hub-name your-iot-hub-name \
  --set properties.desired.cameraBrightness=75

# Enable debug images
az iot hub device-twin update \
  --device-id rpi4b-1407well01 \
  --hub-name your-iot-hub-name \
  --set properties.desired.cameraDebugImagePath="debug_images"
```

### **Step 4: Verify Configuration Updates**

Restart the application and check logs for new camera configuration:

```bash
# Restart application
sudo systemctl restart wellmonitor  # If using systemd service
# OR
./WellMonitor.Device  # If running manually
```

**Expected Log Messages:**
```
info: WellMonitor.Device.Services.DependencyValidationService[0]
      Camera config: width=1920, height=1080, quality=95, brightness=75, contrast=10, rotation=0
```

## 📋 **Testing Camera Settings**

### **Quality Settings to Test:**
1. **Image Quality**: Try values 75, 85, 95 for different file sizes
2. **Brightness**: Test 25, 50, 75 for different lighting conditions
3. **Contrast**: Test -10, 0, 10, 20 for text clarity
4. **Rotation**: Test 0, 90, 180, 270 for camera mounting position

### **Debug Images Testing:**
1. Enable debug images: `"cameraDebugImagePath": "debug_images"`
2. Run application for a few monitoring cycles
3. Check for saved images: `ls -la debug_images/`
4. Analyze image quality for OCR processing

### **Performance Testing:**
1. **Timeout**: Test 3000ms, 5000ms, 10000ms for different capture speeds
2. **Warmup Time**: Test 1000ms, 2000ms, 3000ms for camera initialization
3. **Resolution**: Test 1920x1080, 1280x720, 640x480 for different quality vs performance

## 🔧 **Common Device Twin Property Names**

| Property | Purpose | Example Values |
|----------|---------|----------------|
| `cameraWidth` | Image width | 1920, 1280, 640 |
| `cameraHeight` | Image height | 1080, 720, 480 |
| `cameraQuality` | JPEG quality | 75, 85, 95 |
| `cameraTimeoutMs` | Capture timeout | 3000, 5000, 10000 |
| `cameraWarmupTimeMs` | Camera warmup | 1000, 2000, 3000 |
| `cameraRotation` | Image rotation | 0, 90, 180, 270 |
| `cameraBrightness` | Brightness adj | 25, 50, 75 |
| `cameraContrast` | Contrast adj | -10, 0, 10, 20 |
| `cameraSaturation` | Saturation adj | -10, 0, 10 |
| `cameraEnablePreview` | Show preview | true, false |
| `cameraDebugImagePath` | Debug save path | "/home/pi/debug" |

## 🎯 **Troubleshooting Device Twin Issues**

### **Device Twin Not Loading:**
1. Check IoT Hub connection string in `secrets.json`
2. Verify device is registered in Azure IoT Hub
3. Check device twin permissions and authentication
4. Look for warning: `"Failed to load device twin configuration"`

### **Camera Settings Not Applied:**
1. Verify device twin has correct property names (case-sensitive)
2. Check application logs for configuration loading messages
3. Restart application after device twin changes
4. Verify camera settings are within valid ranges

### **Connection Issues:**
1. Check network connectivity: `ping your-iot-hub-name.azure-devices.net`
2. Verify SAS token is not expired
3. Check firewall settings for outbound MQTT/AMQP traffic
4. Review Azure IoT Hub metrics for connection attempts

## 🎉 **Success Indicators**

✅ **Device Twin Integration Working:**
- Log message: "Device twin configuration loaded successfully"
- Camera config log shows updated values from device twin
- Debug images saved to specified path
- Camera capture uses updated quality/resolution settings

✅ **Real-time Configuration Changes:**
- Device twin updates in Azure Portal are reflected in application logs
- Camera behavior changes based on device twin settings
- No application restart required for non-critical settings

✅ **Production Ready:**
- Device connects to Azure IoT Hub successfully
- Camera captures images with device twin settings
- Debug images help tune OCR quality
- Remote configuration enables operational flexibility

## 📈 **Next Steps After Device Twin Validation**

1. **OCR Integration**: Process captured images to extract pump readings
2. **Pump State Detection**: Identify "Dry" and "rcyc" conditions
3. **Telemetry Reporting**: Send readings and states to Azure IoT Hub
4. **PowerApp Integration**: Enable remote monitoring and control
5. **Alert System**: Notify operators of abnormal conditions

Your device twin configuration is **production-ready** and perfectly aligned with the application's device twin integration. The next step is to deploy and test on the actual Raspberry Pi hardware.
