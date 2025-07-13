#!/bin/bash

echo "=== WellMonitor Azure IoT Hub Configuration ==="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

ENV_FILE="/etc/wellmonitor/environment"

# Check if environment file exists
if [ ! -f "$ENV_FILE" ]; then
    echo -e "${RED}❌ Environment file not found: $ENV_FILE${NC}"
    echo "Please run the installation script first."
    exit 1
fi

echo -e "${BLUE}📋 Current configuration status:${NC}"
echo ""

# Check current values
if grep -q "REPLACE_WITH_YOUR_ACTUAL_CONNECTION_STRING\|YourIoTHub\.azure-devices\.net" "$ENV_FILE"; then
    echo -e "${RED}❌ Azure IoT Hub Connection String: Not configured (placeholder)${NC}"
    IOT_CONFIGURED=false
else
    echo -e "${GREEN}✅ Azure IoT Hub Connection String: Configured${NC}"
    IOT_CONFIGURED=true
fi

if grep -q "REPLACE_WITH_32_CHARACTER_ENCRYPTION_KEY\|YourLocalEncryptionKey32Characters" "$ENV_FILE"; then
    echo -e "${RED}❌ Local Encryption Key: Not configured (placeholder)${NC}"
    KEY_CONFIGURED=false
else
    echo -e "${GREEN}✅ Local Encryption Key: Configured${NC}"
    KEY_CONFIGURED=true
fi

echo ""

if [ "$IOT_CONFIGURED" = true ] && [ "$KEY_CONFIGURED" = true ]; then
    echo -e "${GREEN}🎉 Configuration appears to be complete!${NC}"
    echo ""
    echo -e "${BLUE}📊 Service status:${NC}"
    sudo systemctl status wellmonitor --no-pager -l || true
    echo ""
    echo -e "${BLUE}📝 Recent logs:${NC}"
    sudo journalctl -u wellmonitor -n 10 --no-pager || true
    exit 0
fi

echo -e "${YELLOW}🔧 Configuration needed. Let's set this up!${NC}"
echo ""

# Configure Azure IoT Hub Connection String
if [ "$IOT_CONFIGURED" = false ]; then
    echo -e "${BLUE}🔗 Azure IoT Hub Connection String Setup${NC}"
    echo "You need to get this from your Azure IoT Hub in the Azure Portal:"
    echo ""
    echo "1. Go to Azure Portal → IoT Hub → Your Hub"
    echo "2. Go to 'Devices' → Select your device (or create one)"
    echo "3. Copy the 'Primary connection string'"
    echo ""
    echo "Format should be:"
    echo "HostName=your-hub.azure-devices.net;DeviceId=your-device-id;SharedAccessKey=your-key"
    echo ""
    
    read -p "Enter your Azure IoT Hub connection string: " IOT_CONNECTION_STRING
    
    if [ -z "$IOT_CONNECTION_STRING" ]; then
        echo -e "${RED}❌ No connection string provided. Exiting.${NC}"
        exit 1
    fi
    
    # Validate format
    if [[ "$IOT_CONNECTION_STRING" == *"HostName="* ]] && [[ "$IOT_CONNECTION_STRING" == *"DeviceId="* ]] && [[ "$IOT_CONNECTION_STRING" == *"SharedAccessKey="* ]]; then
        echo -e "${GREEN}✅ Connection string format looks valid${NC}"
        
        # Update the file
        sudo sed -i "s|WELLMONITOR_IOTHUB_CONNECTION_STRING=.*|WELLMONITOR_IOTHUB_CONNECTION_STRING=$IOT_CONNECTION_STRING|" "$ENV_FILE"
        echo -e "${GREEN}✅ Azure IoT Hub connection string updated${NC}"
    else
        echo -e "${RED}❌ Invalid connection string format${NC}"
        echo "Must contain: HostName=, DeviceId=, and SharedAccessKey="
        exit 1
    fi
    echo ""
fi

# Configure Local Encryption Key
if [ "$KEY_CONFIGURED" = false ]; then
    echo -e "${BLUE}🔐 Local Encryption Key Setup${NC}"
    echo "This key is used to encrypt sensitive data stored locally."
    echo "It must be exactly 32 characters long."
    echo ""
    echo "You can:"
    echo "1. Enter your own 32-character key"
    echo "2. Let me generate a random one for you"
    echo ""
    
    read -p "Generate random key? (y/n): " GENERATE_KEY
    
    if [[ "$GENERATE_KEY" =~ ^[Yy] ]]; then
        # Generate a random 32-character key
        ENCRYPTION_KEY=$(openssl rand -hex 16)
        echo -e "${GREEN}✅ Generated random encryption key: $ENCRYPTION_KEY${NC}"
    else
        read -p "Enter your 32-character encryption key: " ENCRYPTION_KEY
        
        if [ ${#ENCRYPTION_KEY} -ne 32 ]; then
            echo -e "${RED}❌ Key must be exactly 32 characters. You entered ${#ENCRYPTION_KEY} characters.${NC}"
            exit 1
        fi
    fi
    
    # Update the file
    sudo sed -i "s|WELLMONITOR_LOCAL_ENCRYPTION_KEY=.*|WELLMONITOR_LOCAL_ENCRYPTION_KEY=$ENCRYPTION_KEY|" "$ENV_FILE"
    echo -e "${GREEN}✅ Local encryption key updated${NC}"
    echo ""
fi

# Show final configuration
echo -e "${GREEN}🎉 Configuration Complete!${NC}"
echo ""
echo -e "${BLUE}📋 Final configuration:${NC}"
echo ""
sudo grep -E "WELLMONITOR_IOTHUB_CONNECTION_STRING|WELLMONITOR_LOCAL_ENCRYPTION_KEY" "$ENV_FILE" | sed 's/SharedAccessKey=[^;]*/SharedAccessKey=***HIDDEN***/g'
echo ""

# Restart service
echo -e "${BLUE}🔄 Restarting WellMonitor service...${NC}"
sudo systemctl restart wellmonitor

# Wait for startup
sleep 5

# Check status
echo ""
echo -e "${BLUE}📊 Service status:${NC}"
sudo systemctl status wellmonitor --no-pager -l

echo ""
echo -e "${BLUE}📝 Recent logs:${NC}"
sudo journalctl -u wellmonitor -n 15 --no-pager

echo ""
echo -e "${GREEN}✅ Setup complete! Your WellMonitor should now be connecting to Azure IoT Hub.${NC}"
echo ""
echo -e "${BLUE}📋 Useful commands:${NC}"
echo "  View logs:   sudo journalctl -u wellmonitor -f"
echo "  Restart:     sudo systemctl restart wellmonitor"
echo "  Status:      sudo systemctl status wellmonitor"
echo "  Config:      sudo cat /etc/wellmonitor/environment"
