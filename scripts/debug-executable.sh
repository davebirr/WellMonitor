#!/bin/bash

echo "=== Checking WellMonitor executable ==="
cd ~/WellMonitor/src/WellMonitor.Device

echo "1. Directory contents:"
ls -la bin/Release/net8.0/linux-arm64/

echo ""
echo "2. File details:"
file bin/Release/net8.0/linux-arm64/WellMonitor.Device

echo ""
echo "3. Permissions:"
ls -la bin/Release/net8.0/linux-arm64/WellMonitor.Device

echo ""
echo "4. Check if it's actually executable:"
ldd bin/Release/net8.0/linux-arm64/WellMonitor.Device 2>&1 | head -10

echo ""
echo "5. Test direct execution:"
./bin/Release/net8.0/linux-arm64/WellMonitor.Device --version 2>&1 | head -5

echo ""
echo "6. Check architecture:"
readelf -h bin/Release/net8.0/linux-arm64/WellMonitor.Device | grep Machine

echo ""
echo "7. Check dynamic linker:"
readelf -l bin/Release/net8.0/linux-arm64/WellMonitor.Device | grep interpreter

echo ""
echo "8. System architecture:"
uname -m

echo ""
echo "9. Available interpreters:"
ls -la /lib/ld-linux-*

echo ""
echo "10. Check if we can run it with strace for more details:"
echo "Run this manually: strace -e trace=execve ./bin/Release/net8.0/linux-arm64/WellMonitor.Device"
