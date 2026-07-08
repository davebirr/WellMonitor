#!/bin/bash

echo "=== Camera Permission Diagnostics ==="
echo ""

# Check if user is in required groups
echo "🔍 Checking user groups:"
echo "Current user: $(whoami)"
echo "Groups: $(groups)"
echo ""

# Check video group membership
if groups | grep -q "video"; then
    echo "✅ User is in video group"
else
    echo "❌ User is NOT in video group"
    echo "   Run: sudo usermod -a -G video $(whoami)"
fi

# Check render group membership  
if groups | grep -q "render"; then
    echo "✅ User is in render group"
else
    echo "❌ User is NOT in render group"
    echo "   Run: sudo usermod -a -G render $(whoami)"
fi

# Check gpio group membership
if groups | grep -q "gpio"; then
    echo "✅ User is in gpio group"
else
    echo "❌ User is NOT in gpio group"
    echo "   Run: sudo usermod -a -G gpio $(whoami)"
fi

echo ""

# Check device permissions
echo "🔍 Checking device permissions:"

if ls /dev/video* >/dev/null 2>&1; then
    echo "📹 Video devices found:"
    ls -la /dev/video* 2>/dev/null
else
    echo "❌ No /dev/video* devices found"
fi

echo ""

if ls /dev/media* >/dev/null 2>&1; then
    echo "📺 Media devices found:"
    ls -la /dev/media* 2>/dev/null
else
    echo "❌ No /dev/media* devices found"
fi

echo ""

# Check if camera hardware is connected
echo "🔍 Checking camera hardware:"
if command -v vcgencmd >/dev/null 2>&1; then
    echo "Camera detected: $(vcgencmd get_camera)"
else
    echo "❌ vcgencmd not available (not on Raspberry Pi?)"
fi

echo ""

# Test camera access directly
echo "🔍 Testing camera access:"
if command -v libcamera-hello >/dev/null 2>&1; then
    echo "Testing libcamera-hello (timeout 1 second)..."
    timeout 1s libcamera-hello --list-cameras 2>&1 | head -10
elif command -v rpicam-hello >/dev/null 2>&1; then
    echo "Testing rpicam-hello (timeout 1 second)..."
    timeout 1s rpicam-hello --list-cameras 2>&1 | head -10
else
    echo "❌ Neither libcamera-hello nor rpicam-hello found"
fi

echo ""

# Check systemd service status
echo "🔍 Checking systemd service:"
if systemctl is-active --quiet wellmonitor; then
    echo "✅ wellmonitor service is running"
else
    echo "❌ wellmonitor service is not running"
fi

if systemctl is-enabled --quiet wellmonitor; then
    echo "✅ wellmonitor service is enabled"
else
    echo "❌ wellmonitor service is not enabled"
fi

echo ""
echo "🔍 Recent service logs (camera related):"
journalctl -u wellmonitor --since "5 minutes ago" | grep -i -E "(camera|permission|media|video)" | tail -5

echo ""
echo "=== Diagnostic Complete ==="
