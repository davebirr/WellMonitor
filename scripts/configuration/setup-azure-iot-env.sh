#!/bin/bash
# Set up Azure IoT Device Connection String as Environment Variable
# This script helps migrate from secrets.json to environment variables

set -e

echo "🔧 Azure IoT Environment Variable Setup"
echo "======================================="
echo "This script helps set up the Azure IoT connection string as an environment variable"
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

print_section "1. Current Configuration Check"

# Check what's currently configured
if [[ -n "$AZURE_IOT_DEVICE_CONNECTION_STRING" ]]; then
    print_status $GREEN "✅ Environment variable already set"
    echo "Current value: ${AZURE_IOT_DEVICE_CONNECTION_STRING:0:50}..."
    read -p "Do you want to update it? (y/N): " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status $BLUE "💡 Keeping current environment variable"
        exit 0
    fi
fi

# Check for existing secrets.json
SECRETS_FILE="/opt/wellmonitor/secrets.json"
if [[ -f "$SECRETS_FILE" ]]; then
    print_status $YELLOW "⚠️  Found existing secrets.json file"
    
    # Try to extract connection string from secrets.json
    CONNECTION_STRING=$(grep -o '"DeviceConnectionString": *"[^"]*"' "$SECRETS_FILE" 2>/dev/null | cut -d'"' -f4)
    
    if [[ -n "$CONNECTION_STRING" ]]; then
        print_status $GREEN "✅ Found connection string in secrets.json"
        echo "Connection string: ${CONNECTION_STRING:0:50}..."
        
        read -p "Use this connection string for environment variable? (Y/n): " -n 1 -r
        echo ""
        if [[ $REPLY =~ ^[Nn]$ ]]; then
            CONNECTION_STRING=""
        fi
    else
        print_status $YELLOW "⚠️  No connection string found in secrets.json"
    fi
fi

print_section "2. Connection String Input"

# Get connection string if not found
if [[ -z "$CONNECTION_STRING" ]]; then
    print_status $BLUE "💡 Please provide your Azure IoT Device connection string"
    echo "Format: HostName=YourIoTHub.azure-devices.net;DeviceId=YourDevice;SharedAccessKey=..."
    echo ""
    read -p "Azure IoT Connection String: " CONNECTION_STRING
    
    if [[ -z "$CONNECTION_STRING" ]]; then
        print_status $RED "❌ No connection string provided"
        exit 1
    fi
fi

# Validate connection string format
if [[ ! "$CONNECTION_STRING" == *"HostName="*"azure-devices.net"* ]]; then
    print_status $RED "❌ Connection string doesn't look valid"
    print_status $YELLOW "💡 Should contain: HostName=...azure-devices.net"
    exit 1
fi

print_section "3. Setting Up Environment Variable"

# Add to systemd service environment
SERVICE_FILE="/etc/systemd/system/wellmonitor.service"
if [[ -f "$SERVICE_FILE" ]]; then
    print_status $BLUE "🔧 Updating systemd service file..."
    
    # Remove any existing environment variable
    sed -i '/Environment.*AZURE_IOT_DEVICE_CONNECTION_STRING/d' "$SERVICE_FILE"
    
    # Add the new environment variable after [Service]
    sed -i "/^\[Service\]/a Environment=AZURE_IOT_DEVICE_CONNECTION_STRING=$CONNECTION_STRING" "$SERVICE_FILE"
    
    print_status $GREEN "✅ Environment variable added to systemd service"
    
    # Reload systemd
    systemctl daemon-reload
    print_status $GREEN "✅ Systemd configuration reloaded"
else
    print_status $RED "❌ Systemd service file not found: $SERVICE_FILE"
    print_status $YELLOW "💡 You'll need to add the environment variable manually"
fi

print_section "4. Testing Configuration"

# Export for current session
export AZURE_IOT_DEVICE_CONNECTION_STRING="$CONNECTION_STRING"
print_status $GREEN "✅ Environment variable set for current session"

# Restart service to pick up new environment variable
print_status $BLUE "🔄 Restarting wellmonitor service..."
systemctl restart wellmonitor
sleep 3

if systemctl is-active --quiet wellmonitor; then
    print_status $GREEN "✅ Service restarted successfully"
else
    print_status $RED "❌ Service failed to restart"
    print_status $YELLOW "💡 Check logs: journalctl -u wellmonitor -n 20"
fi

print_section "5. Migration Complete"

print_status $GREEN "🎯 Environment variable setup complete!"
print_status $BLUE "💡 Next steps:"
echo ""
echo "1. Monitor service logs for Azure IoT connection:"
echo "   sudo journalctl -u wellmonitor -f"
echo ""
echo "2. Run device twin diagnostics:"
echo "   ./scripts/diagnostics/check-device-configuration.sh"
echo ""
echo "3. If connection successful, consider removing secrets.json:"
echo "   sudo rm /opt/wellmonitor/secrets.json"
echo ""

# Show current environment variable (masked for security)
MASKED_STRING="${CONNECTION_STRING:0:30}...${CONNECTION_STRING: -10}"
print_status $GREEN "✅ Environment variable: $MASKED_STRING"
