#!/bin/bash

echo "=== WellMonitor Camera Diagnostics ==="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔍 System Information:${NC}"
echo "  OS: $(cat /etc/os-release | grep PRETTY_NAME | cut -d'"' -f2)"
echo "  Kernel: $(uname -r)"
echo "  Architecture: $(uname -m)"
echo "  Raspberry Pi Model: $(cat /proc/cpuinfo | grep 'Model' | cut -d':' -f2 | xargs)"
echo ""

echo -e "${BLUE}📷 Camera Hardware Detection:${NC}"
# Check for camera devices
if ls /dev/video* >/dev/null 2>&1; then
    echo -e "${GREEN}✅ Camera devices found:${NC}"
    ls -la /dev/video*
else
    echo -e "${RED}❌ No camera devices found${NC}"
fi
echo ""

echo -e "${BLUE}🔧 Camera Configuration:${NC}"
# Check boot config
if [ -f /boot/firmware/config.txt ]; then
    CONFIG_FILE="/boot/firmware/config.txt"
elif [ -f /boot/config.txt ]; then
    CONFIG_FILE="/boot/config.txt"
else
    echo -e "${RED}❌ Boot config file not found${NC}"
    CONFIG_FILE=""
fi

if [ -n "$CONFIG_FILE" ]; then
    echo "  Config file: $CONFIG_FILE"
    echo "  Camera settings:"
    grep -E "(camera|start_x)" "$CONFIG_FILE" | head -10 || echo "    No camera settings found"
fi
echo ""

echo -e "${BLUE}📦 Camera Modules:${NC}"
# Check loaded modules
if lsmod | grep -E "(bcm2835|ov5647|imx219|imx477)" > /dev/null; then
    echo -e "${GREEN}✅ Camera modules loaded:${NC}"
    lsmod | grep -E "(bcm2835|ov5647|imx219|imx477)"
else
    echo -e "${YELLOW}⚠️  No camera modules detected${NC}"
fi
echo ""

echo -e "${BLUE}🛠️  Camera Software:${NC}"
# Check for camera software
if command -v libcamera-still >/dev/null 2>&1; then
    echo -e "${GREEN}✅ libcamera-still found${NC}"
    echo "    Version: $(libcamera-still --version 2>&1 | head -1)"
else
    echo -e "${RED}❌ libcamera-still not found${NC}"
fi

if command -v rpicam-still >/dev/null 2>&1; then
    echo -e "${GREEN}✅ rpicam-still found${NC}"
    echo "    Version: $(rpicam-still --version 2>&1 | head -1)"
else
    echo -e "${YELLOW}⚠️  rpicam-still not found${NC}"
fi
echo ""

echo -e "${BLUE}🧪 Camera Test:${NC}"
if command -v libcamera-still >/dev/null 2>&1; then
    echo "  Testing libcamera-still..."
    if timeout 10s libcamera-still --list-cameras 2>/dev/null | grep -q "Available cameras"; then
        echo -e "${GREEN}✅ libcamera-still can detect cameras${NC}"
        libcamera-still --list-cameras 2>/dev/null
    else
        echo -e "${RED}❌ libcamera-still camera test failed${NC}"
        echo "    Error output:"
        timeout 10s libcamera-still --list-cameras 2>&1 | head -5 | sed 's/^/    /'
    fi
else
    echo -e "${YELLOW}⚠️  Cannot test - libcamera-still not available${NC}"
fi
echo ""

echo -e "${BLUE}💡 Troubleshooting Tips:${NC}"
echo "1. Enable camera interface:"
echo "   sudo raspi-config"
echo "   → Interface Options → Camera → Enable"
echo "   → Finish → Reboot"
echo ""
echo "2. Add to $CONFIG_FILE (if missing):"
echo "   camera_auto_detect=1"
echo "   start_x=1"
echo ""
echo "3. Install camera software (if missing):"
echo "   sudo apt update"
echo "   sudo apt install libcamera-apps rpicam-apps"
echo ""
echo "4. Check permissions:"
echo "   sudo usermod -a -G video davidb"
echo "   logout and login again"
echo ""

echo -e "${BLUE}🔄 Next Steps:${NC}"
echo "After making changes, restart the WellMonitor service:"
echo "  sudo systemctl restart wellmonitor"
echo "  sudo journalctl -u wellmonitor -f"
