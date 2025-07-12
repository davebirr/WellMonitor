#!/bin/bash
# Fix camera service configuration for platform detection issues

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔧 Fixing Camera Service Configuration${NC}"
echo "======================================"

# Stop the service
echo -e "${YELLOW}⏹️  Stopping wellmonitor service...${NC}"
sudo systemctl stop wellmonitor

# Create updated service file with camera-friendly settings
echo -e "${BLUE}📝 Updating service configuration...${NC}"
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

# Security - CAMERA-COMPATIBLE SETTINGS
NoNewPrivileges=yes
PrivateTmp=yes
ProtectHome=no          # ✅ DISABLED - Camera needs platform detection access
ProtectSystem=no        # ✅ DISABLED - Camera needs system access for platform detection
ProtectKernelTunables=no
ProtectKernelModules=no
ProtectControlGroups=no
RestrictRealtime=yes
SystemCallArchitectures=native

# Allow access to required directories
ReadWritePaths=/var/lib/wellmonitor
ReadWritePaths=/var/log/wellmonitor
ReadOnlyPaths=/etc/wellmonitor

# Full device access for camera
DevicePolicy=auto
SupplementaryGroups=gpio video

[Install]
WantedBy=multi-user.target
EOF

echo -e "${GREEN}✅ Updated service configuration for camera compatibility${NC}"

# Reload systemd
echo -e "${BLUE}🔄 Reloading systemd configuration...${NC}"
sudo systemctl daemon-reload

# Start the service
echo -e "${GREEN}🚀 Starting wellmonitor service...${NC}"
sudo systemctl start wellmonitor

# Wait for startup
sleep 5

# Check status
echo ""
echo -e "${BLUE}📊 Service status:${NC}"
sudo systemctl status wellmonitor --no-pager -l | head -20

echo ""
echo -e "${BLUE}📝 Recent logs (checking for camera errors):${NC}"
sudo journalctl -u wellmonitor -n 10 --no-pager | grep -E "camera|Camera|ERROR|error" || echo "No camera errors found"

echo ""
echo -e "${GREEN}🎉 Camera Service Fix Complete!${NC}"
echo "=================================="
echo -e "${YELLOW}⚠️  Security note: Some systemd protections disabled for camera compatibility${NC}"
echo -e "${GREEN}✅ Camera platform detection should now work${NC}"
echo -e "${GREEN}✅ Debug images should resume being created${NC}"
echo ""
echo -e "${BLUE}📋 Monitor service:${NC}"
echo "sudo journalctl -u wellmonitor -f"
echo ""
echo -e "${BLUE}📁 Check debug images:${NC}"
echo "ls -la /var/lib/wellmonitor/debug_images/"
