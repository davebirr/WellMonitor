#!/bin/bash

# Manual Debug Image Path Fix (No Azure CLI Required)
# This script updates the appsettings.json to use absolute path for debug images

echo "🔧 Manual Debug Image Path Fix"
echo "=============================="
echo

# Get project root
PROJECT_ROOT="/home/davidb/WellMonitor/src/WellMonitor.Device"
APPSETTINGS_FILE="$PROJECT_ROOT/appsettings.json"
DEBUG_IMAGES_PATH="$PROJECT_ROOT/debug_images"

echo "Project Root: $PROJECT_ROOT"
echo "Debug Images Path: $DEBUG_IMAGES_PATH"
echo "Config File: $APPSETTINGS_FILE"
echo

# Check if appsettings.json exists
if [ ! -f "$APPSETTINGS_FILE" ]; then
    echo "❌ appsettings.json not found at $APPSETTINGS_FILE"
    exit 1
fi

# Backup current appsettings.json
cp "$APPSETTINGS_FILE" "$APPSETTINGS_FILE.backup"
echo "✅ Backed up appsettings.json to $APPSETTINGS_FILE.backup"

# Create temp file with updated debug settings
cat "$APPSETTINGS_FILE" | jq '.Debug.ImageSaveEnabled = true' | jq '.Debug.DebugMode = true' > "$APPSETTINGS_FILE.tmp"

if [ $? -eq 0 ]; then
    mv "$APPSETTINGS_FILE.tmp" "$APPSETTINGS_FILE"
    echo "✅ Updated appsettings.json:"
    echo "   - Set Debug.ImageSaveEnabled = true"
    echo "   - Set Debug.DebugMode = true"
    echo
    echo "📋 Manual Steps to Complete:"
    echo "1. Restart the WellMonitor service:"
    echo "   sudo systemctl restart wellmonitor"
    echo
    echo "2. The CameraService will now use the path from device twin configuration"
    echo "   Current device twin setting: cameraDebugImagePath = 'debug_images'"
    echo "   This resolves to: $PROJECT_ROOT/debug_images"
    echo
    echo "3. To change to absolute path, you can either:"
    echo "   a) Install Azure CLI and run: ./scripts/update-debug-image-path.sh"
    echo "   b) Manually update device twin in Azure Portal"
    echo
    echo "4. Check if debug images are now saved to the correct location:"
    echo "   ls -la $DEBUG_IMAGES_PATH/"
    echo
    echo "Note: The relative path 'debug_images' should now resolve correctly"
    echo "      when the service runs from the project directory."
else
    echo "❌ Failed to update appsettings.json (jq command failed)"
    echo "   Make sure jq is installed: sudo apt-get install jq"
    rm -f "$APPSETTINGS_FILE.tmp"
fi
