#!/bin/bash

echo "🔍 WellMonitor Camera Path Debug"
echo "==============================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}1. Camera executable paths:${NC}"
echo "   which libcamera-still: $(which libcamera-still 2>/dev/null || echo 'Not found')"
echo "   which rpicam-still: $(which rpicam-still 2>/dev/null || echo 'Not found')"

echo ""
echo -e "${BLUE}2. Camera executable details:${NC}"
if command -v libcamera-still >/dev/null 2>&1; then
    echo "   libcamera-still version:"
    libcamera-still --version 2>&1 | head -3 || echo "   No version info available"
    echo ""
    echo "   libcamera-still file info:"
    ls -la "$(which libcamera-still)" || echo "   Cannot get file info"
    echo ""
    echo "   libcamera-still is symlink to:"
    readlink -f "$(which libcamera-still)" || echo "   Not a symlink"
fi

echo ""
echo -e "${BLUE}3. Camera packages installed:${NC}"
dpkg -l | grep -E "libcamera|rpicam" | awk '{print "   " $2 ": " $3}' || echo "   No camera packages found"

echo ""
echo -e "${BLUE}4. Camera modules loaded:${NC}"
lsmod | grep -E "bcm2835|v4l2" | awk '{print "   " $1}' || echo "   No camera modules loaded"

echo ""
echo -e "${BLUE}5. Camera devices available:${NC}"
ls -la /dev/video* 2>/dev/null | awk '{print "   " $0}' || echo "   No video devices found"

echo ""
echo -e "${BLUE}6. Test simple camera command:${NC}"
echo "   Testing: libcamera-still --help | head -3"
libcamera-still --help 2>&1 | head -3 || echo "   Command failed"

echo ""
echo -e "${BLUE}7. systemd service environment:${NC}"
echo "   Service user: $(systemctl show wellmonitor -p User --value 2>/dev/null || echo 'Unknown')"
echo "   Service working dir: $(systemctl show wellmonitor -p WorkingDirectory --value 2>/dev/null || echo 'Unknown')"
echo "   Service environment file: $(systemctl show wellmonitor -p EnvironmentFiles --value 2>/dev/null || echo 'None')"

echo ""
echo -e "${BLUE}8. User environment PATH:${NC}"
echo "   Current PATH: $PATH"

echo ""
echo -e "${BLUE}9. Run as service user test:${NC}"
if id davidb >/dev/null 2>&1; then
    echo "   Testing camera as 'davidb' user:"
    sudo -u davidb bash -c 'which libcamera-still 2>/dev/null || echo "   libcamera-still not found in PATH"'
    sudo -u davidb bash -c 'libcamera-still --version 2>&1 | head -1 || echo "   Cannot run libcamera-still"'
else
    echo "   User 'davidb' not found"
fi

echo ""
echo -e "${GREEN}✅ Camera path debug complete${NC}"
echo ""
echo -e "${YELLOW}💡 Common issues and solutions:${NC}"
echo "   • If libcamera-still is a symlink to rpicam-still, update WellMonitor to use rpicam-still"
echo "   • If PATH is different for service user, add explicit path to camera command"
echo "   • If packages are missing, install: sudo apt update && sudo apt install libcamera-apps"
echo "   • If dmaHeap error persists, check /boot/firmware/config.txt for camera settings"
