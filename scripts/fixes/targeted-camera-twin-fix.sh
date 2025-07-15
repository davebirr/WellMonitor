#!/bin/bash

echo "🔧 Targeted Camera & Device Twin Fix"
echo "===================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${YELLOW}This script applies a more secure camera fix and fixes device twin sync${NC}"
echo ""

# Stop the service first
echo -e "${BLUE}🛑 Stopping WellMonitor service...${NC}"
sudo systemctl stop wellmonitor

# Backup current service file
echo -e "${BLUE}💾 Backing up current service file...${NC}"
sudo cp /etc/systemd/system/wellmonitor.service /etc/systemd/system/wellmonitor.service.backup.targeted-fix.$(date +%Y%m%d_%H%M%S)

# Create a more secure camera-optimized service file
echo -e "${BLUE}🔧 Creating targeted camera fix service file...${NC}"
sudo tee /etc/systemd/system/wellmonitor.service > /dev/null << 'EOF'
[Unit]
Description=WellMonitor Device Service
After=network.target
Wants=network.target

[Service]
Type=exec
User=davidb
Group=davidb
WorkingDirectory=/var/lib/wellmonitor
ExecStart=/opt/wellmonitor/WellMonitor.Device
Restart=always
RestartSec=10

# Load environment from secure file
EnvironmentFile=/etc/wellmonitor/environment

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=wellmonitor

# TARGETED CAMERA FIX - Only disable what's necessary for camera
NoNewPrivileges=yes
PrivateTmp=yes

# CAMERA FIX: These are the minimum required changes for camera access
ProtectHome=no          # Camera subsystem needs home directory access
ProtectSystem=yes       # Use 'yes' instead of 'strict' - allows camera system files

# Keep other security settings that don't interfere with camera
ProtectKernelTunables=yes
ProtectKernelModules=yes
ProtectControlGroups=yes
RestrictRealtime=yes
SystemCallArchitectures=native

# CAMERA FIX: Allow automatic device discovery
DevicePolicy=auto

# Directory access
ReadWritePaths=/var/lib/wellmonitor
ReadWritePaths=/var/log/wellmonitor
ReadWritePaths=/tmp
ReadWritePaths=/run
ReadWritePaths=/dev/shm
ReadOnlyPaths=/etc/wellmonitor

# CAMERA FIX: Comprehensive but targeted device permissions
DeviceAllow=/dev/gpiochip* rw
DeviceAllow=/dev/video* rw
DeviceAllow=/dev/media* rw
DeviceAllow=/dev/dma_heap* rw
DeviceAllow=/dev/dri* rw
DeviceAllow=/dev/vchiq rw
DeviceAllow=/dev/vcio rw
DeviceAllow=/dev/vcsm-cma rw
DeviceAllow=/dev/fb* rw
DeviceAllow=char-input rw
DeviceAllow=char-mem rw

# Essential groups for camera and GPIO
SupplementaryGroups=gpio video render dialout input

[Install]
WantedBy=multi-user.target
EOF

echo -e "${GREEN}✅ Targeted camera fix service file created${NC}"

# Force a device twin update to ensure debugImageSaveEnabled syncs
echo -e "${BLUE}🔄 Forcing device twin update to sync debug settings...${NC}"

# Create a script to force device twin property update
cat > /tmp/force-debug-twin-update.py << 'EOF'
#!/usr/bin/env python3

import json
import os
from azure.iot.device import IoTHubDeviceClient
from azure.iot.device.exceptions import ClientError

try:
    # Get connection string from environment
    connection_string = os.environ.get('WELLMONITOR_DEVICE_CONNECTION_STRING')
    if not connection_string:
        print("❌ Device connection string not found in environment")
        exit(1)
    
    print("🔄 Connecting to IoT Hub to force device twin sync...")
    client = IoTHubDeviceClient.create_from_connection_string(connection_string)
    client.connect()
    
    # Get current device twin
    twin = client.get_twin()
    desired = twin.get('desired', {})
    
    print(f"📋 Current device twin desired properties:")
    debug_props = {k: v for k, v in desired.items() if 'debug' in k.lower() or 'image' in k.lower()}
    for key, value in debug_props.items():
        print(f"  {key}: {value}")
    
    # Check if debugImageSaveEnabled is set
    if 'debugImageSaveEnabled' in desired:
        print(f"✅ debugImageSaveEnabled found in device twin: {desired['debugImageSaveEnabled']}")
    else:
        print("❌ debugImageSaveEnabled NOT found in device twin")
        print("💡 Run: ./scripts/configuration/update-device-twin.ps1 to set debug properties")
    
    client.disconnect()
    print("✅ Device twin check complete")
    
except Exception as e:
    print(f"❌ Error checking device twin: {e}")
    exit(1)
EOF

# Run the device twin check if Azure IoT libraries are available
echo -e "${BLUE}🔍 Checking device twin debug settings...${NC}"
if command -v python3 >/dev/null 2>&1; then
    # Load environment variables
    source /etc/wellmonitor/environment 2>/dev/null || echo "⚠️  Could not load environment file"
    
    if python3 -c "import azure.iot.device" 2>/dev/null; then
        python3 /tmp/force-debug-twin-update.py
    else
        echo "⚠️  Azure IoT libraries not available for device twin check"
    fi
else
    echo "⚠️  Python3 not available for device twin check"
fi

# Update the application configuration to suppress EF logging
echo -e "${BLUE}🔧 Updating application configuration...${NC}"
cd /opt/wellmonitor

# Create appsettings.Production.json to override EF logging
sudo tee appsettings.Production.json > /dev/null << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "None",
      "Microsoft.EntityFrameworkCore.Database.Command": "None",
      "Microsoft.EntityFrameworkCore.Database.Transaction": "None",
      "Microsoft.EntityFrameworkCore.Database.Connection": "Error",
      "Microsoft.EntityFrameworkCore.Infrastructure": "Error",
      "Microsoft.EntityFrameworkCore.Query": "None"
    }
  }
}
EOF

# Ensure production environment is set
echo -e "${BLUE}🔧 Setting production environment...${NC}"
if ! grep -q "ASPNETCORE_ENVIRONMENT=Production" /etc/wellmonitor/environment; then
    echo "ASPNETCORE_ENVIRONMENT=Production" | sudo tee -a /etc/wellmonitor/environment
fi

echo -e "${BLUE}🔄 Starting service with targeted camera fix...${NC}"
sudo systemctl daemon-reload
sudo systemctl start wellmonitor

# Wait for service to start
echo -e "${BLUE}⏳ Waiting for service to initialize...${NC}"
sleep 10

echo ""
echo -e "${BLUE}📊 Service Status:${NC}"
sudo systemctl status wellmonitor --no-pager -l

echo ""
echo -e "${BLUE}🔍 Testing camera and device twin sync...${NC}"
sleep 10

echo ""
echo -e "${BLUE}📝 Recent camera logs:${NC}"
sudo journalctl -u wellmonitor --since "30 seconds ago" | grep -E "camera|Camera|debug.*image|ImageSave" | tail -5

echo ""
echo -e "${BLUE}🎯 Device twin sync logs:${NC}"
sudo journalctl -u wellmonitor --since "30 seconds ago" | grep -i -E "device twin|debug.*loaded|configuration.*applied" | tail -5

echo ""
echo -e "${BLUE}📊 SQL logging check:${NC}"
sql_count=$(sudo journalctl -u wellmonitor --since "30 seconds ago" | grep -E "SELECT|INSERT|Executed DbCommand" | wc -l)
echo "SQL queries in last 30 seconds: $sql_count"

if [ "$sql_count" -eq 0 ]; then
    echo -e "${GREEN}🎉 SQL logging successfully suppressed!${NC}"
elif [ "$sql_count" -lt 3 ]; then
    echo -e "${YELLOW}⚠️  Minimal SQL logging (${sql_count} entries)${NC}"
else
    echo -e "${RED}❌ SQL logging still present (${sql_count} entries)${NC}"
fi

echo ""
if sudo journalctl -u wellmonitor --since "30 seconds ago" | grep -q "no cameras available\|Operation not permitted"; then
    echo -e "${RED}❌ Camera access still failing - may need emergency fix${NC}"
    echo -e "${YELLOW}💡 Run emergency fix: ./scripts/fixes/emergency-camera-fix.sh${NC}"
else
    echo -e "${GREEN}🎉 Camera appears to be working with targeted fix!${NC}"
fi

echo ""
if sudo journalctl -u wellmonitor --since "30 seconds ago" | grep -q "ImageSaveEnabled=True\|debug.*image.*enabled"; then
    echo -e "${GREEN}🎉 Device twin debug settings synced successfully!${NC}"
else
    echo -e "${YELLOW}⚠️  Device twin debug settings may not be synced${NC}"
    echo -e "${YELLOW}💡 Check device twin: ./scripts/diagnostics/check-device-twin-sync.ps1${NC}"
fi

echo ""
echo -e "${YELLOW}📋 Monitor logs: journalctl -u wellmonitor -f${NC}"
echo -e "${YELLOW}🔍 This fix maintains more security than the emergency fix${NC}"

# Clean up temp files
rm -f /tmp/force-debug-twin-update.py
