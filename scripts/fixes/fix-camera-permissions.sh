#!/bin/bash

echo "🔧 WellMonitor Camera Service Fix"
echo "================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🛑 Stopping WellMonitor service...${NC}"
sudo systemctl stop wellmonitor

echo -e "${BLUE}📝 Updating service file for camera compatibility...${NC}"

# Backup current service file
sudo cp /etc/systemd/system/wellmonitor.service /etc/systemd/system/wellmonitor.service.backup.$(date +%Y%m%d_%H%M%S)
echo -e "${GREEN}✅ Service file backed up${NC}"

# Create updated service file with proper camera permissions
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

# Security - BALANCED PROTECTION (less restrictive for camera)
NoNewPrivileges=yes
PrivateTmp=yes
ProtectHome=yes
ProtectSystem=yes
ProtectKernelTunables=yes
ProtectKernelModules=yes
ProtectControlGroups=yes
RestrictRealtime=yes
SystemCallArchitectures=native

# Allow access to specific directories
ReadWritePaths=/var/lib/wellmonitor
ReadWritePaths=/var/log/wellmonitor
ReadOnlyPaths=/etc/wellmonitor

# Device access for GPIO and camera (expanded for rpicam-apps)
DeviceAllow=/dev/gpiochip0 rw
DeviceAllow=/dev/video* rw
DeviceAllow=/dev/dma_heap rw
DeviceAllow=/dev/dma_heap/* rw
DeviceAllow=/dev/dri rw
DeviceAllow=/dev/dri/* rw
DeviceAllow=/dev/vchiq rw
DeviceAllow=/dev/vcio rw
SupplementaryGroups=gpio video render

[Install]
WantedBy=multi-user.target
EOF

echo -e "${GREEN}✅ Service file updated with camera permissions${NC}"

echo -e "${BLUE}🔄 Reloading systemd and restarting service...${NC}"
sudo systemctl daemon-reload
sudo systemctl start wellmonitor

echo -e "${BLUE}⏳ Waiting for service startup...${NC}"
sleep 5

echo ""
echo -e "${BLUE}📊 Service status:${NC}"
sudo systemctl status wellmonitor --no-pager -l

echo ""
echo -e "${BLUE}📝 Recent logs (last 10 lines):${NC}"
sudo journalctl -u wellmonitor -n 10 --no-pager

echo ""
echo -e "${GREEN}🎉 Service update complete!${NC}"
echo ""
echo -e "${BLUE}💡 Changes made:${NC}"
echo "  • Added DMA heap device access (/dev/dma_heap)"
echo "  • Added DRI device access (/dev/dri/*)"
echo "  • Added vchiq/vcio device access"
echo "  • Added 'render' group membership"
echo "  • Reduced ProtectSystem from 'strict' to 'yes'"
echo ""
echo -e "${BLUE}📋 Monitor logs:${NC}"
echo "  sudo journalctl -u wellmonitor -f"
