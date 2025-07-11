#!/bin/bash

# Device Twin Configuration Validation Script
# This script demonstrates how to validate device twin configuration with your Azure IoT Hub

echo "=== Device Twin Configuration Validation ==="
echo "Device ID: rpi4b-1407well01"
echo "Current working directory: $(pwd)"
echo ""

# Check if we're in the right directory
if [ ! -f "WellMonitor.sln" ]; then
    echo "❌ Error: Please run this script from the repository root directory"
    exit 1
fi

# Check if secrets.json exists
if [ ! -f "src/WellMonitor.Device/secrets.json" ]; then
    echo "❌ Error: secrets.json not found. Please create it with your Azure IoT Hub connection string"
    echo "Expected location: src/WellMonitor.Device/secrets.json"
    echo "Expected format:"
    echo "{"
    echo "  \"IotHubConnectionString\": \"HostName=your-hub.azure-devices.net;DeviceId=rpi4b-1407well01;SharedAccessKey=your-key\""
    echo "}"
    exit 1
fi

echo "✅ Found secrets.json"

# Run the integration tests specifically
echo ""
echo "=== Running Device Twin Integration Tests ==="
echo "These tests connect to your real Azure IoT Hub and validate the device twin configuration"
echo ""

dotnet test tests/WellMonitor.Device.Tests/ \
    --filter "DeviceTwinCameraConfigurationIntegrationTests" \
    --logger console \
    --verbosity normal

# Check if tests passed
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Integration tests passed successfully!"
    echo ""
    echo "=== Device Twin Configuration Validation Results ==="
    echo "✅ Camera configuration loaded and validated from Azure IoT Hub"
    echo "✅ Well monitor configuration loaded and validated from Azure IoT Hub"
    echo "✅ Input validation working correctly"
    echo "✅ All property ranges validated"
    echo ""
    echo "Your device twin configuration is ready for deployment!"
    echo ""
    echo "Expected camera settings from your device twin:"
    echo "  - Width: 1920px"
    echo "  - Height: 1080px"
    echo "  - Quality: 95%"
    echo "  - Brightness: 50"
    echo "  - Contrast: 10"
    echo "  - Rotation: 0°"
    echo "  - Timeout: 5000ms"
    echo "  - Warmup: 2000ms"
    echo "  - Saturation: 0"
    echo "  - Preview: disabled"
    echo "  - Debug Path: /home/pi/wellmonitor/debug_images"
    echo ""
    echo "Expected well monitor settings from your device twin:"
    echo "  - Current Threshold: 4.5A"
    echo "  - Cycle Time Threshold: 30s"
    echo "  - Relay Debounce: 500ms"
    echo "  - Sync Interval: 5min"
    echo "  - Log Retention: 14 days"
    echo "  - OCR Mode: tesseract"
    echo "  - PowerApp: enabled"
    echo ""
    echo "Next steps:"
    echo "1. Deploy to your Raspberry Pi using: dotnet publish -c Release -r linux-arm64"
    echo "2. Test camera capture with the configured settings"
    echo "3. Verify device twin updates are applied in real-time"
    echo ""
else
    echo ""
    echo "❌ Integration tests failed"
    echo ""
    echo "This could mean:"
    echo "1. Your connection string is invalid"
    echo "2. Your device twin doesn't have the expected properties"
    echo "3. Network connectivity issues"
    echo ""
    echo "Please verify:"
    echo "1. Your Azure IoT Hub connection string is correct"
    echo "2. Device 'rpi4b-1407well01' exists in your Azure IoT Hub"
    echo "3. The device twin has the expected properties set"
    echo ""
    echo "You can check your device twin using Azure CLI:"
    echo "az iot hub device-twin show --device-id rpi4b-1407well01 --hub-name your-hub-name"
fi
