#!/bin/bash

# Update Azure IoT Hub device twin with correct debug image path for Pi deployment
# This script updates the device twin to use the correct absolute path for debug images
# on the Raspberry Pi: /home/davidb/WellMonitor/src/WellMonitor.Device/debug_images

# Default values
DEVICE_NAME="${1:-rpi4b-1407well01}"
IOT_HUB_NAME="${2:-WellMonitorIoTHub-dev}"
RESOURCE_GROUP="${3:-WellMonitor-dev}"

echo "🔧 Updating Debug Image Path Configuration"
echo "=========================================="
echo "Device: $DEVICE_NAME"
echo "IoT Hub: $IOT_HUB_NAME"
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI is not installed!"
    echo "Please install Azure CLI first:"
    echo "curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash"
    exit 1
fi

# Check if logged in to Azure CLI
if ! az account show &> /dev/null; then
    echo "❌ Not logged in to Azure CLI!"
    echo "Please run: az login"
    exit 1
fi

echo "📝 Updating device twin with absolute debug image path..."

# Create the device twin update JSON
DEVICE_TWIN_UPDATE='{
    "properties": {
        "desired": {
            "debugImageSaveEnabled": true,
            "cameraDebugImagePath": "/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"
        }
    }
}'

echo "Device twin update payload:"
echo "$DEVICE_TWIN_UPDATE"
echo ""

# Update device twin using Azure CLI
if az iot hub device-twin update \
    --device-id "$DEVICE_NAME" \
    --hub-name "$IOT_HUB_NAME" \
    --set "$DEVICE_TWIN_UPDATE"; then
    
    echo "✅ Device twin updated successfully!"
    echo ""
    echo "Debug Image Configuration:"
    echo "- debugImageSaveEnabled: true"
    echo "- cameraDebugImagePath: /home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"
    echo ""
    echo "The device will pick up these changes automatically."
    echo "Debug images will now be saved to the correct location."
else
    echo "❌ Failed to update device twin!"
    echo "Please check:"
    echo "1. Device name is correct: $DEVICE_NAME"
    echo "2. IoT Hub name is correct: $IOT_HUB_NAME"
    echo "3. You have permission to modify the device twin"
    exit 1
fi

echo ""
echo "🎉 Debug image path configuration completed!"
