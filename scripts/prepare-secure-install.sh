#!/bin/bash

echo "=== Preparing for Secure WellMonitor Installation ==="

echo "Checking current service status..."
sudo systemctl status wellmonitor --no-pager -l || echo "Service not found"

echo ""
echo "Stopping wellmonitor service..."
sudo systemctl stop wellmonitor 2>/dev/null && echo "✅ Service stopped" || echo "ℹ️  Service was not running"

echo ""
echo "Disabling wellmonitor service..."
sudo systemctl disable wellmonitor 2>/dev/null && echo "✅ Service disabled" || echo "ℹ️  Service was not enabled"

echo ""
echo "Backing up current service file..."
if [ -f /etc/systemd/system/wellmonitor.service ]; then
    sudo cp /etc/systemd/system/wellmonitor.service /etc/systemd/system/wellmonitor.service.backup.$(date +%Y%m%d_%H%M%S)
    echo "✅ Service file backed up"
else
    echo "ℹ️  No existing service file found"
fi

echo ""
echo "Checking for running WellMonitor processes..."
pgrep -f WellMonitor.Device && echo "⚠️  WellMonitor processes still running - killing them..." && pkill -f WellMonitor.Device || echo "✅ No WellMonitor processes running"

echo ""
echo "Current database and debug images locations:"
echo "Database: $(find ~/WellMonitor -name 'wellmonitor.db*' 2>/dev/null | head -5)"
echo "Debug images: $(find ~/WellMonitor -name 'debug_images' -type d 2>/dev/null)"

echo ""
echo "=== Preparation Complete ==="
echo "You can now safely run: ./scripts/install-wellmonitor-secure.sh"
echo ""
echo "The secure installation will:"
echo "✅ Move application to /opt/wellmonitor/"
echo "✅ Move database to /var/lib/wellmonitor/"
echo "✅ Move debug images to /var/lib/wellmonitor/debug_images/"
echo "✅ Create secure configuration in /etc/wellmonitor/"
echo "✅ Enable full systemd security protections"
