#!/bin/bash
# Device Twin Sync Troubleshooting Script for Raspberry Pi
# Run this script on the device to diagnose Azure IoT Hub connectivity and device twin sync issues

set -e

echo "🔍 Device Twin Sync Troubleshooting"
echo "==================================="
echo "Running on: $(hostname)"
echo "Date: $(date)"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to print colored output
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

# Check if running as root (some commands need it)
if [[ $EUID -eq 0 ]]; then
    print_status $YELLOW "⚠️  Running as root - some file permissions may look different"
fi

print_section "1. Basic System Information"
print_status $BLUE "• Hostname: $(hostname)"
print_status $BLUE "• OS: $(cat /etc/os-release | grep PRETTY_NAME | cut -d'"' -f2)"
print_status $BLUE "• Kernel: $(uname -r)"
print_status $BLUE "• Architecture: $(uname -m)"
print_status $BLUE "• Uptime: $(uptime -p)"

print_section "2. Network Connectivity"
if ping -c 1 8.8.8.8 &> /dev/null; then
    print_status $GREEN "✅ Internet connectivity: OK"
else
    print_status $RED "❌ Internet connectivity: FAILED"
    print_status $YELLOW "💡 Check network configuration and router"
fi

if ping -c 1 azure.microsoft.com &> /dev/null; then
    print_status $GREEN "✅ Azure connectivity: OK"
else
    print_status $RED "❌ Azure connectivity: FAILED"
    print_status $YELLOW "💡 Check firewall and DNS settings"
fi

print_section "3. WellMonitor Service Status"
if systemctl is-active --quiet wellmonitor; then
    print_status $GREEN "✅ Service Status: RUNNING"
    echo "   • Started: $(systemctl show wellmonitor --property=ActiveEnterTimestamp --value)"
    echo "   • PID: $(systemctl show wellmonitor --property=MainPID --value)"
else
    print_status $RED "❌ Service Status: NOT RUNNING"
    print_status $YELLOW "💡 Start with: sudo systemctl start wellmonitor"
fi

if systemctl is-enabled --quiet wellmonitor; then
    print_status $GREEN "✅ Service Enabled: YES"
else
    print_status $YELLOW "⚠️  Service Enabled: NO"
    print_status $YELLOW "💡 Enable with: sudo systemctl enable wellmonitor"
fi

print_section "4. Application Files and Permissions"
APP_DIR="/opt/wellmonitor"
DATA_DIR="/var/lib/wellmonitor"
DEBUG_DIR="/var/lib/wellmonitor/debug_images"

# Check application directory
if [[ -d "$APP_DIR" ]]; then
    print_status $GREEN "✅ Application directory exists: $APP_DIR"
    echo "   • Owner: $(stat -c '%U:%G' $APP_DIR)"
    echo "   • Permissions: $(stat -c '%a' $APP_DIR)"
    
    if [[ -f "$APP_DIR/WellMonitor.Device" ]]; then
        print_status $GREEN "✅ Main executable exists"
        echo "   • Size: $(stat -c '%s' $APP_DIR/WellMonitor.Device) bytes"
        echo "   • Modified: $(stat -c '%y' $APP_DIR/WellMonitor.Device)"
    else
        print_status $RED "❌ Main executable missing: $APP_DIR/WellMonitor.Device"
    fi
else
    print_status $RED "❌ Application directory missing: $APP_DIR"
fi

# Check data directory
if [[ -d "$DATA_DIR" ]]; then
    print_status $GREEN "✅ Data directory exists: $DATA_DIR"
    echo "   • Owner: $(stat -c '%U:%G' $DATA_DIR)"
    echo "   • Permissions: $(stat -c '%a' $DATA_DIR)"
else
    print_status $RED "❌ Data directory missing: $DATA_DIR"
fi

# Check debug images directory
if [[ -d "$DEBUG_DIR" ]]; then
    print_status $GREEN "✅ Debug images directory exists: $DEBUG_DIR"
    echo "   • Owner: $(stat -c '%U:%G' $DEBUG_DIR)"
    echo "   • Permissions: $(stat -c '%a' $DEBUG_DIR)"
    
    # Count debug images
    IMAGE_COUNT=$(find "$DEBUG_DIR" -name "*.jpg" -type f 2>/dev/null | wc -l)
    echo "   • Debug images: $IMAGE_COUNT files"
    
    if [[ $IMAGE_COUNT -gt 0 ]]; then
        LATEST_IMAGE=$(find "$DEBUG_DIR" -name "*.jpg" -type f -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -1 | cut -d' ' -f2-)
        if [[ -n "$LATEST_IMAGE" ]]; then
            echo "   • Latest image: $(basename "$LATEST_IMAGE")"
            echo "   • Modified: $(stat -c '%y' "$LATEST_IMAGE")"
            echo "   • Size: $(stat -c '%s' "$LATEST_IMAGE") bytes"
        fi
    fi
else
    print_status $YELLOW "⚠️  Debug images directory missing: $DEBUG_DIR"
fi

print_section "5. Configuration"
# Check for environment variables (preferred) or secrets file (legacy)
SECRETS_FILE="$APP_DIR/secrets.json"

# If running as root, try to load environment variables from service environment file
if [[ $EUID -eq 0 ]] && [[ -f "/etc/wellmonitor/environment" ]]; then
    print_status $BLUE "🔍 Loading environment variables from service environment file..."
    source /etc/wellmonitor/environment
fi

# First check for environment variables (check both possible names)
if [[ -n "$WELLMONITOR_IOTHUB_CONNECTION_STRING" ]]; then
    print_status $GREEN "✅ Azure IoT connection string found: WELLMONITOR_IOTHUB_CONNECTION_STRING"
    # Check if it looks like a valid Azure IoT connection string
    if [[ "$WELLMONITOR_IOTHUB_CONNECTION_STRING" == *"HostName="*"azure-devices.net"* ]]; then
        print_status $GREEN "✅ Environment variable contains valid Azure IoT Hub hostname"
    else
        print_status $YELLOW "⚠️  Environment variable doesn't look like Azure IoT connection string"
    fi
    
    # Check secrets mode
    if [[ "$WELLMONITOR_SECRETS_MODE" == "environment" ]]; then
        print_status $GREEN "✅ Secrets mode set to environment variables"
    else
        print_status $YELLOW "⚠️  WELLMONITOR_SECRETS_MODE not set to 'environment'"
    fi
elif [[ -n "$AZURE_IOT_DEVICE_CONNECTION_STRING" ]]; then
    print_status $GREEN "✅ Azure IoT connection string found: AZURE_IOT_DEVICE_CONNECTION_STRING"
    # Check if it looks like a valid Azure IoT connection string
    if [[ "$AZURE_IOT_DEVICE_CONNECTION_STRING" == *"HostName="*"azure-devices.net"* ]]; then
        print_status $GREEN "✅ Environment variable contains valid Azure IoT Hub hostname"
    else
        print_status $YELLOW "⚠️  Environment variable doesn't look like Azure IoT connection string"
    fi
elif [[ -f "$SECRETS_FILE" ]]; then
    print_status $YELLOW "⚠️  Using legacy secrets.json file: $SECRETS_FILE"
    echo "   • Owner: $(stat -c '%U:%G' $SECRETS_FILE)"
    echo "   • Permissions: $(stat -c '%a' $SECRETS_FILE)"
    print_status $BLUE "💡 Consider migrating to environment variable: AZURE_IOT_DEVICE_CONNECTION_STRING"
    
    # Check if file contains Azure IoT connection string (without revealing it)
    if grep -q "DeviceConnectionString" "$SECRETS_FILE" 2>/dev/null; then
        print_status $GREEN "✅ Device connection string found in secrets.json"
    else
        print_status $RED "❌ Device connection string missing from secrets.json"
    fi
    
    if grep -q "HostName.*azure-devices.net" "$SECRETS_FILE" 2>/dev/null; then
        print_status $GREEN "✅ Azure IoT Hub hostname found in secrets.json"
    else
        print_status $YELLOW "⚠️  Azure IoT Hub hostname not detected in secrets.json"
    fi
else
    print_status $RED "❌ No Azure IoT configuration found"
    print_status $YELLOW "💡 Set environment variable: WELLMONITOR_IOTHUB_CONNECTION_STRING"
    print_status $YELLOW "   And: WELLMONITOR_SECRETS_MODE=environment"
    print_status $YELLOW "   Or create secrets.json with Azure IoT connection string"
fi

print_section "6. Recent Service Logs"
print_status $BLUE "📜 Last 10 log entries (last 5 minutes):"
echo "----------------------------------------"
if journalctl -u wellmonitor --since "5 minutes ago" -n 10 --no-pager 2>/dev/null; then
    echo "----------------------------------------"
else
    print_status $YELLOW "⚠️  No recent logs found or insufficient permissions"
    print_status $YELLOW "💡 Run with sudo to see logs, or check: sudo journalctl -u wellmonitor -f"
fi

print_section "7. Device Twin Sync Indicators"
print_status $BLUE "🔍 Checking for device twin sync in logs..."

# Look for device twin related log messages
DEVICE_TWIN_LOGS=$(journalctl -u wellmonitor --since "1 hour ago" --no-pager 2>/dev/null | grep -i "device.*twin\|azure\|connection\|configuration" | tail -5)

if [[ -n "$DEVICE_TWIN_LOGS" ]]; then
    echo "Recent Azure/Device Twin related logs:"
    echo "$DEVICE_TWIN_LOGS"
else
    print_status $YELLOW "⚠️  No device twin sync logs found in the last hour"
fi

# Check for camera configuration in logs
CAMERA_LOGS=$(journalctl -u wellmonitor --since "30 minutes ago" --no-pager 2>/dev/null | grep -i "camera\|debug.*image\|gain\|shutter" | tail -3)

if [[ -n "$CAMERA_LOGS" ]]; then
    echo ""
    echo "Recent camera configuration logs:"
    echo "$CAMERA_LOGS"
fi

print_section "8. Camera Hardware Status"
# Check if camera is detected
if [[ -e /dev/video0 ]]; then
    print_status $GREEN "✅ Camera device detected: /dev/video0"
    echo "   • Permissions: $(stat -c '%a' /dev/video0)"
    echo "   • Owner: $(stat -c '%U:%G' /dev/video0)"
else
    print_status $RED "❌ Camera device not found: /dev/video0"
    print_status $YELLOW "💡 Check camera connection and enable legacy camera support"
fi

# Check GPU memory (needed for camera)
GPU_MEM=$(vcgencmd get_mem gpu 2>/dev/null | cut -d'=' -f2 | cut -d'M' -f1)
if [[ -n "$GPU_MEM" && "$GPU_MEM" -ge 128 ]]; then
    print_status $GREEN "✅ GPU memory: ${GPU_MEM}M (sufficient)"
else
    print_status $YELLOW "⚠️  GPU memory: ${GPU_MEM}M (may need increase to 128M+)"
    print_status $YELLOW "💡 Add 'gpu_mem=128' to /boot/config.txt"
fi

print_section "9. System Resources"
echo "Memory Usage:"
free -h

echo ""
echo "Disk Usage:"
df -h /opt /var/lib

echo ""
echo "Load Average:"
uptime

print_section "10. Troubleshooting Recommendations"

# Determine likely issues and recommendations
echo "Based on the analysis above:"
echo ""

if ! systemctl is-active --quiet wellmonitor; then
    print_status $RED "🔧 CRITICAL: Service not running"
    echo "   → sudo systemctl start wellmonitor"
    echo "   → sudo systemctl enable wellmonitor"
fi

if ! ping -c 1 azure.microsoft.com &> /dev/null; then
    print_status $RED "🔧 CRITICAL: No Azure connectivity"
    echo "   → Check internet connection and firewall"
    echo "   → Verify DNS resolution"
fi

if [[ -z "$WELLMONITOR_IOTHUB_CONNECTION_STRING" ]] && [[ -z "$AZURE_IOT_DEVICE_CONNECTION_STRING" ]] && ([[ ! -f "$SECRETS_FILE" ]] || ! grep -q "DeviceConnectionString" "$SECRETS_FILE" 2>/dev/null); then
    print_status $RED "🔧 CRITICAL: Missing Azure IoT configuration"
    echo "   → Set environment variable: export WELLMONITOR_IOTHUB_CONNECTION_STRING='HostName=...'"
    echo "   → Set: export WELLMONITOR_SECRETS_MODE=environment"
    echo "   → Add to systemd service file or /etc/environment"
    echo "   → Or create/update secrets.json with connection string (legacy)"
fi

if [[ ! -d "$DEBUG_DIR" ]]; then
    print_status $YELLOW "🔧 RECOMMENDED: Create debug images directory"
    echo "   → sudo mkdir -p $DEBUG_DIR"
    echo "   → sudo chown wellmonitor:wellmonitor $DEBUG_DIR"
fi

if [[ ! -e /dev/video0 ]]; then
    print_status $YELLOW "🔧 RECOMMENDED: Fix camera detection"
    echo "   → Check camera cable connection"
    echo "   → Run camera configuration script"
fi

print_status $GREEN "✅ Troubleshooting complete!"
print_status $BLUE "💡 For real-time monitoring: sudo journalctl -u wellmonitor -f"
