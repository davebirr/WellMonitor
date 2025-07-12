# Raspberry Pi OCR Troubleshooting Guide

This guide helps resolve common OCR-related issues when running the WellMonitor application on Raspberry Pi.

## Quick Fix: Install Tesseract

The most common issue is missing Tesseract OCR installation. Run this script:

```bash
# Make the script executable
chmod +x scripts/install-tesseract-pi.sh

# Run the installation script
./scripts/install-tesseract-pi.sh
```

## Manual Tesseract Installation

If the script doesn't work, install manually:

```bash
# Update package lists
sudo apt update

# Install Tesseract OCR
sudo apt install -y tesseract-ocr tesseract-ocr-eng libtesseract-dev

# Verify installation
tesseract --version
tesseract --list-langs
```

## Common OCR Error Messages

### "Tesseract provider not initialized"

**Cause**: Tesseract OCR is not installed or tessdata files are missing.

**Solution**:
1. Install Tesseract using the script above
2. Restart the service: `sudo systemctl restart wellmonitor.service`
3. Check logs: `sudo journalctl -u wellmonitor.service -f`

### "Could not find tessdata directory"

**Cause**: Tesseract language data files are missing.

**Solution**:
```bash
# Install English language data
sudo apt install -y tesseract-ocr-eng

# Check if tessdata directory exists
ls -la /usr/share/tesseract-ocr/*/tessdata/
ls -la /usr/share/tessdata/
```

### OCR returns empty results

**Cause**: Image preprocessing or OCR configuration issues.

**Solution**:
1. Check debug images in `debug_images/` directory
2. Verify camera is capturing clear images
3. Adjust OCR parameters via device twin configuration

## Diagnostic Commands

### Check Application Status
```bash
# Service status
sudo systemctl status wellmonitor.service

# View real-time logs
sudo journalctl -u wellmonitor.service -f

# View recent logs
sudo journalctl -u wellmonitor.service --since "10 minutes ago"
```

### Check Tesseract Installation
```bash
# Check if Tesseract is installed
which tesseract
tesseract --version

# List available languages
tesseract --list-langs

# Check tessdata directory
find /usr -name tessdata 2>/dev/null
ls -la /usr/share/tesseract-ocr/*/tessdata/ 2>/dev/null
```

### Check File Permissions
```bash
# Ensure pi user can access camera
groups pi | grep video

# Add pi to video group if needed
sudo usermod -a -G video pi

# Check tessdata permissions
ls -la /usr/share/tesseract-ocr/*/tessdata/
```

## Application Logs Analysis

### Successful OCR Initialization
Look for these log messages:
```
info: HardwareInitializationService[0] Starting hardware initialization...
info: OcrDiagnosticsService[0] Starting OCR diagnostics...
info: OcrDiagnosticsService[0] Found Tesseract at: /usr/bin/tesseract
info: HardwareInitializationService[0] Initializing Tesseract OCR provider...
info: HardwareInitializationService[0] Tesseract OCR provider initialized successfully
```

### OCR Provider Failures
Look for these error patterns:
```
warn: HardwareInitializationService[0] Tesseract OCR provider failed to initialize
System.InvalidOperationException: Tesseract provider not initialized
DirectoryNotFoundException: Could not find tessdata directory
```

## Device Twin Configuration

### OCR Provider Selection
Configure which OCR provider to use via device twin:
```json
{
  "properties": {
    "desired": {
      "ocrProvider": "Tesseract",
      "ocrFallbackEnabled": true
    }
  }
}
```

### Tesseract-Specific Settings
```json
{
  "properties": {
    "desired": {
      "tesseractLanguage": "eng",
      "tesseractEngineMode": "Default",
      "tesseractPageSegmentationMode": "Auto"
    }
  }
}
```

## Debug Image Analysis

### Enable Debug Images
Configure via device twin:
```json
{
  "properties": {
    "desired": {
      "cameraDebugImagePath": "debug_images",
      "debugImageEnabled": true
    }
  }
}
```

### Analyze Debug Images
1. Check `debug_images/` directory for saved images
2. Verify images are clear and show LED display clearly
3. Look for proper contrast and lighting
4. Ensure display text is readable

## Performance Optimization

### Reduce OCR Processing Time
```json
{
  "properties": {
    "desired": {
      "ocrMaxRetries": 2,
      "ocrTimeoutSeconds": 10,
      "imageProcessingEnabled": true
    }
  }
}
```

### Memory Usage
```bash
# Check memory usage
free -h
ps aux | grep wellmonitor

# Monitor memory over time
watch -n 5 'free -h && echo "--- WellMonitor Process ---" && ps aux | grep wellmonitor'
```

## Network and Azure Connectivity

### Test IoT Hub Connection
```bash
# Check network connectivity
ping google.com

# Check if secrets file exists
ls -la /home/pi/.wellmonitor-secrets.json

# Check Azure IoT Hub connection in logs
sudo journalctl -u wellmonitor.service | grep "IoT Hub\|device twin\|telemetry"
```

## Step-by-Step Troubleshooting

1. **Install Tesseract**: Run `./scripts/install-tesseract-pi.sh`
2. **Update Repository**: `git pull origin main`
3. **Restart Service**: `sudo systemctl restart wellmonitor.service`
4. **Check Logs**: `sudo journalctl -u wellmonitor.service -f`
5. **Verify OCR**: Look for "OCR provider initialized successfully"
6. **Test Image Capture**: Check for "Successfully captured image" messages
7. **Monitor Debug Images**: Verify images are saved to `debug_images/`

## Getting Help

If issues persist:

1. **Collect Logs**: `sudo journalctl -u wellmonitor.service --since "1 hour ago" > wellmonitor-logs.txt`
2. **Run Diagnostics**: Check startup logs for OCR diagnostics output
3. **Check System Resources**: Monitor CPU, memory, and disk usage
4. **Verify Configuration**: Ensure device twin parameters are properly set

## Common Solutions Summary

| Error | Quick Fix |
|-------|-----------|
| Tesseract not initialized | `sudo apt install tesseract-ocr tesseract-ocr-eng` |
| Camera access denied | `sudo usermod -a -G video pi` |
| Service won't start | Check dependency injection registration in logs |
| OCR returns empty results | Enable debug images and check image quality |
| High memory usage | Reduce OCR retries and enable image cleanup |
| Network connectivity | Verify IoT Hub connection string in secrets file |
