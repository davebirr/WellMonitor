#!/bin/bash

# Camera Settings Fix for WellMonitor
# Addresses common issues that cause grey squares or poor image quality

echo "🔧 WellMonitor Camera Settings Fix"
echo "=================================="
echo

# Check if Azure CLI is available for device twin updates
if command -v az &> /dev/null; then
    echo "✅ Azure CLI available - can update device twin settings"
    CAN_UPDATE_TWIN=true
else
    echo "⚠️  Azure CLI not available - will show manual update instructions"
    CAN_UPDATE_TWIN=false
fi

echo
echo "Common Grey Square Causes & Solutions:"
echo "1. Camera not properly initialized (timeout too short)"
echo "2. Insufficient lighting"
echo "3. Camera lens covered/dirty"
echo "4. Wrong camera settings (resolution, quality)"
echo "5. Hardware connection issues"
echo

# Test current camera quickly
echo "Testing current camera settings..."
TEST_IMAGE="/tmp/wellmonitor_camera_fix_test.jpg"

# Try current WellMonitor-style capture
if timeout 15s libcamera-still --output "$TEST_IMAGE" --width 1920 --height 1080 --quality 85 --timeout 2000 --encoding jpg --immediate --nopreview 2>/dev/null; then
    if [ -f "$TEST_IMAGE" ]; then
        SIZE=$(stat -c%s "$TEST_IMAGE" 2>/dev/null || echo "0")
        echo "✅ Current settings work ($SIZE bytes)"
        if [ "$SIZE" -lt 10000 ]; then
            echo "⚠️  Image very small - likely a grey square or minimal content"
            NEEDS_FIX=true
        else
            echo "✅ Image size looks good"
            NEEDS_FIX=false
        fi
    else
        echo "❌ Camera capture failed - no image file created"
        NEEDS_FIX=true
    fi
else
    echo "❌ Camera capture failed"
    NEEDS_FIX=true
fi

if [ "$NEEDS_FIX" = true ]; then
    echo
    echo "🔧 Applying camera fixes..."
    
    # Optimized camera settings for reliable capture
    RECOMMENDED_SETTINGS='
{
  "cameraWidth": 1280,
  "cameraHeight": 720,
  "cameraQuality": 75,
  "cameraTimeoutMs": 5000,
  "cameraWarmupTimeMs": 3000,
  "cameraRotation": 0,
  "cameraBrightness": 55,
  "cameraContrast": 15,
  "cameraSaturation": 0,
  "cameraEnablePreview": false
}'

    if [ "$CAN_UPDATE_TWIN" = true ]; then
        echo "Updating device twin with optimized camera settings..."
        
        # Prompt for IoT Hub and Device details
        read -p "Enter IoT Hub name (e.g., WellMonitorIoTHub-dev): " IOT_HUB
        read -p "Enter Device ID (e.g., rpi4b-1407well01): " DEVICE_ID
        
        if [ -n "$IOT_HUB" ] && [ -n "$DEVICE_ID" ]; then
            echo "Updating device twin..."
            
            # Update camera settings in device twin
            az iot hub device-twin update \
                --hub-name "$IOT_HUB" \
                --device-id "$DEVICE_ID" \
                --set properties.desired.cameraWidth=1280 \
                --set properties.desired.cameraHeight=720 \
                --set properties.desired.cameraQuality=75 \
                --set properties.desired.cameraTimeoutMs=5000 \
                --set properties.desired.cameraWarmupTimeMs=3000 \
                --set properties.desired.cameraBrightness=55 \
                --set properties.desired.cameraContrast=15
            
            if [ $? -eq 0 ]; then
                echo "✅ Device twin updated successfully"
                echo "The WellMonitor service will pick up these changes automatically"
            else
                echo "❌ Failed to update device twin"
                echo "Try manual update via Azure Portal"
            fi
        else
            echo "❌ IoT Hub name or Device ID not provided"
        fi
    else
        echo "Manual device twin update required:"
        echo "1. Go to Azure Portal → IoT Hub → Devices → Your Device"
        echo "2. Click 'Device Twin'"
        echo "3. Update the following properties in 'desired' section:"
        echo "$RECOMMENDED_SETTINGS"
        echo "4. Click 'Save'"
    fi
    
    echo
    echo "Additional troubleshooting steps:"
    echo "1. Physical checks:"
    echo "   • Ensure camera cable is properly connected"
    echo "   • Check camera lens is clean and unobstructed"
    echo "   • Verify adequate lighting on the target display"
    echo
    echo "2. Test with manual capture:"
    echo "   libcamera-still --output test.jpg --timeout 5000 --width 1280 --height 720"
    echo
    echo "3. View captured image:"
    echo "   # Copy to local machine to view"
    echo "   scp pi@your-pi:/tmp/test.jpg ."
    echo
    echo "4. If still grey squares:"
    echo "   • Try different --timeout values (3000, 5000, 10000)"
    echo "   • Remove --immediate flag"
    echo "   • Add --verbose flag to see debug info"
    echo "   • Check camera with: libcamera-hello (should show preview)"
    
else
    echo
    echo "✅ Camera appears to be working correctly!"
    echo "If you're still experiencing issues:"
    echo "1. Check the actual debug images content (may be legitimate grey content)"
    echo "2. Ensure the camera is pointed at a visible target"
    echo "3. Check lighting conditions"
    echo "4. Verify the target display is powered on and showing content"
fi

echo
echo "Testing optimized settings..."
if timeout 15s libcamera-still --output "/tmp/optimized_test.jpg" --width 1280 --height 720 --quality 75 --timeout 5000 --nopreview 2>/dev/null; then
    if [ -f "/tmp/optimized_test.jpg" ]; then
        SIZE=$(stat -c%s "/tmp/optimized_test.jpg" 2>/dev/null || echo "0")
        echo "✅ Optimized settings test successful ($SIZE bytes)"
        echo "Test image saved: /tmp/optimized_test.jpg"
    fi
else
    echo "❌ Optimized settings test failed"
    echo "May need hardware troubleshooting"
fi

echo
echo "Next steps:"
echo "• Restart WellMonitor service: sudo systemctl restart wellmonitor"
echo "• Monitor debug images: ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/"
echo "• Check service logs: sudo journalctl -u wellmonitor -f"
echo "• Run camera diagnostics: ./scripts/diagnose-camera.sh"

# Cleanup
rm -f "$TEST_IMAGE" 2>/dev/null
