#!/bin/bash
# Device Twin Configuration Checker for Raspberry Pi
# Checks what configuration values the device is actually using

set -e

echo "🔧 Device Twin Configuration Checker"
echo "===================================="
echo "Checking actual configuration values used by WellMonitor service"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

print_section() {
    echo ""
    print_status $CYAN "📋 $1"
    echo "----------------------------------------"
}

print_section "1. Azure IoT Configuration Method"

# Check which configuration method is being used
if [[ -n "$WELLMONITOR_IOTHUB_CONNECTION_STRING" ]]; then
    print_status $GREEN "✅ Using environment variable: WELLMONITOR_IOTHUB_CONNECTION_STRING"
    if [[ "$WELLMONITOR_IOTHUB_CONNECTION_STRING" == *"HostName="*"azure-devices.net"* ]]; then
        print_status $GREEN "✅ Environment variable contains valid Azure IoT Hub hostname"
    else
        print_status $YELLOW "⚠️  Environment variable format may be incorrect"
    fi
    
    if [[ "$WELLMONITOR_SECRETS_MODE" == "environment" ]]; then
        print_status $GREEN "✅ Secrets mode: environment"
    else
        print_status $YELLOW "⚠️  WELLMONITOR_SECRETS_MODE not set to 'environment'"
    fi
elif [[ -n "$AZURE_IOT_DEVICE_CONNECTION_STRING" ]]; then
    print_status $GREEN "✅ Using environment variable: AZURE_IOT_DEVICE_CONNECTION_STRING"
    if [[ "$AZURE_IOT_DEVICE_CONNECTION_STRING" == *"HostName="*"azure-devices.net"* ]]; then
        print_status $GREEN "✅ Environment variable contains valid Azure IoT Hub hostname"
    else
        print_status $YELLOW "⚠️  Environment variable format may be incorrect"
    fi
elif [[ -f "/opt/wellmonitor/secrets.json" ]]; then
    print_status $YELLOW "⚠️  Using legacy secrets.json file"
    print_status $BLUE "💡 Consider migrating to environment variable for better security"
else
    print_status $RED "❌ No Azure IoT configuration found"
    print_status $YELLOW "💡 This explains why device twin sync isn't working"
fi

print_section "2. Current Service Configuration"

# Look for configuration loading in logs
CONFIG_LOGS=$(journalctl -u wellmonitor --since "1 hour ago" --no-pager 2>/dev/null | grep -i "configuration\|device.*twin\|setting" | tail -10)

if [[ -n "$CONFIG_LOGS" ]]; then
    echo "Recent configuration logs:"
    echo "$CONFIG_LOGS"
    echo ""
else
    print_status $YELLOW "⚠️  No configuration logs found in the last hour"
fi

print_section "3. Camera Settings Detection"

# Extract camera settings from recent logs
print_status $BLUE "🔍 Extracting camera settings from logs..."

GAIN_LOG=$(journalctl -u wellmonitor --since "30 minutes ago" --no-pager 2>/dev/null | grep -i "gain" | tail -1)
SHUTTER_LOG=$(journalctl -u wellmonitor --since "30 minutes ago" --no-pager 2>/dev/null | grep -i "shutter" | tail -1)
DEBUG_PATH_LOG=$(journalctl -u wellmonitor --since "30 minutes ago" --no-pager 2>/dev/null | grep -i "debug.*image.*path\|saving.*debug" | tail -1)

if [[ -n "$GAIN_LOG" ]]; then
    echo "Gain setting: $GAIN_LOG"
else
    print_status $YELLOW "⚠️  No gain settings found in logs"
fi

if [[ -n "$SHUTTER_LOG" ]]; then
    echo "Shutter setting: $SHUTTER_LOG"
else
    print_status $YELLOW "⚠️  No shutter settings found in logs"
fi

if [[ -n "$DEBUG_PATH_LOG" ]]; then
    echo "Debug path: $DEBUG_PATH_LOG"
else
    print_status $YELLOW "⚠️  No debug path settings found in logs"
fi

print_section "4. Azure IoT Connection Status"

# Check for Azure connection logs
AZURE_LOGS=$(journalctl -u wellmonitor --since "30 minutes ago" --no-pager 2>/dev/null | grep -i "azure\|iot.*hub\|connection" | tail -5)

if [[ -n "$AZURE_LOGS" ]]; then
    echo "Azure IoT connection logs:"
    echo "$AZURE_LOGS"
    echo ""
    
    # Check for connection success/failure
    if echo "$AZURE_LOGS" | grep -qi "connect.*success\|established\|authenticated"; then
        print_status $GREEN "✅ Azure IoT connection appears successful"
    elif echo "$AZURE_LOGS" | grep -qi "connect.*fail\|error\|timeout"; then
        print_status $RED "❌ Azure IoT connection issues detected"
    else
        print_status $YELLOW "⚠️  Azure IoT connection status unclear"
    fi
else
    print_status $YELLOW "⚠️  No Azure IoT connection logs found"
fi

print_section "5. Device Twin Sync Status"

# Look for device twin sync messages
TWIN_LOGS=$(journalctl -u wellmonitor --since "1 hour ago" --no-pager 2>/dev/null | grep -i "device.*twin\|twin.*update\|desired.*properties" | tail -5)

if [[ -n "$TWIN_LOGS" ]]; then
    echo "Device twin sync logs:"
    echo "$TWIN_LOGS"
    echo ""
    
    if echo "$TWIN_LOGS" | grep -qi "twin.*update.*received\|properties.*updated\|configuration.*updated"; then
        print_status $GREEN "✅ Device twin updates appear to be working"
    else
        print_status $YELLOW "⚠️  Device twin sync status unclear"
    fi
else
    print_status $YELLOW "⚠️  No device twin sync logs found"
    print_status $BLUE "💡 This might indicate the device isn't connecting to Azure IoT Hub"
fi

print_section "6. Debug Images Analysis"

DEBUG_DIR="/var/lib/wellmonitor/debug_images"
OLD_DEBUG_DIR="/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"

# Check both possible debug directories
for dir in "$DEBUG_DIR" "$OLD_DEBUG_DIR"; do
    if [[ -d "$dir" ]]; then
        IMAGE_COUNT=$(find "$dir" -name "*.jpg" -type f -mtime -1 2>/dev/null | wc -l)
        print_status $BLUE "📁 $dir: $IMAGE_COUNT recent images (last 24h)"
        
        if [[ $IMAGE_COUNT -gt 0 ]]; then
            # Get the latest image
            LATEST_IMAGE=$(find "$dir" -name "*.jpg" -type f -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -1 | cut -d' ' -f2-)
            if [[ -n "$LATEST_IMAGE" ]]; then
                echo "   • Latest: $(basename "$LATEST_IMAGE")"
                echo "   • Size: $(stat -c '%s' "$LATEST_IMAGE") bytes"
                echo "   • Modified: $(stat -c '%y' "$LATEST_IMAGE")"
                
                # Check if image might be overexposed (very small or very large file size)
                SIZE=$(stat -c '%s' "$LATEST_IMAGE")
                if [[ $SIZE -lt 5000 ]]; then
                    print_status $RED "   ❌ Image very small ($SIZE bytes) - likely black/failed"
                elif [[ $SIZE -gt 200000 ]]; then
                    print_status $YELLOW "   ⚠️  Image very large ($SIZE bytes) - check quality"
                else
                    print_status $GREEN "   ✅ Image size normal ($SIZE bytes)"
                fi
            fi
        fi
    fi
done

# Check which directory the service is actually using
ACTIVE_DEBUG_LOG=$(journalctl -u wellmonitor --since "30 minutes ago" --no-pager 2>/dev/null | grep -i "debug.*image.*saved\|saving.*debug" | tail -1)
if [[ -n "$ACTIVE_DEBUG_LOG" ]]; then
    echo ""
    print_status $BLUE "🎯 Service is currently using:"
    echo "$ACTIVE_DEBUG_LOG"
    
    if echo "$ACTIVE_DEBUG_LOG" | grep -q "/var/lib/wellmonitor"; then
        print_status $GREEN "✅ Using secure path (/var/lib/wellmonitor)"
    elif echo "$ACTIVE_DEBUG_LOG" | grep -q "/home/davidb"; then
        print_status $YELLOW "⚠️  Using old path (/home/davidb) - device twin not synced"
    fi
fi

print_section "7. OCR Processing Status"

OCR_LOGS=$(journalctl -u wellmonitor --since "15 minutes ago" --no-pager 2>/dev/null | grep -i "ocr\|confidence\|tesseract" | tail -5)

if [[ -n "$OCR_LOGS" ]]; then
    echo "Recent OCR processing logs:"
    echo "$OCR_LOGS"
    echo ""
    
    if echo "$OCR_LOGS" | grep -qi "confidence.*0\|failed.*attempts"; then
        print_status $RED "❌ OCR processing failing - likely overexposed/poor images"
    elif echo "$OCR_LOGS" | grep -qi "confidence.*[0-9]"; then
        print_status $GREEN "✅ OCR processing working"
    fi
else
    print_status $YELLOW "⚠️  No recent OCR logs found"
fi

print_section "8. Force Configuration Refresh"

print_status $BLUE "🔄 Available actions to force configuration sync:"
echo ""
echo "1. Restart service (applies any cached device twin updates):"
echo "   sudo systemctl restart wellmonitor"
echo ""
echo "2. Check service logs in real-time:"
echo "   sudo journalctl -u wellmonitor -f"
echo ""
echo "3. If still using old settings after restart:"
echo "   • Check internet connectivity: ping azure.microsoft.com"
echo "   • Verify Azure IoT connection string in secrets.json"
echo "   • Contact Azure IoT Hub administrator to verify device twin"
echo ""

print_section "9. Quick Status Summary"

# Determine overall status
ISSUES=0

if ! systemctl is-active --quiet wellmonitor; then
    print_status $RED "❌ Service not running"
    ((ISSUES++))
fi

if [[ ! -d "$DEBUG_DIR" ]] && [[ ! -d "$OLD_DEBUG_DIR" ]]; then
    print_status $RED "❌ No debug images directory found"
    ((ISSUES++))
fi

# Check for recent images
RECENT_IMAGES=0
for dir in "$DEBUG_DIR" "$OLD_DEBUG_DIR"; do
    if [[ -d "$dir" ]]; then
        COUNT=$(find "$dir" -name "*.jpg" -type f -mtime -0.1 2>/dev/null | wc -l)  # Last 2.4 hours
        RECENT_IMAGES=$((RECENT_IMAGES + COUNT))
    fi
done

if [[ $RECENT_IMAGES -eq 0 ]]; then
    print_status $YELLOW "⚠️  No recent debug images (last 2.4 hours)"
    ((ISSUES++))
fi

if echo "$ACTIVE_DEBUG_LOG" | grep -q "/home/davidb"; then
    print_status $YELLOW "⚠️  Using old debug path - device twin not synced"
    ((ISSUES++))
fi

if [[ $ISSUES -eq 0 ]]; then
    print_status $GREEN "✅ Overall status: GOOD"
    print_status $GREEN "💡 Device twin sync appears to be working correctly"
else
    print_status $YELLOW "⚠️  Overall status: $ISSUES issues detected"
    print_status $BLUE "💡 Run 'sudo systemctl restart wellmonitor' and check again in 5 minutes"
fi

echo ""
print_status $GREEN "🔍 Analysis complete!"
