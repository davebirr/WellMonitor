#!/bin/bash

echo "🔧 Comprehensive Camera & Logging Fix"
echo "===================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${YELLOW}This script fixes both camera permissions and Entity Framework logging noise${NC}"
echo ""

# Stop the service first
echo -e "${BLUE}🛑 Stopping WellMonitor service...${NC}"
sudo systemctl stop wellmonitor

# Backup current service file
echo -e "${BLUE}💾 Backing up current service file...${NC}"
sudo cp /etc/systemd/system/wellmonitor.service /etc/systemd/system/wellmonitor.service.backup.comprehensive-fix.$(date +%Y%m%d_%H%M%S)

# Create the ultimate camera fix service file
echo -e "${BLUE}🔧 Creating camera-optimized service file (ULTIMATE FIX)...${NC}"
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

# ULTIMATE CAMERA FIX - MINIMAL RESTRICTIONS
# The camera subsystem needs broad access to system resources
NoNewPrivileges=yes

# CAMERA FIX: Home access required for camera initialization
ProtectHome=no

# CAMERA FIX: Allow system access for camera subsystem
ProtectSystem=no

# CAMERA FIX: Disable most security restrictions that block camera
# ProtectKernelTunables=no
# ProtectKernelModules=no
# ProtectControlGroups=no
# RestrictRealtime=no

# CAMERA FIX: Allow all device access
DevicePolicy=auto

# Allow access to all necessary directories
ReadWritePaths=/var/lib/wellmonitor
ReadWritePaths=/var/log/wellmonitor
ReadWritePaths=/tmp
ReadWritePaths=/run
ReadWritePaths=/dev/shm
ReadOnlyPaths=/etc/wellmonitor

# CAMERA FIX: Comprehensive device permissions
DeviceAllow=char-* rw
DeviceAllow=block-* rw
SupplementaryGroups=gpio video render dialout input audio users

[Install]
WantedBy=multi-user.target
EOF

echo -e "${GREEN}✅ Ultimate camera fix service file created${NC}"

# Update the application configuration to suppress EF logging
echo -e "${BLUE}🔧 Updating application configuration to suppress Entity Framework logging...${NC}"
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

echo -e "${GREEN}✅ Production logging configuration created${NC}"

# Set production environment
echo -e "${BLUE}🔧 Setting ASPNETCORE_ENVIRONMENT to Production...${NC}"
if ! grep -q "ASPNETCORE_ENVIRONMENT=Production" /etc/wellmonitor/environment; then
    echo "ASPNETCORE_ENVIRONMENT=Production" | sudo tee -a /etc/wellmonitor/environment
fi

echo -e "${BLUE}🔄 Reloading systemd and starting service...${NC}"
sudo systemctl daemon-reload
sudo systemctl start wellmonitor

# Wait for service to start
echo -e "${BLUE}⏳ Waiting for service to initialize...${NC}"
sleep 8

echo ""
echo -e "${BLUE}📊 Service Status:${NC}"
sudo systemctl status wellmonitor --no-pager -l

echo ""
echo -e "${BLUE}🔍 Testing camera and logging (next 15 seconds)...${NC}"
sleep 15

echo ""
echo -e "${BLUE}📝 Recent logs (checking for camera and SQL noise):${NC}"
sudo journalctl -u wellmonitor --since "1 minute ago" | tail -20

echo ""
echo -e "${BLUE}🎯 Camera-specific logs:${NC}"
sudo journalctl -u wellmonitor --since "1 minute ago" | grep -E "camera|Camera|ERROR|no cameras|media device" | tail -10

echo ""
echo -e "${BLUE}📊 SQL logging check (should be minimal):${NC}"
sudo journalctl -u wellmonitor --since "1 minute ago" | grep -E "SELECT|INSERT|Executed DbCommand" | wc -l
sql_count=$(sudo journalctl -u wellmonitor --since "1 minute ago" | grep -E "SELECT|INSERT|Executed DbCommand" | wc -l)

if [ "$sql_count" -eq 0 ]; then
    echo -e "${GREEN}🎉 SQL logging successfully suppressed!${NC}"
elif [ "$sql_count" -lt 5 ]; then
    echo -e "${YELLOW}⚠️  Minimal SQL logging (${sql_count} entries) - acceptable${NC}"
else
    echo -e "${RED}❌ SQL logging still excessive (${sql_count} entries)${NC}"
fi

echo ""
if sudo journalctl -u wellmonitor --since "1 minute ago" | grep -q "no cameras available\|Operation not permitted"; then
    echo -e "${RED}❌ Camera access still failing${NC}"
    echo -e "${YELLOW}💡 Try running as root to test: sudo libcamera-hello --list-cameras${NC}"
    echo -e "${YELLOW}💡 May need to disable more systemd security restrictions${NC}"
else
    echo -e "${GREEN}🎉 Camera appears to be working!${NC}"
    echo -e "${GREEN}📸 Check for captured images: ls -la /var/lib/wellmonitor/debug_images/${NC}"
fi

echo ""
echo -e "${YELLOW}📋 Monitor logs continuously with: journalctl -u wellmonitor -f${NC}"
echo -e "${YELLOW}🔍 This fix disables most systemd security for camera access${NC}"
