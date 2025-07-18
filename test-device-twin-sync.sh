#!/bin/bash
# Test script to verify device twin configuration sync

echo "🔍 Testing Device Twin Configuration Sync"
echo "========================================"

# Check if WellMonitor is running
PID=$(pgrep -f "WellMonitor")
if [ -z "$PID" ]; then
    echo "❌ WellMonitor is not running"
    exit 1
fi

echo "✅ WellMonitor is running (PID: $PID)"
echo ""

# Check device twin values
echo "📋 Device Twin Configuration:"
echo "  debugImageSaveEnabled: $(az iot hub device-twin show --device-id LAPTOP-FBVH49A7 --hub-name RTHIoTHub --query "properties.desired.debugImageSaveEnabled" -o tsv)"
echo "  Camera.DebugImagePath: $(az iot hub device-twin show --device-id LAPTOP-FBVH49A7 --hub-name RTHIoTHub --query "properties.desired.Camera.DebugImagePath" -o tsv)"
echo ""

# Check if debug_images directory exists
echo "📁 Debug Directory Status:"
if [ -d "/home/davidb/WellMonitor/debug_images" ]; then
    echo "  ✅ debug_images directory exists"
    echo "  📊 Files in debug_images: $(ls -1 /home/davidb/WellMonitor/debug_images 2>/dev/null | wc -l)"
else
    echo "  ❌ debug_images directory does not exist"
fi
echo ""

# Try to trigger a camera capture by checking the web endpoint
echo "🌐 Testing Web Dashboard:"
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000)
if [ "$HTTP_STATUS" = "200" ]; then
    echo "  ✅ Web dashboard is accessible (HTTP $HTTP_STATUS)"
else
    echo "  ❌ Web dashboard is not accessible (HTTP $HTTP_STATUS)"
fi
echo ""

echo "⏰ Monitoring will occur every 60 seconds..."
echo "💡 To see live logs, run: journalctl -f | grep -E '(Debug image check|ImageSaveEnabled|DebugImagePath)'"
echo ""
echo "🎯 Expected Result After Fix:"
echo "  Should see: Debug image check: ImageSaveEnabled=True, DebugImagePath='debug_images'"
echo "  NOT:        Debug image check: ImageSaveEnabled=False, DebugImagePath='NULL'"
