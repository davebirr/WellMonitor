#!/bin/bash

echo "🔄 Force Device Twin Debug Settings Update"
echo "=========================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${YELLOW}This script forces the application to reload device twin debug settings${NC}"
echo ""

# First, let's check what the application currently sees
echo -e "${BLUE}📋 Current application debug settings:${NC}"
sudo journalctl -u wellmonitor --since "1 minute ago" | grep -E "Debug.*check|ImageSaveEnabled" | tail -3

echo ""
echo -e "${BLUE}🔄 Forcing device twin sync by restarting runtime configuration...${NC}"

# The easiest way to force a device twin sync is to restart the service
# But first let's check if there's a specific method to trigger device twin updates

echo -e "${BLUE}🛑 Restarting wellmonitor service to force device twin reload...${NC}"
sudo systemctl restart wellmonitor

echo -e "${BLUE}⏳ Waiting for service to restart and sync device twin...${NC}"
sleep 15

echo ""
echo -e "${BLUE}📊 Service Status:${NC}"
sudo systemctl status wellmonitor --no-pager -l | head -10

echo ""
echo -e "${BLUE}🔍 Checking for device twin sync logs...${NC}"
sudo journalctl -u wellmonitor --since "30 seconds ago" | grep -i -E "device twin|debug.*load|configuration.*applied|ImageSave" | head -10

echo ""
echo -e "${BLUE}📝 Latest debug image check:${NC}"
# Wait for next monitoring cycle
sleep 35
sudo journalctl -u wellmonitor --since "10 seconds ago" | grep -E "Debug.*check|ImageSaveEnabled|debug.*image.*saving" | tail -3

echo ""
echo -e "${BLUE}💡 If ImageSaveEnabled is still False, the device twin may not have the correct properties.${NC}"
echo -e "${YELLOW}   Check device twin with: ./scripts/diagnostics/check-device-twin-sync.ps1${NC}"
echo -e "${YELLOW}   Update device twin with: ./scripts/configuration/update-device-twin.ps1${NC}"

echo ""
echo -e "${BLUE}🔍 Monitor continuous logs with: journalctl -u wellmonitor -f${NC}"
