# Camera & OCR Setup Guide

Hardware configuration and optimization guide for camera capture and OCR processing.

## Camera Hardware Setup

### Physical Installation
1. **Mount the camera** to point directly at the pump display
2. **Secure positioning** to minimize vibration and movement
3. **Ensure stable lighting** or use manual exposure for LED displays
4. **Keep lens clean** and free from condensation

### Camera Module Verification

**Note**: In modern Raspberry Pi OS (Bullseye and later), the camera is enabled by default and no longer requires configuration through `raspi-config`.

```bash
# Check camera detection
libcamera-hello --list-cameras

# Verify camera hardware
ls -la /dev/video*

# Basic camera test
libcamera-still -o test.jpg --width 1280 --height 720

# Check image quality
display test.jpg  # or transfer to view on another system
```

**Legacy Camera Configuration** (only needed for older Pi OS versions):
```bash
# Only if using older Raspberry Pi OS versions
sudo raspi-config
# Navigate to Interface Options → Camera → Enable (if available)
```

## Camera Configuration for LED Displays

### Dark Basement with Red LED Display

For optimal results with red 7-segment LED displays in dark environments:

```json
{
  "cameraGain": 12.0,
  "cameraShutterSpeedMicroseconds": 50000,
  "cameraAutoExposure": false,
  "cameraAutoWhiteBalance": false,
  "cameraBrightness": 70,
  "cameraContrast": 50,
  "cameraWidth": 1280,
  "cameraHeight": 720
}
```

**Key Settings Explained:**
- **High Gain (12.0)**: Amplifies signal for dark environments
- **50ms Shutter Speed**: Long enough exposure to capture LED segments clearly
- **Manual Exposure**: Prevents auto-exposure from being confused by dark background
- **Manual White Balance**: Prevents color shift with red LED displays

### Bright Environment Configuration

For well-lit displays or LCD panels:

```json
{
  "cameraGain": 1.0,
  "cameraShutterSpeedMicroseconds": 10000,
  "cameraAutoExposure": true,
  "cameraAutoWhiteBalance": true,
  "cameraBrightness": 50,
  "cameraContrast": 50
}
```

### Camera Debug Images

Enable debug image saving to optimize camera settings:

```json
{
  "debugImageSaveEnabled": true,
  "cameraDebugImagePath": "debug_images",
  "debugImageRetentionDays": 7
}
```

Debug images are saved to `/var/lib/wellmonitor/debug_images/` with timestamp format:
- `pump_reading_20250712_143022.jpg`

## OCR Configuration

### OCR Provider Selection

**Tesseract OCR (Recommended)**
- ✅ Works offline
- ✅ No API costs
- ✅ Good for simple numeric displays
- ✅ Character whitelisting support

**Azure Cognitive Services**
- ✅ Higher accuracy for complex text
- ✅ Better handling of poor image quality
- ❌ Requires internet connection
- ❌ API costs per transaction

### Tesseract Configuration

```json
{
  "ocrProvider": "Tesseract",
  "ocrTesseractLanguage": "eng",
  "ocrTesseractEngineMode": 3,
  "ocrTesseractPageSegmentationMode": 7,
  "ocrTesseractCharWhitelist": "0123456789.DryAMPSrcyc ",
  "ocrMinimumConfidence": 0.7
}
```

**Parameter Explanations:**
- **Engine Mode 3**: LSTM (best for most use cases)
- **Page Segmentation Mode 7**: Single text line
- **Character Whitelist**: Only allow expected characters (numbers, "Dry", "rcyc", etc.)
- **Minimum Confidence**: Reject results below 70% confidence

### Image Preprocessing

OCR accuracy improves significantly with proper image preprocessing:

```json
{
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

**Preprocessing Pipeline:**
1. **Grayscale Conversion**: Reduces complexity, improves speed
2. **Contrast Enhancement**: Makes text more distinct from background
3. **Brightness Adjustment**: Compensates for under/over exposure
4. **Noise Reduction**: Removes camera sensor noise
5. **Scaling**: Enlarges small text for better recognition
6. **Binary Thresholding**: Creates high-contrast black/white image

### OCR Testing and Optimization

**Test OCR Configuration:**
```bash
# Capture test image manually
cd /var/lib/wellmonitor/debug_images

# Test Tesseract directly
tesseract latest_image.jpg output.txt --psm 7 -c tessedit_char_whitelist=0123456789.DryAMPSrcyc

# View results
cat output.txt
```

**OCR Performance Monitoring:**
```bash
# Monitor OCR processing in logs
sudo journalctl -u wellmonitor | grep -i "ocr\|confidence\|recognition"

# Check OCR statistics
sudo journalctl -u wellmonitor | grep "OCR.*success\|OCR.*failed"
```

## Display-Specific Configurations

### 7-Segment LED Displays

Best configuration for numeric LED displays showing current readings:

```json
{
  "ocrTesseractPageSegmentationMode": 7,
  "ocrTesseractCharWhitelist": "0123456789.",
  "ocrImagePreprocessing": {
    "enableBinaryThresholding": true,
    "binaryThreshold": 100,
    "enableScaling": true,
    "scaleFactor": 3.0
  }
}
```

### LCD/Text Displays

For displays with text status messages:

```json
{
  "ocrTesseractPageSegmentationMode": 8,
  "ocrTesseractCharWhitelist": "0123456789.DryAMPSrcyc ",
  "statusDetection": {
    "dryKeywords": ["Dry", "No Water", "Empty", "Well Dry"],
    "rapidCycleKeywords": ["rcyc", "Rapid Cycle", "Cycling", "Fault", "Error"],
    "statusMessageCaseSensitive": false
  }
}
```

## Optimization Workflow

### 1. Initial Setup
```bash
# Enable debug images
# Set via device twin: "debugImageSaveEnabled": true

# Capture several test images
sudo systemctl restart wellmonitor
sleep 60
ls -la /var/lib/wellmonitor/debug_images/
```

### 2. Camera Optimization
```bash
# Copy latest image to development machine for analysis
scp pi@raspberry-pi-ip:/var/lib/wellmonitor/debug_images/latest_image.jpg .

# Analyze image quality:
# - Is the display clearly visible?
# - Are LED segments bright and distinct?
# - Is the background too dark or bright?
# - Any motion blur or noise?
```

### 3. OCR Testing
```bash
# Test different OCR settings
tesseract image.jpg output1.txt --psm 7 -c tessedit_char_whitelist=0123456789.
tesseract image.jpg output2.txt --psm 8 -c tessedit_char_whitelist=0123456789.DryAMPS

# Compare results
cat output1.txt
cat output2.txt
```

### 4. Device Twin Updates
Use PowerShell scripts to update camera and OCR settings:

```powershell
# Update camera settings for LED display
.\scripts\Update-LedCameraSettings.ps1

# Test new configuration
.\scripts\Test-LedCameraOptimization.ps1
```

### 5. Validation
```bash
# Monitor OCR success rate
sudo journalctl -u wellmonitor -f | grep -i "ocr\|confidence"

# Check recent readings
sudo -u wellmonitor sqlite3 /var/lib/wellmonitor/wellmonitor.db "SELECT * FROM readings ORDER BY timestamp DESC LIMIT 10;"
```

## Camera DMA Error Troubleshooting

If you encounter "Could not open any dmaHeap device" errors:

**Common Causes:**
1. **Insufficient GPU memory**: The camera requires adequate GPU memory allocation
2. **Camera interface disabled**: Some Pi OS versions may have camera disabled
3. **Hardware conflicts**: Multiple processes trying to access camera simultaneously

**Solutions:**

1. **Increase GPU Memory Split**:
```bash
# Edit boot configuration
sudo nano /boot/config.txt

# Add or modify this line (allocate 128MB to GPU)
gpu_mem=128

# Reboot to apply changes
sudo reboot
```

2. **Enable Camera Interface** (if needed):
```bash
# Check if camera interface is enabled
grep -E "^(start_x|camera_auto_detect)" /boot/config.txt

# Enable camera if not present
echo "camera_auto_detect=1" | sudo tee -a /boot/config.txt
echo "start_x=1" | sudo tee -a /boot/config.txt

# Disable camera LED (optional)
echo "disable_camera_led=1" | sudo tee -a /boot/config.txt

sudo reboot
```

3. **Check for Process Conflicts**:
```bash
# Kill any processes using the camera
sudo pkill -f libcamera
sudo pkill -f rpicam

# Restart WellMonitor service
sudo systemctl restart wellmonitor
```

4. **Verify Camera Module**:
```bash
# Check camera detection
libcamera-hello --list-cameras

# Test basic capture
libcamera-still -o test.jpg --timeout 2000 --nopreview

# If the above works, the camera hardware is functional
```

## Troubleshooting

### Poor OCR Accuracy

**Symptoms:**
- Low confidence scores
- Incorrect number recognition
- Missing text detection

**Solutions:**
1. **Improve lighting** or adjust camera exposure
2. **Clean camera lens** and display surface
3. **Adjust image preprocessing** settings
4. **Use character whitelisting** to reduce false positives
5. **Test different page segmentation modes**

### Camera Issues

**Symptoms:**
- "Could not open any dmaHeap device" errors
- "rpicam-apps currently only supports the Raspberry Pi platforms" 
- Dark or overexposed images
- Blurry captures
- No images captured

**Solutions:**

1. **Fix DMA/GPU Memory Issues**:
```bash
# Use automated fix script
./scripts/maintenance/fix-camera-dma-error.sh --fix

# Or manual fixes:
# Increase GPU memory
sudo nano /boot/config.txt
# Add: gpu_mem=128

# Enable camera (if needed)
echo "camera_auto_detect=1" | sudo tee -a /boot/config.txt

# Reboot after boot config changes
sudo reboot
```

2. **Check camera hardware**: 
```bash
libcamera-hello --list-cameras
ls -la /dev/video*
```

3. **Test manual capture**: 
```bash
libcamera-still -o test.jpg --timeout 2000 --nopreview
```

4. **Kill conflicting processes**:
```bash
sudo pkill -f libcamera
sudo pkill -f rpicam
sudo systemctl restart wellmonitor
```

5. **Verify service permissions** and systemd configuration

**Note**: Modern Raspberry Pi OS (Bullseye+) has camera enabled by default. The DMA error usually indicates insufficient GPU memory allocation.

### Performance Issues

**Symptoms:**
- Slow OCR processing
- High CPU usage
- Service timeouts

**Solutions:**
1. **Reduce image resolution** if acceptable quality
2. **Disable unnecessary preprocessing** steps
3. **Increase OCR timeout** settings
4. **Monitor system resources**: `top`, `htop`

## Hardware Recommendations

### Camera Modules
- **Raspberry Pi Camera Module v2**: Good general purpose
- **Raspberry Pi HQ Camera**: Better low-light performance
- **USB Webcam**: Alternative with manual focus

### Mounting Hardware
- **Adjustable mount**: For precise positioning
- **Vibration dampening**: To prevent motion blur
- **Weatherproof housing**: For outdoor installations

### Lighting (if needed)
- **LED ring light**: Even illumination around camera
- **Infrared illuminator**: For night vision cameras
- **Diffuser**: To reduce glare from shiny displays

For device twin configuration details, see [Configuration Guide](configuration-guide.md).
For Azure service setup, see [Azure Integration](azure-integration.md).
