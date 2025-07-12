#!/bin/bash

# Low Light LED Camera Optimization Script
# Optimizes camera settings for capturing red 7-segment LEDs in dark environments

echo "🔧 Low Light LED Camera Optimization"
echo "==================================="
echo

echo "Scenario: Capturing red 7-segment LEDs in dark basement"
echo "Challenge: No ambient lighting, camera few inches from display"
echo "Goal: Clear, readable LED digit capture"
echo

# Test current position first
echo "1. Testing current camera position..."
CURRENT_TEST="/tmp/current_position_$(date +%Y%m%d_%H%M%S).jpg"

# Current WellMonitor settings
timeout 15s libcamera-still --output "$CURRENT_TEST" \
    --width 1920 --height 1080 --quality 85 --timeout 2000 \
    --encoding jpg --immediate --nopreview 2>/dev/null

if [ -f "$CURRENT_TEST" ]; then
    SIZE=$(stat -c%s "$CURRENT_TEST" 2>/dev/null || echo "0")
    echo "   Current settings result: $SIZE bytes"
else
    echo "   Current settings failed"
fi

echo
echo "2. Testing optimized settings for LED capture..."

# Optimized settings for LED displays in low light
LED_SETTINGS=(
    # Test 1: Lower resolution, higher ISO sensitivity, longer exposure
    "--width 1280 --height 720 --timeout 5000 --gain 8.0 --brightness 0.3 --contrast 1.8"
    
    # Test 2: Focus on the LED area with higher gain
    "--width 1280 --height 720 --timeout 5000 --gain 12.0 --brightness 0.4 --contrast 2.0 --saturation 0.8"
    
    # Test 3: Macro-style capture for close objects
    "--width 1280 --height 720 --timeout 8000 --gain 16.0 --brightness 0.5 --contrast 2.2 --shutter 100000"
    
    # Test 4: Very high gain for extreme low light
    "--width 1280 --height 720 --timeout 8000 --gain 20.0 --brightness 0.6 --contrast 2.5"
)

for i in "${!LED_SETTINGS[@]}"; do
    TEST_NUM=$((i + 1))
    TEST_FILE="/tmp/led_test_${TEST_NUM}_$(date +%Y%m%d_%H%M%S).jpg"
    
    echo "   Test $TEST_NUM: LED-optimized settings..."
    echo "   Settings: ${LED_SETTINGS[$i]}"
    
    # Use eval to properly expand the settings
    if timeout 20s libcamera-still --output "$TEST_FILE" --nopreview ${LED_SETTINGS[$i]} 2>/dev/null; then
        if [ -f "$TEST_FILE" ]; then
            SIZE=$(stat -c%s "$TEST_FILE" 2>/dev/null || echo "0")
            echo "   ✅ Test $TEST_NUM successful: $SIZE bytes"
            
            # Basic brightness analysis if identify is available
            if command -v identify &> /dev/null; then
                BRIGHTNESS=$(identify -verbose "$TEST_FILE" 2>/dev/null | grep "mean:" | head -1 | awk '{print $2}')
                echo "   Image brightness: $BRIGHTNESS"
            fi
        else
            echo "   ❌ Test $TEST_NUM failed: No image created"
        fi
    else
        echo "   ❌ Test $TEST_NUM failed: Command timeout"
    fi
    echo
done

echo "3. Device Twin Configuration Recommendations:"
echo

# Determine best settings based on tests
BEST_SETTINGS=""
if [ -f "/tmp/led_test_2_"* ]; then
    BEST_SETTINGS="Test 2 (moderate gain)"
    RECOMMENDED_TWIN='{
  "cameraWidth": 1280,
  "cameraHeight": 720,
  "cameraTimeoutMs": 5000,
  "cameraWarmupTimeMs": 3000,
  "cameraBrightness": 70,
  "cameraContrast": 50,
  "cameraSaturation": 30,
  "cameraGain": 12.0,
  "cameraShutter": 50000
}'
elif [ -f "/tmp/led_test_1_"* ]; then
    BEST_SETTINGS="Test 1 (basic optimization)"
    RECOMMENDED_TWIN='{
  "cameraWidth": 1280,
  "cameraHeight": 720,
  "cameraTimeoutMs": 5000,
  "cameraWarmupTimeMs": 3000,
  "cameraBrightness": 65,
  "cameraContrast": 45,
  "cameraSaturation": 25
}'
else
    BEST_SETTINGS="Fallback settings"
    RECOMMENDED_TWIN='{
  "cameraWidth": 1280,
  "cameraHeight": 720,
  "cameraTimeoutMs": 8000,
  "cameraWarmupTimeMs": 5000,
  "cameraBrightness": 75,
  "cameraContrast": 55
}'
fi

echo "Recommended device twin settings ($BEST_SETTINGS):"
echo "$RECOMMENDED_TWIN"

echo
echo "4. Physical Setup Recommendations:"
echo
echo "   For optimal 7-segment LED capture:"
echo "   📏 Distance: 2-4 inches from display"
echo "   📐 Angle: Perpendicular to display surface"
echo "   💡 Lighting: Consider adding small LED strip or flashlight"
echo "   🔧 Focus: Ensure camera can focus on close objects"
echo "   📱 Stability: Secure mount to prevent blur"
echo
echo "   LED-specific tips:"
echo "   🔴 Red LEDs: May need saturation adjustment"
echo "   ✨ Brightness: LEDs should be brightest part of image"
echo "   🖼️ Framing: Keep LED display centered in frame"
echo "   ⚡ Power: Ensure display is always on during capture"

echo
echo "5. Alternative Solutions:"
echo
echo "   If camera still struggles:"
echo "   💡 Add supplemental lighting:"
echo "      • Small USB LED strip"
echo "      • Adjustable desk lamp"
echo "      • Phone flashlight (for testing)"
echo
echo "   🔧 Hardware alternatives:"
echo "      • Pi Camera with adjustable focus"
echo "      • USB webcam with manual focus"
echo "      • Add IR illuminator for night vision"

echo
echo "6. Apply Best Settings:"
echo

if command -v az &> /dev/null; then
    echo "   Azure CLI available - can update device twin automatically"
    echo
    read -p "   Update device twin with optimized settings? (y/N): " UPDATE_TWIN
    
    if [[ "$UPDATE_TWIN" =~ ^[Yy]$ ]]; then
        read -p "   Enter IoT Hub name: " IOT_HUB
        read -p "   Enter Device ID: " DEVICE_ID
        
        if [ -n "$IOT_HUB" ] && [ -n "$DEVICE_ID" ]; then
            echo "   Updating device twin..."
            
            az iot hub device-twin update \
                --hub-name "$IOT_HUB" \
                --device-id "$DEVICE_ID" \
                --set properties.desired.cameraWidth=1280 \
                --set properties.desired.cameraHeight=720 \
                --set properties.desired.cameraTimeoutMs=5000 \
                --set properties.desired.cameraWarmupTimeMs=3000 \
                --set properties.desired.cameraBrightness=70 \
                --set properties.desired.cameraContrast=50 \
                --set properties.desired.cameraSaturation=30 \
                --set properties.desired.debugImageSaveEnabled=true
            
            echo "   ✅ Device twin updated for LED capture"
        fi
    fi
else
    echo "   Install Azure CLI to automatically update device twin"
    echo "   Or manually update via Azure Portal with the settings above"
fi

echo
echo "7. Test Images Available:"
echo "   Copy images to your computer to compare quality:"
echo
[ -f "$CURRENT_TEST" ] && echo "   scp pi@$(hostname -I | awk '{print $1}'):$CURRENT_TEST ./current.jpg"

for i in {1..4}; do
    TEST_FILE=$(ls /tmp/led_test_${i}_*.jpg 2>/dev/null | head -1)
    if [ -f "$TEST_FILE" ]; then
        echo "   scp pi@$(hostname -I | awk '{print $1}'):$TEST_FILE ./led_test_${i}.jpg"
    fi
done

echo
echo "8. Next Steps:"
echo "   • Copy and compare test images"
echo "   • Restart WellMonitor: sudo systemctl restart wellmonitor"
echo "   • Monitor new debug images: watch 'ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/'"
echo "   • Consider adding supplemental lighting if needed"

echo
echo "🎯 Expected Results:"
echo "   With optimized settings, you should see:"
echo "   • Clear red LED digits visible in images"
echo "   • Better contrast between LEDs and background"
echo "   • Reduced grey/fuzzy appearance"
echo "   • Readable 7-segment numbers for OCR processing"
