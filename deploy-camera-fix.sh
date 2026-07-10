#!/bin/bash

# Quick deployment script to sync and apply the camera fix to the Pi
# Run this from your development machine

PI_HOST="davidb@rpi4b-1407well01"
PROJECT_DIR="/home/davidb/WellMonitor"

echo "🚀 Deploying Camera Exposure Fix to Production Pi"
echo "================================================="
echo ""

# Sync the latest code to the Pi
echo "📦 Syncing code to Pi..."
rsync -avz --exclude='bin/' --exclude='obj/' --exclude='debug_images/' \
    "$PROJECT_DIR/" "$PI_HOST:~/WellMonitor/"

echo ""
echo "🔧 Applying fix on Pi..."

# Execute the fix script on the Pi
ssh "$PI_HOST" "cd ~/WellMonitor && ./scripts/fixes/fix-camera-exposure.sh"

echo ""
echo "📋 Verification steps (run on Pi):"
echo "1. Monitor service logs:"
echo "   ssh $PI_HOST"
echo "   sudo journalctl -u wellmonitor -f"
echo ""
echo "2. Look for success indicators:"
echo "   ✅ 'using barcode exposure mode for LED displays'"
echo "   ✅ 'using normal exposure mode'"
echo "   ❌ Should NOT see: 'Invalid exposure mode:off'"
echo ""
echo "3. Test manual camera capture:"
echo "   ssh $PI_HOST"
echo "   libcamera-still -o test.jpg --exposure barcode --timeout 2000"
echo ""
echo "✅ Deployment complete! The camera should now work without exposure errors."
