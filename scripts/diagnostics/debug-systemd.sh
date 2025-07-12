#!/bin/bash

echo "=== Checking systemd service configuration ==="

echo "1. Current service file:"
cat /etc/systemd/system/wellmonitor.service

echo ""
echo "2. Check if systemd can see the file:"
sudo systemd-analyze verify /etc/systemd/system/wellmonitor.service

echo ""
echo "3. Check service status:"
sudo systemctl status wellmonitor --no-pager

echo ""
echo "4. Test with systemd-run to see if systemd can execute it:"
sudo systemd-run --uid=davidb --gid=davidb --working-directory=/home/davidb/WellMonitor/src/WellMonitor.Device /home/davidb/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/WellMonitor.Device --version

echo ""
echo "5. Check if the path is accessible to systemd:"
sudo test -x /home/davidb/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/WellMonitor.Device && echo "File is executable by root" || echo "File not executable by root"

echo ""
echo "6. Check directory permissions from root to the executable:"
sudo ls -la /home/davidb/
sudo ls -la /home/davidb/WellMonitor/
sudo ls -la /home/davidb/WellMonitor/src/
sudo ls -la /home/davidb/WellMonitor/src/WellMonitor.Device/
sudo ls -la /home/davidb/WellMonitor/src/WellMonitor.Device/bin/
sudo ls -la /home/davidb/WellMonitor/src/WellMonitor.Device/bin/Release/
sudo ls -la /home/davidb/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/

echo ""
echo "7. Try running with full environment simulation:"
sudo runuser -l davidb -c 'cd /home/davidb/WellMonitor/src/WellMonitor.Device && ./bin/Release/net8.0/linux-arm64/WellMonitor.Device --version'
