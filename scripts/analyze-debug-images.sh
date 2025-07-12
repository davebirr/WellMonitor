#!/bin/bash

# Debug Image Viewer and Analysis Script
# Helps analyze what's actually in the WellMonitor debug images

echo "🔍 Debug Image Analysis"
echo "======================"
echo

DEBUG_DIR="/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"
TEST_DIR="/tmp/wellmonitor-camera-test"

echo "1. Debug Image Status:"
if [ -d "$DEBUG_DIR" ]; then
    echo "   Debug directory: $DEBUG_DIR"
    
    # Get recent images
    RECENT_IMAGES=$(find "$DEBUG_DIR" -name "*.jpg" -mtime -1 | sort -r | head -5)
    
    if [ -n "$RECENT_IMAGES" ]; then
        echo "   Recent debug images (last 24 hours):"
        for img in $RECENT_IMAGES; do
            SIZE=$(stat -c%s "$img" 2>/dev/null || echo "0")
            TIMESTAMP=$(basename "$img" | sed 's/pump_reading_\([0-9]*_[0-9]*\)\.jpg/\1/' | sed 's/_/ /')
            echo "     $(basename "$img"): $SIZE bytes (captured: $TIMESTAMP)"
        done
    else
        echo "   No recent debug images found"
    fi
else
    echo "   ❌ Debug directory not found: $DEBUG_DIR"
fi

echo
echo "2. Image Content Analysis:"

# Analyze the most recent debug image
LATEST_DEBUG=$(find "$DEBUG_DIR" -name "*.jpg" -type f -printf '%T@ %p\n' 2>/dev/null | sort -nr | head -1 | cut -d' ' -f2-)
LATEST_TEST="$TEST_DIR/test2.jpg"

if [ -f "$LATEST_DEBUG" ]; then
    echo "   Latest debug image: $(basename "$LATEST_DEBUG")"
    
    # Basic file analysis
    if command -v identify &> /dev/null; then
        echo "   Image properties:"
        identify "$LATEST_DEBUG" 2>/dev/null | while read line; do
            echo "     $line"
        done
        
        # Get image statistics (brightness, etc.)
        echo "   Image statistics:"
        identify -verbose "$LATEST_DEBUG" 2>/dev/null | grep -E "(mean|standard deviation|min|max):" | head -4 | while read line; do
            echo "     $line"
        done
    fi
    
    echo
    echo "   Comparing debug image vs test capture:"
    DEBUG_SIZE=$(stat -c%s "$LATEST_DEBUG" 2>/dev/null || echo "0")
    if [ -f "$LATEST_TEST" ]; then
        TEST_SIZE=$(stat -c%s "$LATEST_TEST" 2>/dev/null || echo "0")
        echo "     Debug image: $DEBUG_SIZE bytes"
        echo "     Test capture: $TEST_SIZE bytes"
        
        # Size comparison analysis
        if [ "$DEBUG_SIZE" -lt 10000 ]; then
            echo "     ⚠️  Debug image very small - likely minimal content"
        elif [ "$DEBUG_SIZE" -lt 50000 ]; then
            echo "     ⚠️  Debug image small - may have limited content or poor lighting"
        else
            echo "     ✅ Debug image size normal - likely contains meaningful content"
        fi
    fi
else
    echo "   No debug images available for analysis"
fi

echo
echo "3. Camera Positioning Test:"
echo "   Taking a test capture from current camera position..."

# Capture image with current camera position
POSITION_TEST="/tmp/position_test_$(date +%Y%m%d_%H%M%S).jpg"
if timeout 10s libcamera-still --output "$POSITION_TEST" --width 1920 --height 1080 --quality 85 --timeout 3000 --nopreview 2>/dev/null; then
    if [ -f "$POSITION_TEST" ]; then
        SIZE=$(stat -c%s "$POSITION_TEST" 2>/dev/null || echo "0")
        echo "   ✅ Position test successful: $SIZE bytes"
        echo "   Position test image: $POSITION_TEST"
        
        if command -v identify &> /dev/null; then
            echo "   Image brightness analysis:"
            BRIGHTNESS=$(identify -verbose "$POSITION_TEST" 2>/dev/null | grep "mean:" | head -1)
            echo "     $BRIGHTNESS"
        fi
    fi
else
    echo "   ❌ Position test failed"
fi

echo
echo "4. Recommendations:"
echo

# Analyze and provide specific recommendations
if [ -f "$LATEST_DEBUG" ]; then
    DEBUG_SIZE=$(stat -c%s "$LATEST_DEBUG" 2>/dev/null || echo "0")
    
    if [ "$DEBUG_SIZE" -gt 100000 ]; then
        echo "   ✅ Debug images appear to contain real content (not grey squares)"
        echo "   The images are likely showing what the camera actually sees."
        echo
        echo "   To verify what's in the images:"
        echo "   1. Copy latest debug image to view on your computer:"
        echo "      scp pi@$(hostname -I | awk '{print $1}'):$LATEST_DEBUG ."
        echo
        echo "   2. Check camera positioning:"
        echo "      • Ensure camera is pointed at the pump display"
        echo "      • Verify the display is powered on and showing readings"
        echo "      • Check for proper lighting (not too dark/bright)"
        echo "      • Make sure nothing is blocking the camera view"
        echo
        echo "   3. If images show wrong content:"
        echo "      • Physically reposition the camera"
        echo "      • Adjust camera mount/angle"
        echo "      • Check for reflections or glare"
        
    else
        echo "   ⚠️  Debug images are smaller than expected"
        echo "   This could indicate:"
        echo "   1. Very dark environment (camera sees mostly black)"
        echo "   2. Camera pointed at blank/uniform surface"  
        echo "   3. Camera lens dirty or blocked"
        echo "   4. Target display is off or not visible"
        echo
        echo "   Solutions:"
        echo "   • Add lighting to the pump display area"
        echo "   • Clean camera lens"
        echo "   • Verify pump display is powered and showing readings"
        echo "   • Adjust camera position to see the display clearly"
    fi
else
    echo "   ❌ No debug images to analyze"
    echo "   • Check if debug image saving is enabled"
    echo "   • Verify WellMonitor service is running"
    echo "   • Check service logs for errors"
fi

echo
echo "5. Copy Images for Viewing:"
echo "   To view the actual images on your local computer:"
echo
if [ -f "$LATEST_DEBUG" ]; then
    echo "   # Copy latest debug image:"
    echo "   scp pi@$(hostname -I | awk '{print $1}'):$LATEST_DEBUG ./debug_image.jpg"
fi
if [ -f "$POSITION_TEST" ]; then
    echo "   # Copy position test image:"
    echo "   scp pi@$(hostname -I | awk '{print $1}'):$POSITION_TEST ./position_test.jpg"
fi
if [ -f "$LATEST_TEST" ]; then
    echo "   # Copy camera test image:"
    echo "   scp pi@$(hostname -I | awk '{print $1}'):$LATEST_TEST ./camera_test.jpg"
fi

echo
echo "6. Next Steps:"
echo "   • View copied images to see what camera actually captures"
echo "   • Adjust camera position based on image content"
echo "   • Run camera settings fix if needed: ./scripts/fix-camera-settings.sh"
echo "   • Monitor new captures: watch 'ls -la $DEBUG_DIR'"

echo
echo "Images available for download:"
[ -f "$LATEST_DEBUG" ] && echo "  - Debug: $LATEST_DEBUG"
[ -f "$POSITION_TEST" ] && echo "  - Position: $POSITION_TEST"  
[ -f "$LATEST_TEST" ] && echo "  - Test: $LATEST_TEST"
