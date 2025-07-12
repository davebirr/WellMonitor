#!/usr/bin/env pwsh
# Force Device Twin Update with Correct Settings

param(
    [string]$DeviceId = "rpi4b-1407well01",
    [string]$HubName = "RTHIoTHub"
)

Write-Host "🔧 Forcing Device Twin Update with Fixed Settings" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Create the corrected device twin patch
$deviceTwinPatch = @{
    properties = @{
        desired = @{
            # Camera settings for red LEDs (reduced sensitivity)
            cameraGain = 4
            cameraShutterSpeedMicroseconds = 10000
            cameraBrightness = 50
            cameraContrast = 40
            cameraSaturation = 20
            
            # Correct debug path for secure installation
            cameraDebugImagePath = "/var/lib/wellmonitor/debug_images"
            
            # Ensure debug images are enabled
            debugImageSaveEnabled = $true
            debugMode = $true
            
            # IoT Hub connectivity settings
            telemetryIntervalMinutes = 5
            syncIntervalMinutes = 5
        }
    }
} | ConvertTo-Json -Depth 10

Write-Host "📝 Updating device twin with corrected settings..." -ForegroundColor Yellow
Write-Host "• Gain: 12 → 4 (reduced sensitivity)" -ForegroundColor White
Write-Host "• Shutter: 50ms → 10ms (shorter exposure)" -ForegroundColor White
Write-Host "• Brightness: 70 → 50 (reduced)" -ForegroundColor White
Write-Host "• Debug Path: → /var/lib/wellmonitor/debug_images" -ForegroundColor White

# Apply the device twin update
try {
    $result = az iot hub device-twin update --device-id $DeviceId --hub-name $HubName --set "properties.desired.cameraGain=4" "properties.desired.cameraShutterSpeedMicroseconds=10000" "properties.desired.cameraBrightness=50" "properties.desired.cameraContrast=40" "properties.desired.cameraSaturation=20" "properties.desired.cameraDebugImagePath='/var/lib/wellmonitor/debug_images'"
    
    if ($result) {
        Write-Host "✅ Device twin updated successfully" -ForegroundColor Green
    }
} catch {
    Write-Error "❌ Failed to update device twin: $_"
    exit 1
}

# Check if device is online now
Write-Host "`n🔍 Checking device connection status..." -ForegroundColor Yellow
$deviceStatus = az iot hub device-identity show --device-id $DeviceId --hub-name $HubName | ConvertFrom-Json

Write-Host "📊 Device Status:" -ForegroundColor Cyan
Write-Host "• Connection State: $($deviceStatus.connectionState)" -ForegroundColor White
Write-Host "• Status: $($deviceStatus.status)" -ForegroundColor White

if ($deviceStatus.connectionState -eq "Connected") {
    Write-Host "✅ Device is now CONNECTED - updates should be received!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Device is still DISCONNECTED" -ForegroundColor Yellow
    Write-Host "💡 Check these on the Pi:" -ForegroundColor Cyan
    Write-Host "   1. Internet connectivity: ping 8.8.8.8" -ForegroundColor White
    Write-Host "   2. Service status: sudo systemctl status wellmonitor" -ForegroundColor White
    Write-Host "   3. Service logs: sudo journalctl -u wellmonitor --since '5 minutes ago'" -ForegroundColor White
    Write-Host "   4. Azure IoT connection string in secrets.json" -ForegroundColor White
}

Write-Host "`n📋 Next Steps:" -ForegroundColor Cyan
Write-Host "1. On Pi: sudo systemctl restart wellmonitor" -ForegroundColor White
Write-Host "2. Wait 2-3 minutes for device to connect and sync" -ForegroundColor White
Write-Host "3. Check debug images: ls -la /var/lib/wellmonitor/debug_images/" -ForegroundColor White
Write-Host "4. New images should show red LEDs clearly (not all white)" -ForegroundColor White
