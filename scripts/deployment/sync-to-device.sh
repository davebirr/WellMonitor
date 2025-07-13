#!/bin/bash
# sync-to-device.sh - Deploy latest changes to Raspberry Pi

set -e

DEVICE_USER="davidb"
DEVICE_HOST="192.168.1.48"  # Update with your Pi's IP
REMOTE_PATH="/home/davidb/wellmonitor"
BUILD_CONFIG="Release"

echo "🚀 Syncing WellMonitor changes to Raspberry Pi..."

# Check if we can reach the device
echo "📡 Checking device connectivity..."
if ! ping -c 1 "$DEVICE_HOST" >/dev/null 2>&1; then
    echo "❌ Cannot reach device at $DEVICE_HOST"
    echo "💡 Please check:"
    echo "   • Device is powered on and connected to network"
    echo "   • IP address is correct"
    echo "   • SSH is enabled on the device"
    exit 1
fi

echo "✅ Device is reachable"

# Pull latest changes on the device
echo "📥 Pulling latest changes on device..."
ssh "$DEVICE_USER@$DEVICE_HOST" "cd $REMOTE_PATH && git pull origin main"

# Build the project on the device
echo "🔨 Building project on device..."
ssh "$DEVICE_USER@$DEVICE_HOST" "cd $REMOTE_PATH && dotnet build -c $BUILD_CONFIG"

# Restart the service to pick up changes
echo "🔄 Restarting WellMonitor service..."
ssh "$DEVICE_USER@$DEVICE_HOST" "sudo systemctl restart wellmonitor.service"

# Check service status
echo "🔍 Checking service status..."
ssh "$DEVICE_USER@$DEVICE_HOST" "sudo systemctl status wellmonitor.service --no-pager -l"

echo ""
echo "✅ Deployment complete!"
echo ""
echo "🔍 To monitor the service:"
echo "   ssh $DEVICE_USER@$DEVICE_HOST"
echo "   sudo journalctl -u wellmonitor.service -f"
echo ""
echo "🖼️  To check debug images:"
echo "   sudo ls -la /var/lib/wellmonitor/debug_images/"
echo ""
echo "📊 To check device twin sync:"
echo "   ./scripts/diagnostics/troubleshoot-device-twin-sync.sh"
