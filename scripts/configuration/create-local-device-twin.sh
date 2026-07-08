#!/bin/bash
# Bash version of local device twin creation for Linux users

DEVICE_NAME="${1:-LAPTOP-FBVH49A7}"
IOT_HUB_NAME="${2:-}"  # Will auto-detect from .env or use RTHIoTHub
RESOURCE_GROUP="${3:-your-resource-group}"  # Replace with your resource group

set -e

# Function to extract IoT Hub name from connection string or .env file
get_iot_hub_name() {
    local provided_name="$1"
    
    if [ -n "$provided_name" ]; then
        echo "$provided_name"
        return
    fi
    
    # Try to read from .env file
    local env_path="$(dirname "$0")/../../.env"
    if [ -f "$env_path" ]; then
        # Extract HostName from connection string
        local connection_string_line=$(grep -v "^#" "$env_path" | grep "WELLMONITOR_IOTHUB_CONNECTION_STRING" || true)
        if [ -n "$connection_string_line" ]; then
            # Extract hub name from HostName=YourHub.azure-devices.net
            local hub_name=$(echo "$connection_string_line" | sed -n 's/.*HostName=\([^.]*\)\.azure-devices\.net.*/\1/p')
            if [ -n "$hub_name" ]; then
                echo "$hub_name"
                return
            fi
        fi
        
        # Also check for explicit hub name variable
        local hub_name_line=$(grep -v "^#" "$env_path" | grep "WELLMONITOR_IOTHUB_NAME" || true)
        if [ -n "$hub_name_line" ]; then
            local hub_name=$(echo "$hub_name_line" | sed 's/.*=\s*["\x27]*\([^"\x27]*\)["\x27]*.*/\1/')
            if [ -n "$hub_name" ]; then
                echo "$hub_name"
                return
            fi
        fi
    fi
    
    # Default fallback
    echo "RTHIoTHub"
}

# Auto-detect IoT Hub name
IOT_HUB_NAME=$(get_iot_hub_name "$IOT_HUB_NAME")

# Display configuration detection results
if [ -f "$(dirname "$0")/../../.env" ] && [ "$IOT_HUB_NAME" = "RTHIoTHub" ]; then
    echo "📖 Reading configuration from .env file..."
    echo "✅ Found IoT Hub name in .env: $IOT_HUB_NAME"
elif [ "$IOT_HUB_NAME" = "RTHIoTHub" ]; then
    echo "⚠️  No IoT Hub name found in .env, using default: RTHIoTHub"
fi

echo "🔧 Creating Local Development Device Twin"
echo "========================================"
echo "Device: $DEVICE_NAME"
echo "IoT Hub: $IOT_HUB_NAME"
echo ""

# Check if device already exists
echo "📋 Checking if device already exists..."
if az iot hub device-identity show --device-id "$DEVICE_NAME" --hub-name "$IOT_HUB_NAME" >/dev/null 2>&1; then
    echo "✅ Device '$DEVICE_NAME' already exists"
else
    echo "📝 Creating new device identity..."
    if ! az iot hub device-identity create --device-id "$DEVICE_NAME" --hub-name "$IOT_HUB_NAME" --auth-method shared_private_key; then
        echo "❌ Failed to create device identity!"
        echo "💡 Make sure you're logged in to Azure CLI and have access to IoT Hub '$IOT_HUB_NAME'"
        echo "   Try: az login"
        echo "   Try: az account set --subscription $AZURE_SUBSCRIPTION_ID"
        exit 1
    fi
    echo "✅ Device identity created successfully"
fi

echo "🔧 Configuring device twin for local development..."

# Create device twin patch JSON in a temporary file
TEMP_JSON=$(mktemp)
cat > "$TEMP_JSON" << 'EOF'
{
    "currentThreshold": 2.0,
    "cycleTimeThreshold": 60,
    "relayDebounceMs": 1000,
    "syncIntervalMinutes": 10,
    "logRetentionDays": 7,
    "ocrMode": "tesseract",
    "powerAppEnabled": false,
    "debugMode": true,
    "debugImageSaveEnabled": true,
    "debugImagePath": "debug_images",
    "verboseLogging": true,
    "enableVerboseOcrLogging": true,
    "debugImageRetentionDays": 3,
    "logLevel": "Debug",
    "Camera": {
        "Width": 1920,
        "Height": 1080,
        "Quality": 85,
        "TimeoutMs": 8000,
        "WarmupTimeMs": 1000,
        "Rotation": 0,
        "Brightness": 50,
        "Contrast": 0,
        "Saturation": 0,
        "Gain": 1.0,
        "ShutterSpeedMicroseconds": 0,
        "AutoExposure": true,
        "AutoWhiteBalance": true,
        "EnablePreview": false,
        "DebugImagePath": "debug_images"
    },
    "OCR": {
        "Provider": "Tesseract",
        "MinimumConfidence": 0.6,
        "MaxRetryAttempts": 2,
        "TimeoutSeconds": 15,
        "EnablePreprocessing": true,
        "ImagePreprocessing": {
            "EnableScaling": true,
            "ScaleFactor": 2.0,
            "BinaryThreshold": 128,
            "ContrastFactor": 1.2,
            "BrightnessAdjustment": 5
        }
    },
    "monitoringIntervalSeconds": 60,
    "telemetryIntervalMinutes": 2,
    "syncIntervalHours": 1,
    "dataRetentionDays": 7,
    "webPort": 5000,
    "webAllowNetworkAccess": true,
    "webBindAddress": "0.0.0.0",
    "webEnableHttps": false,
    "webEnableAuthentication": false,
    "alertDryCountThreshold": 5,
    "alertRcycCountThreshold": 3,
    "alertMaxRetryAttempts": 3,
    "alertCooldownMinutes": 5,
    "imageQualityMinThreshold": 0.5,
    "imageQualityBrightnessMin": 20,
    "imageQualityBrightnessMax": 200,
    "imageQualityContrastMin": 0.3,
    "imageQualityNoiseMax": 0.8,
    "environment": "development"
}
EOF

echo "📝 Updating device twin with local development configuration..."
if az iot hub device-twin update --device-id "$DEVICE_NAME" --hub-name "$IOT_HUB_NAME" --set properties.desired=@"$TEMP_JSON"; then
    echo "✅ Local development device twin configured successfully!"
    echo ""
    echo "🔍 Local Development Configuration:"
    echo "  • Device Name: LAPTOP-FBVH49A7"
    echo "  • Debug Mode: ENABLED"
    echo "  • Debug Images: ENABLED (debug_images/)"
    echo "  • OCR Provider: Tesseract (offline)"
    echo "  • Camera: Auto exposure/white balance"
    echo "  • Monitoring: Every 60 seconds"
    echo "  • Web Dashboard: Port 5000, no auth"
    echo "  • Logging: Debug level with verbose OCR"
    echo ""
    echo "🔑 Next Steps:"
    echo "1. Get connection string: az iot hub device-identity connection-string show --device-id $DEVICE_NAME --hub-name $IOT_HUB_NAME"
    echo "2. Add to your .env file: WELLMONITOR_DEVICE_CONNECTION_STRING=..."
    echo "3. Or set environment variable: export WELLMONITOR_DEVICE_CONNECTION_STRING=..."
    echo "4. Run: dotnet run"
    echo "5. Check logs for device twin sync messages"
else
    echo "❌ Failed to configure device twin!"
    exit 1
fi

# Cleanup
rm -f "$TEMP_JSON"

echo ""
echo "🎉 Local development device twin setup complete!"
