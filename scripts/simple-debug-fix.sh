#!/bin/bash

# Simple Debug Image Path Fix
# Updates the service configuration to enable debug image saving

echo "🔧 Simple Debug Image Path Fix"
echo "============================="
echo

PROJECT_ROOT="/home/davidb/WellMonitor/src/WellMonitor.Device"
DEBUG_DIR="$PROJECT_ROOT/debug_images"

echo "Current situation:"
echo "  Debug images are saved to: bin/Release/net8.0/debug_images"
echo "  We want them saved to: $DEBUG_DIR"
echo

# Ensure debug directory exists
mkdir -p "$DEBUG_DIR"
echo "✅ Ensured debug directory exists: $DEBUG_DIR"

echo
echo "📋 To fix the debug image path, you have these options:"
echo
echo "Option 1: Install Azure CLI and update device twin (Recommended)"
echo "  curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash"
echo "  ./scripts/update-debug-image-path.sh"
echo
echo "Option 2: Use Azure Portal"
echo "  1. Go to Azure Portal > IoT Hub > Devices > rpi4b-1407well01"
echo "  2. Click 'Device Twin'"
echo "  3. Find 'cameraDebugImagePath': 'debug_images'"
echo "  4. Change it to: 'cameraDebugImagePath': '$DEBUG_DIR'"
echo "  5. Click 'Save'"
echo
echo "Option 3: Live with current location"
echo "  Debug images will continue to save in bin/Release/net8.0/debug_images"
echo "  This works fine for debugging, just a different location"
echo
echo "Current debug images in bin directory:"
ls -la "$PROJECT_ROOT/bin/Release/net8.0/debug_images/" 2>/dev/null || echo "  No debug images found"
echo
echo "To restart the service after any changes:"
echo "  sudo systemctl restart wellmonitor"
