# WellMonitor Troubleshooting Guide

Common issues, solutions, and diagnostic procedures for the WellMonitor application.

## Quick Diagnostics

### Service Status Check
```bash
# Quick health check
sudo systemctl status wellmonitor
sudo journalctl -u wellmonitor -n 10 --no-pager

# If service is not running
sudo systemctl start wellmonitor
sudo journalctl -u wellmonitor -f
```

### Application Health Check
```bash
# Check if application files exist
ls -la /opt/wellmonitor/
ls -la /var/lib/wellmonitor/

# Check environment configuration
sudo cat /etc/wellmonitor/environment

# Verify database accessibility
sudo -u wellmonitor sqlite3 /var/lib/wellmonitor/wellmonitor.db ".tables"
```

## Common Issues and Solutions

### Issue: Service Fails to Start with SIGABRT

**Symptoms:**
- Service status shows "failed" or "inactive"
- Logs show "Process: ... ExitCode=killed, Signal=ABRT"

**Causes & Solutions:**

1. **Missing Environment Variables**
   ```bash
   # Check environment configuration
   sudo cat /etc/wellmonitor/environment
   
   # Ensure required variables are set
   sudo nano /etc/wellmonitor/environment
   # Add: WELLMONITOR_IOTHUB_CONNECTION_STRING=your-connection-string
   
   sudo systemctl restart wellmonitor
   ```

2. **Database Permission Issues**
   ```bash
   # Fix database ownership
   sudo chown wellmonitor:wellmonitor /var/lib/wellmonitor/wellmonitor.db
   sudo chmod 644 /var/lib/wellmonitor/wellmonitor.db
   ```

3. **Missing Dependencies**
   ```bash
   # Check .NET runtime
   dotnet --list-runtimes
   
   # Reinstall if missing
   sudo apt install dotnet-runtime-8.0
   ```

### Issue: Camera Access Denied

**Symptoms:**
- Logs show "Camera initialization failed"
- "Permission denied" errors for `/dev/video0`

**Solutions:**
```bash
# Check camera device existence
ls -la /dev/video*

# Add wellmonitor user to video group
sudo usermod -a -G video wellmonitor

# Verify camera access in service
sudo systemctl show wellmonitor | grep DeviceAllow

# Test camera manually
sudo -u wellmonitor libcamera-hello --list-cameras
```

### Issue: GPIO Access Denied

**Symptoms:**
- "GPIO initialization failed" in logs
- Relay control not working

**Solutions:**
```bash
# Check GPIO devices
ls -la /dev/gpiochip*

# Verify GPIO access in service
sudo systemctl show wellmonitor | grep DeviceAllow

# Test GPIO manually
sudo -u wellmonitor gpiodetect
```

### Issue: OCR Errors

**Symptoms:**
- "OCR processing failed" errors
- No text recognition from images

**Solutions:**
```bash
# Check Tesseract installation
tesseract --version

# Verify language data
ls -la /usr/share/tesseract-ocr/*/tessdata/

# Check debug images
ls -la /var/lib/wellmonitor/debug_images/

# Test OCR manually
cd /var/lib/wellmonitor/debug_images
tesseract latest_image.jpg output.txt
cat output.txt
```

### Issue: Azure IoT Hub Connection Failed

**Symptoms:**
- "Failed to connect to IoT Hub" errors
- Telemetry not being sent

**Solutions:**
```bash
# Test internet connectivity
ping -c 4 8.8.8.8

# Check IoT Hub connection string
sudo grep IOTHUB /etc/wellmonitor/environment

# Test DNS resolution
nslookup your-hub.azure-devices.net

# Verify connection string format
# Should be: HostName=your-hub.azure-devices.net;DeviceId=your-device;SharedAccessKey=your-key
```

### Issue: High Memory Usage

**Symptoms:**
- Service using excessive memory (>500MB)
- System becoming slow or unresponsive

**Solutions:**
```bash
# Check memory usage
ps aux | grep wellmonitor
free -h

# Check for memory leaks in logs
sudo journalctl -u wellmonitor | grep -i "memory\|leak\|out of memory"

# Restart service to clear memory
sudo systemctl restart wellmonitor

# Monitor memory usage over time
watch 'ps aux | grep wellmonitor'
```

### Issue: Database Corruption

**Symptoms:**
- "Database is locked" errors
- SQLite errors in logs

**Solutions:**
```bash
# Check database integrity
sudo -u wellmonitor sqlite3 /var/lib/wellmonitor/wellmonitor.db "PRAGMA integrity_check;"

# If corrupted, restore from backup
sudo systemctl stop wellmonitor
sudo -u wellmonitor cp /var/lib/wellmonitor/wellmonitor.db.backup.* /var/lib/wellmonitor/wellmonitor.db
sudo systemctl start wellmonitor

# If no backup, recreate database
sudo -u wellmonitor rm /var/lib/wellmonitor/wellmonitor.db
sudo systemctl start wellmonitor
```

### Issue: Camera DMA Error - "Could not open any dmaHeap device"

**Symptoms:**
- Service running but no debug images created
- Logs show "Could not open any dmaHeap device"
- Error: "rpicam-apps currently only supports the Raspberry Pi platforms"

**Causes:**
- Insufficient GPU memory allocation
- Camera interface not properly enabled
- Conflicting camera processes

**Solutions:**

1. **Quick Fix** (automated):
   ```bash
   # Use automated fix script
   cd ~/WellMonitor
   ./scripts/maintenance/fix-camera-dma-error.sh --fix
   ```

2. **Manual Fix**:
   ```bash
   # Increase GPU memory allocation
   sudo nano /boot/config.txt
   # Add or modify: gpu_mem=128
   
   # Enable camera (if needed)
   echo "camera_auto_detect=1" | sudo tee -a /boot/config.txt
   
   # Kill conflicting processes
   sudo pkill -f libcamera
   sudo pkill -f rpicam
   
   # Reboot to apply boot config changes
   sudo reboot
   
   # After reboot, restart service
   sudo systemctl restart wellmonitor
   ```

3. **Verification**:
   ```bash
   # Test camera manually
   libcamera-hello --list-cameras
   libcamera-still -o test.jpg --timeout 2000 --nopreview
   
   # Monitor service logs
   sudo journalctl -u wellmonitor -f | grep -i camera
   
   # Check for new debug images
   ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/
   ```

**Note**: Modern Raspberry Pi OS (Bullseye+) has camera enabled by default, but may still need GPU memory configuration.

## Diagnostic Procedures

### Comprehensive System Check

Run this diagnostic script to collect system information:

```bash
#!/bin/bash
echo "=== WellMonitor Diagnostic Report ==="
echo "Date: $(date)"
echo ""

echo "=== Service Status ==="
sudo systemctl status wellmonitor --no-pager
echo ""

echo "=== Recent Logs (last 20 lines) ==="
sudo journalctl -u wellmonitor -n 20 --no-pager
echo ""

echo "=== Environment Configuration ==="
sudo cat /etc/wellmonitor/environment 2>/dev/null || echo "Environment file not found"
echo ""

echo "=== File Permissions ==="
ls -la /opt/wellmonitor/ 2>/dev/null || echo "/opt/wellmonitor not found"
ls -la /var/lib/wellmonitor/ 2>/dev/null || echo "/var/lib/wellmonitor not found"
echo ""

echo "=== Hardware Access ==="
ls -la /dev/video* 2>/dev/null || echo "No video devices found"
ls -la /dev/gpiochip* 2>/dev/null || echo "No GPIO devices found"
echo ""

echo "=== System Resources ==="
free -h
df -h | grep -E "(Filesystem|/dev/root|/dev/mmcblk)"
echo ""

echo "=== Network Connectivity ==="
ping -c 2 8.8.8.8 >/dev/null 2>&1 && echo "Internet: OK" || echo "Internet: FAILED"
echo ""

echo "=== .NET Runtime ==="
dotnet --list-runtimes 2>/dev/null || echo ".NET runtime not found"
```

### Manual Testing

To test the application manually outside of systemd:

```bash
# Stop the service
sudo systemctl stop wellmonitor

# Test manual execution
sudo -u wellmonitor bash -c 'cd /opt/wellmonitor && ./WellMonitor.Device'

# If that fails, try with full environment
sudo -u wellmonitor bash -c '
source /etc/wellmonitor/environment
cd /opt/wellmonitor
./WellMonitor.Device
'
```

### Log Analysis

Common log patterns to look for:

```bash
# Error patterns
sudo journalctl -u wellmonitor | grep -i "error\|exception\|failed\|abort"

# Azure IoT patterns
sudo journalctl -u wellmonitor | grep -i "iothub\|azure\|telemetry"

# Hardware patterns
sudo journalctl -u wellmonitor | grep -i "camera\|gpio\|hardware"

# OCR patterns
sudo journalctl -u wellmonitor | grep -i "ocr\|tesseract\|image"
```

## Performance Issues

### Slow Startup
```bash
# Check service startup time
systemd-analyze blame | grep wellmonitor

# Check dependencies
systemd-analyze critical-chain wellmonitor

# Review startup logs
sudo journalctl -u wellmonitor --since "10 minutes ago"
```

### Poor OCR Performance
```bash
# Check image quality
ls -la /var/lib/wellmonitor/debug_images/
# Recent images should be clear and well-lit

# Test different OCR settings via device twin
# See Configuration Guide for OCR tuning

# Monitor OCR processing time
sudo journalctl -u wellmonitor | grep -i "ocr.*time\|processing.*ms"
```

## Getting Help

### Information to Collect

When reporting issues, provide:

1. **System Information**
   ```bash
   uname -a
   cat /etc/os-release
   ```

2. **Service Status**
   ```bash
   sudo systemctl status wellmonitor --no-pager -l
   ```

3. **Recent Logs**
   ```bash
   sudo journalctl -u wellmonitor -n 50 --no-pager
   ```

4. **Configuration**
   ```bash
   sudo cat /etc/wellmonitor/environment
   ls -la /opt/wellmonitor/
   ls -la /var/lib/wellmonitor/
   ```

5. **Hardware Status**
   ```bash
   ls -la /dev/video* /dev/gpiochip*
   ```

### Emergency Recovery

If the system is completely unresponsive:

1. **Stop the service**
   ```bash
   sudo systemctl stop wellmonitor
   sudo systemctl disable wellmonitor
   ```

2. **Clean reinstall**
   ```bash
   cd ~/WellMonitor
   git pull
   ./scripts/install-wellmonitor-complete.sh --clean
   ```

3. **Restore from backup if needed**
   ```bash
   sudo -u wellmonitor cp /var/lib/wellmonitor/wellmonitor.db.backup.* /var/lib/wellmonitor/wellmonitor.db
   ```

For additional support, refer to:
- [Installation Guide](installation-guide.md) for setup issues
- [Service Management](service-management.md) for operational procedures
- [Configuration Guide](../configuration/configuration-guide.md) for settings problems
