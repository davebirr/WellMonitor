#!/bin/bash
#
# deploy-improved-logging.sh
# Deploy enhanced device twin configuration logging to Raspberry Pi
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PI_HOST="pi@raspberrypi.local"
WELLMONITOR_SERVICE="wellmonitor"

echo "🚀 Deploying Enhanced Configuration Logging"
echo "============================================="

# Build the project first
echo "📦 Building WellMonitor.Device project..."
cd "$SCRIPT_DIR/../.."
dotnet build src/WellMonitor.Device/WellMonitor.Device.csproj -c Release -r linux-arm64

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful"

# Stop the service
echo "⏸️ Stopping WellMonitor service on Raspberry Pi..."
ssh $PI_HOST "sudo systemctl stop $WELLMONITOR_SERVICE" || true

# Copy the updated binaries
echo "📋 Copying updated binaries to Raspberry Pi..."
scp -r src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/* $PI_HOST:/opt/wellmonitor/

# Set correct permissions
echo "🔒 Setting correct permissions..."
ssh $PI_HOST "sudo chown -R wellmonitor:wellmonitor /opt/wellmonitor/"
ssh $PI_HOST "sudo chmod +x /opt/wellmonitor/WellMonitor.Device"

# Start the service
echo "▶️ Starting WellMonitor service..."
ssh $PI_HOST "sudo systemctl start $WELLMONITOR_SERVICE"

# Wait a moment for startup
sleep 3

# Check service status
echo "📊 Checking service status..."
ssh $PI_HOST "sudo systemctl status $WELLMONITOR_SERVICE --no-pager -l"

echo ""
echo "📋 Recent logs with enhanced configuration logging:"
ssh $PI_HOST "sudo journalctl -u $WELLMONITOR_SERVICE --since '1 minute ago' --no-pager"

echo ""
echo "✅ Enhanced configuration logging deployed successfully!"
echo ""
echo "🔍 Key improvements:"
echo "   • Detailed camera configuration logging with device twin vs default tracking"
echo "   • Nested Camera property support (Camera.Gain, Camera.ShutterSpeedMicroseconds, etc.)"
echo "   • Backward compatibility with legacy flat properties (cameraGain, etc.)"
echo "   • Warnings for default values not found in device twin"
echo "   • Warnings for potentially problematic camera settings"
echo "   • Hourly configuration summary reports"
echo "   • Enhanced device twin version tracking"
echo ""
echo "📊 To monitor configuration logs in real-time:"
echo "   ssh $PI_HOST \"sudo journalctl -u $WELLMONITOR_SERVICE -f\""
echo ""
echo "🔧 To trigger an immediate configuration update:"
echo "   Update the device twin in Azure IoT Hub - changes will be logged on next cycle"
