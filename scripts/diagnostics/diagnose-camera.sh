#!/bin/bash

# Camera Diagnostics Script for WellMonitor
# Helps diagnose grey square issues and camera problems

echo "🔍 WellMonitor Camera Diagnostics"
echo "================================="
echo

# Check if camera is detected
echo "1. Camera Detection:"
echo "   Checking for camera hardware..."

# List camera devices
if command -v libcamera-hello &> /dev/null; then
    echo "   ✅ libcamera-hello is available"
    
    # Try to detect cameras
    echo "   Detecting cameras..."
    timeout 10s libcamera-hello --list-cameras 2>&1 | head -20
    echo
else
    echo "   ❌ libcamera-hello not found"
    echo "   Install with: sudo apt update && sudo apt install libcamera-apps"
    echo
fi

# Check camera interface
echo "2. Camera Interface Status:"
if grep -q "camera_auto_detect=1" /boot/config.txt 2>/dev/null; then
    echo "   ✅ Camera auto-detect enabled"
elif grep -q "start_x=1" /boot/config.txt 2>/dev/null; then
    echo "   ✅ Legacy camera support enabled"
else
    echo "   ⚠️  Camera may not be enabled in /boot/config.txt"
    echo "   Run: sudo raspi-config → Interface Options → Camera → Enable"
fi

# Check camera permissions
echo
echo "3. Camera Permissions:"
groups | grep -q video && echo "   ✅ User is in video group" || echo "   ⚠️  User not in video group"

# Test basic camera capture
echo
echo "4. Basic Camera Test:"
echo "   Testing camera capture..."

TEST_DIR="/tmp/wellmonitor-camera-test"
mkdir -p "$TEST_DIR"

# Test 1: Simple capture
echo "   Test 1: Simple capture (5 second timeout)..."
if timeout 10s libcamera-still --output "$TEST_DIR/test1.jpg" --timeout 5000 --width 640 --height 480 --nopreview 2>/dev/null; then
    if [ -f "$TEST_DIR/test1.jpg" ]; then
        SIZE=$(stat -c%s "$TEST_DIR/test1.jpg" 2>/dev/null || echo "0")
        echo "   ✅ Basic capture successful ($SIZE bytes)"
    else
        echo "   ❌ Capture command succeeded but no file created"
    fi
else
    echo "   ❌ Basic capture failed"
    echo "   Error details:"
    timeout 10s libcamera-still --output "$TEST_DIR/test1.jpg" --timeout 5000 --width 640 --height 480 --nopreview
fi

# Test 2: Immediate capture (WellMonitor style)
echo
echo "   Test 2: Immediate capture (WellMonitor style)..."
if timeout 10s libcamera-still --output "$TEST_DIR/test2.jpg" --width 1920 --height 1080 --quality 85 --timeout 2000 --encoding jpg --immediate --nopreview 2>/dev/null; then
    if [ -f "$TEST_DIR/test2.jpg" ]; then
        SIZE=$(stat -c%s "$TEST_DIR/test2.jpg" 2>/dev/null || echo "0")
        echo "   ✅ WellMonitor-style capture successful ($SIZE bytes)"
    else
        echo "   ❌ Capture command succeeded but no file created"
    fi
else
    echo "   ❌ WellMonitor-style capture failed"
    echo "   Error details:"
    timeout 10s libcamera-still --output "$TEST_DIR/test2.jpg" --width 1920 --height 1080 --quality 85 --timeout 2000 --encoding jpg --immediate --nopreview
fi

# Test 3: Verbose capture for debugging
echo
echo "   Test 3: Verbose capture with debug info..."
echo "   Command: libcamera-still --output test3.jpg --timeout 5000 --width 640 --height 480 --nopreview --verbose"
timeout 15s libcamera-still --output "$TEST_DIR/test3.jpg" --timeout 5000 --width 640 --height 480 --nopreview --verbose 2>&1 | head -30

echo
echo "5. Image Analysis:"
for i in 1 2 3; do
    if [ -f "$TEST_DIR/test$i.jpg" ]; then
        SIZE=$(stat -c%s "$TEST_DIR/test$i.jpg" 2>/dev/null || echo "0")
        echo "   test$i.jpg: $SIZE bytes"
        
        # Check if file is actually a valid JPEG
        if command -v file &> /dev/null; then
            FILE_TYPE=$(file "$TEST_DIR/test$i.jpg" 2>/dev/null)
            echo "   File type: $FILE_TYPE"
        fi
        
        # Very basic image analysis - check for grey square (all pixels similar)
        if command -v identify &> /dev/null; then
            echo "   Image info: $(identify "$TEST_DIR/test$i.jpg" 2>/dev/null)"
        fi
    fi
done

echo
echo "6. WellMonitor Debug Images:"
DEBUG_DIR="/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"
if [ -d "$DEBUG_DIR" ]; then
    echo "   Debug images directory: $DEBUG_DIR"
    RECENT_FILES=$(find "$DEBUG_DIR" -name "*.jpg" -mtime -1 2>/dev/null | sort -r | head -3)
    if [ -n "$RECENT_FILES" ]; then
        echo "   Recent debug images:"
        for file in $RECENT_FILES; do
            SIZE=$(stat -c%s "$file" 2>/dev/null || echo "0")
            echo "     $(basename "$file"): $SIZE bytes"
        done
    else
        echo "   No recent debug images found"
    fi
else
    echo "   Debug images directory not found: $DEBUG_DIR"
fi

echo
echo "7. Recommendations:"
echo

# Analyze results and provide recommendations
if [ ! -f "$TEST_DIR/test1.jpg" ] && [ ! -f "$TEST_DIR/test2.jpg" ]; then
    echo "   ❌ CRITICAL: Camera not working at all"
    echo "   Solutions:"
    echo "   1. Check camera cable connection"
    echo "   2. Enable camera: sudo raspi-config → Interface Options → Camera"
    echo "   3. Reboot: sudo reboot"
    echo "   4. Check camera with: libcamera-hello --list-cameras"
    echo
elif [ -f "$TEST_DIR/test1.jpg" ] && [ ! -f "$TEST_DIR/test2.jpg" ]; then
    echo "   ⚠️  Camera works but WellMonitor settings may be problematic"
    echo "   Solutions:"
    echo "   1. Try lower resolution: 1280x720 instead of 1920x1080"
    echo "   2. Increase timeout: 5000ms instead of 2000ms"
    echo "   3. Remove --immediate flag"
    echo
else
    echo "   ✅ Camera appears to be working"
    echo "   If you're still getting grey squares:"
    echo "   1. Check camera positioning and lighting"
    echo "   2. Clean camera lens"
    echo "   3. Ensure target is visible and well-lit"
    echo "   4. Try different camera settings (brightness, contrast)"
    echo "   5. Check for camera focus issues"
    echo

fi

echo "8. Next Steps:"
echo "   • View test images: ls -la $TEST_DIR/"
echo "   • Copy images to view: scp pi@your-pi:$TEST_DIR/*.jpg ."
echo "   • Run WellMonitor camera fix: ./scripts/fix-camera-settings.sh"
echo "   • Monitor WellMonitor logs: sudo journalctl -u wellmonitor -f"

# Cleanup
echo
echo "Test images saved in: $TEST_DIR/"
echo "Use 'rm -rf $TEST_DIR' to clean up test files"
