#!/bin/bash

echo "🚨 Emergency Camera Fix - Disable Systemd Security"
echo "=================================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${YELLOW}⚠️  This script temporarily disables systemd security to fix camera access.${NC}"
echo -e "${YELLOW}⚠️  Use this only for troubleshooting - restore security after camera works.${NC}"
echo ""

read -p "Continue? (y/N): " confirm
if [[ ! "$confirm" =~ ^[Yy] ]]; then
    echo "Cancelled."
    exit 0
fi

echo -e "${BLUE}🛑 Stopping WellMonitor service...${NC}"
sudo systemctl stop wellmonitor

echo -e "${BLUE}📝 Creating minimal security service file...${NC}"

# Backup current service file
sudo cp /etc/systemd/system/wellmonitor.service /etc/systemd/system/wellmonitor.service.security-backup.$(date +%Y%m%d_%H%M%S)
echo -e "${GREEN}✅ Service file backed up${NC}"

# Create service file with MINIMAL security (for troubleshooting only)
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

# MINIMAL SECURITY - FOR TROUBLESHOOTING ONLY
# Comment out most restrictions to test camera access
# NoNewPrivileges=yes
# PrivateTmp=yes
# ProtectHome=yes
# ProtectSystem=yes
# ProtectKernelTunables=yes
# ProtectKernelModules=yes
# ProtectControlGroups=yes
# RestrictRealtime=yes
# SystemCallArchitectures=native

# Allow access to specific directories
ReadWritePaths=/var/lib/wellmonitor
ReadWritePaths=/var/log/wellmonitor
ReadOnlyPaths=/etc/wellmonitor

# Full device access (troubleshooting mode)
SupplementaryGroups=gpio video render

[Install]
WantedBy=multi-user.target
EOF

echo -e "${GREEN}✅ Minimal security service file created${NC}"

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
echo -e "${YELLOW}🔍 Testing camera access...${NC}"
sleep 10
echo -e "${BLUE}📝 Camera test logs:${NC}"
sudo journalctl -u wellmonitor -n 5 --no-pager | grep -E "camera|Camera|ERROR|error"

echo ""
if sudo journalctl -u wellmonitor -n 10 --no-pager | grep -q "Camera capture.*successful\|✅"; then
    echo -e "${GREEN}🎉 Camera appears to be working!${NC}"
    echo ""
    echo -e "${YELLOW}📋 Next steps:${NC}"
    echo "1. Monitor debug images: ls -la /var/lib/wellmonitor/debug_images/"
    echo "2. Restore security: ./scripts/fixes/restore-camera-security.sh"
else
    echo -e "${RED}❌ Camera still not working with minimal security.${NC}"
    echo ""
    echo -e "${YELLOW}💡 Additional troubleshooting needed:${NC}"
    echo "1. Check camera hardware: ./scripts/diagnostics/diagnose-camera.sh"
    echo "2. Verify Pi camera is working: libcamera-still --output test.jpg"
    echo "3. Check if camera is in use by another process"
fi

echo ""
echo -e "${YELLOW}⚠️  IMPORTANT: Restore security after testing!${NC}"
echo "Run: ./scripts/fixes/restore-camera-security.sh"
