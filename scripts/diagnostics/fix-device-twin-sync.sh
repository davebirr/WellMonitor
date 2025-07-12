#!/bin/bash
# Device Twin Sync Fix Script for Raspberry Pi
# Attempts to resolve common device twin synchronization issues

set -e

echo "🔧 Device Twin Sync Fix Script"
echo "=============================="
echo "This script will attempt to resolve device twin sync issues"
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

# Check if running as root
if [[ $EUID -ne 0 ]]; then
    print_status $RED "❌ This script must be run as root (use sudo)"
    exit 1
fi

print_section "1. Stopping WellMonitor Service"
print_status $BLUE "🛑 Stopping service..."
systemctl stop wellmonitor
print_status $GREEN "✅ Service stopped"

print_section "2. Checking Network Connectivity"
if ping -c 3 azure.microsoft.com &> /dev/null; then
    print_status $GREEN "✅ Azure connectivity: OK"
else
    print_status $RED "❌ Azure connectivity: FAILED"
    print_status $YELLOW "💡 Cannot proceed without Azure connectivity"
    print_status $YELLOW "   Check internet connection and firewall settings"
    exit 1
fi

print_section "3. Verifying Configuration"
APP_DIR="/opt/wellmonitor"
SECRETS_FILE="$APP_DIR/secrets.json"

# Check for environment variable (preferred) or secrets file (legacy)
if [[ -n "$WELLMONITOR_IOTHUB_CONNECTION_STRING" ]]; then
    print_status $GREEN "✅ Azure IoT connection string found: WELLMONITOR_IOTHUB_CONNECTION_STRING"
    
    if [[ "$WELLMONITOR_IOTHUB_CONNECTION_STRING" == *"HostName="*"azure-devices.net"* ]]; then
        print_status $GREEN "✅ Environment variable contains valid Azure IoT Hub hostname"
    else
        print_status $RED "❌ Environment variable doesn't look like Azure IoT connection string"
        print_status $YELLOW "💡 Verify WELLMONITOR_IOTHUB_CONNECTION_STRING format"
        exit 1
    fi
    
    if [[ "$WELLMONITOR_SECRETS_MODE" == "environment" ]]; then
        print_status $GREEN "✅ Secrets mode set to environment"
    else
        print_status $YELLOW "⚠️  WELLMONITOR_SECRETS_MODE should be 'environment'"
    fi
elif [[ -n "$AZURE_IOT_DEVICE_CONNECTION_STRING" ]]; then
    print_status $GREEN "✅ Azure IoT connection string found: AZURE_IOT_DEVICE_CONNECTION_STRING"
    
    if [[ "$AZURE_IOT_DEVICE_CONNECTION_STRING" == *"HostName="*"azure-devices.net"* ]]; then
        print_status $GREEN "✅ Environment variable contains valid Azure IoT Hub hostname"
    else
        print_status $RED "❌ Environment variable doesn't look like Azure IoT connection string"
        print_status $YELLOW "💡 Verify AZURE_IOT_DEVICE_CONNECTION_STRING format"
        exit 1
    fi
elif [[ -f "$SECRETS_FILE" ]]; then
    print_status $YELLOW "⚠️  Using legacy secrets.json file"
    
    if ! grep -q "DeviceConnectionString" "$SECRETS_FILE" 2>/dev/null; then
        print_status $RED "❌ Device connection string missing from secrets.json"
        print_status $YELLOW "💡 Add Azure IoT Hub connection string to secrets.json"
        exit 1
    fi
    print_status $GREEN "✅ Legacy configuration file present"
else
    print_status $RED "❌ No Azure IoT configuration found"
    print_status $YELLOW "💡 Set environment variable: WELLMONITOR_IOTHUB_CONNECTION_STRING"
    print_status $YELLOW "   And: WELLMONITOR_SECRETS_MODE=environment"
    print_status $YELLOW "   Or create secrets.json with connection string"
    exit 1
fi

print_section "4. Setting Up Debug Images Directory"
DATA_DIR="/var/lib/wellmonitor"
DEBUG_DIR="/var/lib/wellmonitor/debug_images"
OLD_DEBUG_DIR="/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"

# Ensure secure debug directory exists with correct permissions
if [[ ! -d "$DEBUG_DIR" ]]; then
    print_status $BLUE "📁 Creating debug images directory..."
    mkdir -p "$DEBUG_DIR"
    chown wellmonitor:wellmonitor "$DEBUG_DIR"
    chmod 755 "$DEBUG_DIR"
    print_status $GREEN "✅ Debug directory created: $DEBUG_DIR"
else
    print_status $GREEN "✅ Debug directory exists: $DEBUG_DIR"
    # Fix permissions if needed
    chown wellmonitor:wellmonitor "$DEBUG_DIR"
    chmod 755 "$DEBUG_DIR"
fi

# Move old debug images if they exist
if [[ -d "$OLD_DEBUG_DIR" ]] && [[ -n "$(ls -A "$OLD_DEBUG_DIR" 2>/dev/null)" ]]; then
    print_status $BLUE "📁 Moving debug images from old location..."
    mv "$OLD_DEBUG_DIR"/*.jpg "$DEBUG_DIR"/ 2>/dev/null || true
    print_status $GREEN "✅ Debug images moved to secure location"
fi

print_section "5. Clearing Configuration Cache"
# Clear any cached configuration to force fresh device twin sync
CACHE_FILES=(
    "/var/lib/wellmonitor/.device-twin-cache"
    "/tmp/wellmonitor-config.json"
    "/opt/wellmonitor/last-known-config.json"
)

for cache_file in "${CACHE_FILES[@]}"; do
    if [[ -f "$cache_file" ]]; then
        print_status $BLUE "🗑️  Removing cache file: $cache_file"
        rm -f "$cache_file"
    fi
done

print_status $GREEN "✅ Configuration cache cleared"

print_section "6. Checking Camera Hardware"
if [[ -e /dev/video0 ]]; then
    print_status $GREEN "✅ Camera device detected: /dev/video0"
    # Ensure camera permissions
    chgrp video /dev/video0 2>/dev/null || true
    chmod 660 /dev/video0 2>/dev/null || true
else
    print_status $YELLOW "⚠️  Camera device not found: /dev/video0"
    print_status $BLUE "🔧 Attempting to fix camera detection..."
    
    # Add camera overlay if not present
    if ! grep -q "start_x=1" /boot/config.txt; then
        echo "start_x=1" >> /boot/config.txt
        print_status $BLUE "   Added start_x=1 to /boot/config.txt"
    fi
    
    if ! grep -q "gpu_mem=" /boot/config.txt; then
        echo "gpu_mem=128" >> /boot/config.txt
        print_status $BLUE "   Added gpu_mem=128 to /boot/config.txt"
    fi
    
    print_status $YELLOW "⚠️  Camera configuration updated - reboot may be required"
fi

print_section "7. Starting Service with Enhanced Logging"
print_status $BLUE "🚀 Starting service with verbose logging..."

# Enable debug logging temporarily
if [[ -f /etc/systemd/system/wellmonitor.service ]]; then
    # Add debug environment variable if not present
    if ! grep -q "Environment.*DEBUG" /etc/systemd/system/wellmonitor.service; then
        sed -i '/\[Service\]/a Environment=WELLMONITOR_DEBUG=true' /etc/systemd/system/wellmonitor.service
        systemctl daemon-reload
        print_status $BLUE "   Enhanced debug logging enabled"
    fi
fi

systemctl start wellmonitor
sleep 2

if systemctl is-active --quiet wellmonitor; then
    print_status $GREEN "✅ Service started successfully"
else
    print_status $RED "❌ Service failed to start"
    print_status $YELLOW "💡 Check logs: journalctl -u wellmonitor --no-pager -n 20"
    exit 1
fi

print_section "8. Monitoring Initial Sync"
print_status $BLUE "👀 Monitoring service startup and device twin sync..."
print_status $BLUE "   Watching logs for 60 seconds..."

# Monitor logs for device twin sync indicators
timeout 60s journalctl -u wellmonitor -f --no-pager | grep -i --line-buffered "device.*twin\|azure\|connection\|configuration\|debug.*image" &
MONITOR_PID=$!

sleep 60
kill $MONITOR_PID 2>/dev/null || true

print_section "9. Verification"
print_status $BLUE "🔍 Verifying device twin sync results..."

# Check for recent debug images
sleep 5  # Give service time to capture first image
RECENT_IMAGES=$(find "$DEBUG_DIR" -name "*.jpg" -type f -mtime -0.01 2>/dev/null | wc -l)  # Last ~15 minutes

if [[ $RECENT_IMAGES -gt 0 ]]; then
    LATEST_IMAGE=$(find "$DEBUG_DIR" -name "*.jpg" -type f -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -1 | cut -d' ' -f2-)
    print_status $GREEN "✅ New debug images created: $RECENT_IMAGES"
    print_status $GREEN "   Latest: $(basename "$LATEST_IMAGE")"
    
    # Check image size to detect overexposure
    SIZE=$(stat -c '%s' "$LATEST_IMAGE")
    if [[ $SIZE -lt 5000 ]]; then
        print_status $RED "   ❌ Image very small ($SIZE bytes) - likely black/failed"
    elif [[ $SIZE -gt 200000 ]]; then
        print_status $YELLOW "   ⚠️  Image very large ($SIZE bytes) - may be overexposed"
    else
        print_status $GREEN "   ✅ Image size normal ($SIZE bytes)"
    fi
else
    print_status $YELLOW "⚠️  No new debug images yet - may need more time"
fi

# Check if service is using correct debug path
DEBUG_PATH_LOG=$(journalctl -u wellmonitor --since "2 minutes ago" --no-pager 2>/dev/null | grep -i "debug.*image.*path\|saving.*debug" | tail -1)
if [[ -n "$DEBUG_PATH_LOG" ]]; then
    if echo "$DEBUG_PATH_LOG" | grep -q "/var/lib/wellmonitor"; then
        print_status $GREEN "✅ Using correct debug path (device twin synced)"
    else
        print_status $YELLOW "⚠️  Still using old debug path (device twin not synced yet)"
    fi
fi

# Check for Azure connection
AZURE_LOG=$(journalctl -u wellmonitor --since "2 minutes ago" --no-pager 2>/dev/null | grep -i "azure\|iot.*hub" | tail -1)
if [[ -n "$AZURE_LOG" ]]; then
    if echo "$AZURE_LOG" | grep -qi "connect.*success\|established"; then
        print_status $GREEN "✅ Azure IoT connection established"
    elif echo "$AZURE_LOG" | grep -qi "connect.*fail\|error"; then
        print_status $RED "❌ Azure IoT connection failed"
    fi
fi

print_section "10. Next Steps"
print_status $GREEN "🎯 Device twin sync fix completed!"
print_status $BLUE "💡 Recommendations:"
echo ""
echo "1. Monitor logs for the next few minutes:"
echo "   sudo journalctl -u wellmonitor -f"
echo ""
echo "2. Check for new debug images every few minutes:"
echo "   ls -la $DEBUG_DIR/"
echo ""
echo "3. If images are still overexposed (all white):"
echo "   • Device twin sync may take 5-10 minutes"
echo "   • Check with Azure administrator about device twin settings"
echo ""
echo "4. If no Azure connection after 10 minutes:"
echo "   • Verify Azure IoT connection string in secrets.json"
echo "   • Check firewall and network restrictions"
echo ""

print_status $GREEN "✅ Fix script completed successfully!"
