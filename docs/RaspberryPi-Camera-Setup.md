# Raspberry Pi Camera Setup for Well Monitoring

## 📷 Camera Installation Guide

### **1. Hardware Setup**

**Required Components**:
- Raspberry Pi 4B
- Raspberry Pi Camera Module (v2 or v3 recommended)
- Camera ribbon cable (usually included)
- Camera mounting hardware (tripod, bracket, or custom mount)

**Physical Installation**:
1. **Power off** your Raspberry Pi completely
2. **Locate camera connector** on Pi (between HDMI ports)
3. **Lift plastic latch** on camera connector
4. **Insert ribbon cable** with contacts facing away from ethernet port
5. **Press latch down** to secure cable
6. **Attach camera module** to other end of ribbon cable
7. **Mount camera** to view your pump's LED display

### **2. Software Configuration**

**Enable Camera Interface**:
```bash
# Open Raspberry Pi configuration
sudo raspi-config

# Navigate to: Interface Options → Camera → Enable
# Select "Yes" and reboot when prompted
sudo reboot
```

**Install Camera Software**:
```bash
# Update package lists
sudo apt update

# Install libcamera (modern camera stack)
sudo apt install -y libcamera-apps libcamera-dev

# Install legacy camera support (if needed)
sudo apt install -y python3-picamera2
```

### **3. Camera Testing**

**Basic Functionality Test**:
```bash
# Test camera capture
libcamera-still -o test_image.jpg --width 1920 --height 1080

# Check if image was created
ls -la test_image.jpg

# View image properties
file test_image.jpg
```

**Well Monitor Application Test**:
```bash
# Test with Well Monitor settings
libcamera-still -o pump_test.jpg \
  --width 1920 \
  --height 1080 \
  --quality 85 \
  --timeout 30000

# Check image quality for OCR
# Image should clearly show current reading digits
```

### **4. Camera Positioning**

**Optimal Placement**:
- **Distance**: 6-12 inches from LED display
- **Angle**: Perpendicular to display surface (avoid reflections)
- **Lighting**: Ensure adequate ambient light or add LED strip
- **Stability**: Use tripod or secure mounting to prevent vibration

**Framing Guidelines**:
- **Center the display** in camera view
- **Fill frame** with display area (not too wide)
- **Focus on digits** - current readings should be clearly visible
- **Avoid shadows** or reflections on display

### **5. Integration with Well Monitor**

**Camera Configuration** (via device twin):
```json
{
  "properties": {
    "desired": {
      "camera": {
        "width": 1920,
        "height": 1080,
        "quality": 85,
        "timeoutMs": 30000,
        "warmupTimeMs": 2000,
        "rotation": 0,
        "brightness": 50,
        "contrast": 0,
        "saturation": 0,
        "enablePreview": false,
        "debugImagePath": "debug_images"
      }
    }
  }
}
```

**Debug Image Collection**:
```bash
# Enable debug images to tune OCR
mkdir -p /home/pi/wellmonitor/debug_images

# Images will be saved with timestamps:
# debug_images/pump_reading_20250711_143022.jpg
```

### **6. Troubleshooting**

**Camera Not Detected**:
```bash
# Check camera detection
libcamera-hello --list-cameras

# If no cameras found, check:
# 1. Cable connection
# 2. Camera interface enabled
# 3. Reboot after enabling
```

**Image Quality Issues**:
```bash
# Test different settings
libcamera-still -o test_bright.jpg --brightness 70
libcamera-still -o test_contrast.jpg --contrast 10
libcamera-still -o test_rotation.jpg --rotation 180

# Fine-tune for your specific display
```

**OCR Accuracy Problems**:
1. **Lighting**: Add consistent LED lighting
2. **Focus**: Ensure camera is properly focused
3. **Angle**: Adjust camera angle to minimize glare
4. **Distance**: Move closer for larger digit size
5. **Settings**: Adjust brightness/contrast for your display

### **7. Production Deployment**

**Secure Mounting**:
- Use weatherproof enclosure if outdoors
- Ensure vibration-resistant mounting
- Protect cable connections from moisture

**Regular Maintenance**:
- Clean camera lens monthly
- Check mounting stability
- Verify image quality periodically

**Performance Monitoring**:
```bash
# Monitor OCR accuracy in logs
sudo journalctl -u wellmonitor -f | grep -E "(OCR|Confidence|Valid)"

# Check debug images periodically
ls -la /home/pi/wellmonitor/debug_images/
```

## 🎯 Testing Checklist

- [ ] Camera physically installed and secured
- [ ] Camera interface enabled in raspi-config
- [ ] Test image capture works with libcamera-still
- [ ] Camera positioned to clearly view LED display
- [ ] Well Monitor service can capture images
- [ ] Debug images show clear current readings
- [ ] OCR extraction works with high confidence
- [ ] Pump status detection working correctly

## 📞 Support

If you encounter issues:

1. **Check hardware connections**
2. **Verify camera interface enabled**
3. **Test with libcamera-still first**
4. **Review debug images for quality**
5. **Monitor logs for OCR confidence scores**

The camera setup is critical for reliable OCR performance. Take time to properly position and configure for optimal results!
