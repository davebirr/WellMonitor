#!/bin/bash

# Camera Exposure Fix Script
# Fixes the invalid "--exposure off" issue that causes camera failures

set -e

echo "🔧 Deploying Camera Exposure Fix"
echo "=================================="
echo ""

# Check if we're on the Pi
if ! command -v libcamera-still &> /dev/null; then
    echo "❌ libcamera-still not found. This script should be run on the Raspberry Pi."
    exit 1
fi

# Function to test camera with different exposure modes
test_exposure_mode() {
    local mode=$1
    local output_file="/tmp/test_exposure_${mode}.jpg"
    
    echo "🧪 Testing exposure mode: $mode"
    
    if timeout 10s libcamera-still --output "$output_file" --width 640 --height 480 --timeout 2000 --exposure "$mode" --nopreview --encoding jpg >/dev/null 2>&1; then
        if [ -f "$output_file" ] && [ -s "$output_file" ]; then
            local size=$(stat -c%s "$output_file" 2>/dev/null || echo "0")
            echo "✅ Exposure mode '$mode' works (${size} bytes)"
            rm -f "$output_file"
            return 0
        else
            echo "⚠️  Exposure mode '$mode' creates empty file"
            return 1
        fi
    else
        echo "❌ Exposure mode '$mode' failed"
        return 1
    fi
}

# Test various exposure modes to find the best one for LED displays
echo "🔍 Testing available exposure modes..."
echo ""

# Test the problematic 'off' mode first
echo "Testing the problematic 'off' mode that was causing failures:"
if test_exposure_mode "off"; then
    echo "⚠️  Surprisingly, 'off' mode works on this system"
else
    echo "✅ Confirmed: 'off' mode fails (this was our problem)"
fi

echo ""
echo "Testing alternative exposure modes for LED displays:"

# Test modes that should work for LED displays
EXPOSURE_MODES=("normal" "barcode" "sport" "night" "candlelight")
BEST_MODE=""

for mode in "${EXPOSURE_MODES[@]}"; do
    if test_exposure_mode "$mode"; then
        if [ -z "$BEST_MODE" ]; then
            BEST_MODE="$mode"
        fi
    fi
done

echo ""
if [ -n "$BEST_MODE" ]; then
    echo "🎯 Best exposure mode for this system: $BEST_MODE"
    echo ""
    echo "📋 Recommendation for LED displays:"
    if [ "$BEST_MODE" == "barcode" ]; then
        echo "   • 'barcode' mode is ideal for high-contrast LED displays"
        echo "   • This mode is optimized for reading text/numbers"
    elif [ "$BEST_MODE" == "normal" ]; then
        echo "   • 'normal' mode is a safe fallback for most situations"
        echo "   • Works well with manual shutter speed settings"
    else
        echo "   • '$BEST_MODE' mode works but may not be optimal for LEDs"
        echo "   • Consider using 'barcode' if available for LED displays"
    fi
else
    echo "❌ No working exposure modes found - camera may have hardware issues"
    exit 1
fi

echo ""
echo "🚀 Deploying the fix..."

# Stop the service
echo "⏹️  Stopping WellMonitor service..."
sudo systemctl stop wellmonitor

# The fix is already in the code, we just need to rebuild and restart
echo "🔄 Rebuilding application..."
cd /home/davidb/WellMonitor/src/WellMonitor.Device
dotnet publish -c Release -o /opt/wellmonitor --self-contained false

# Restart the service
echo "▶️  Starting WellMonitor service..."
sudo systemctl start wellmonitor

# Wait a moment for startup
echo "⏳ Waiting for service to start..."
sleep 5

# Check service status
echo "🔍 Checking service status..."
sudo systemctl status wellmonitor --no-pager -l

echo ""
echo "📋 Next steps:"
echo "1. Monitor the service logs to verify the fix:"
echo "   sudo journalctl -u wellmonitor -f"
echo ""
echo "2. Look for these log messages:"
echo "   ✅ 'Manual shutter speed set, using barcode exposure mode for LED displays'"
echo "   ✅ 'Auto exposure disabled, using normal exposure mode'"
echo "   ❌ Should NOT see: 'ERROR: *** Invalid exposure mode:off ***'"
echo ""
echo "3. If camera still fails, check device twin configuration:"
echo "   • Ensure cameraShutterSpeedMicroseconds > 0 for manual mode"
echo "   • Set cameraAutoExposure = false for LED displays"
echo ""
echo "🎯 The fix replaces invalid 'off' exposure mode with proper modes:"
echo "   • 'barcode' mode for manual shutter (optimal for LED displays)"
echo "   • 'normal' mode for other situations"
echo ""
echo "✅ Camera exposure fix deployment complete!"
