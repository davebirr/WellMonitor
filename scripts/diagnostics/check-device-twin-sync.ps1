#!/usr/bin/env pwsh
# Check Device Twin Sync Status and Force Update

param(
    [string]$DeviceId = "rpi4b-1407well01",
    [string]$HubName = "WellMonitorHub"
)

Write-Host "🔍 Checking Device Twin Sync Status" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Get current device twin
Write-Host "📱 Getting current device twin..." -ForegroundColor Yellow
$deviceTwin = az iot hub device-twin show --device-id $DeviceId --hub-name $HubName | ConvertFrom-Json

if (-not $deviceTwin) {
    Write-Error "❌ Failed to get device twin"
    exit 1
}

Write-Host "✅ Device twin retrieved" -ForegroundColor Green

# Check key settings
$desired = $deviceTwin.properties.desired
$reported = $deviceTwin.properties.reported

Write-Host "`n📋 Key Camera Settings (Desired):" -ForegroundColor Cyan
Write-Host "• Gain: $($desired.cameraGain)" -ForegroundColor White
Write-Host "• Shutter: $($desired.cameraShutterSpeedMicroseconds)μs" -ForegroundColor White
Write-Host "• Brightness: $($desired.cameraBrightness)" -ForegroundColor White
Write-Host "• Debug Path: $($desired.cameraDebugImagePath)" -ForegroundColor White
Write-Host "• Version: $($desired.'$version')" -ForegroundColor White

Write-Host "`n📊 Device Twin Status:" -ForegroundColor Cyan
Write-Host "• Connection State: $($deviceTwin.connectionState)" -ForegroundColor White
Write-Host "• Last Activity: $($deviceTwin.lastActivityTime)" -ForegroundColor White
Write-Host "• Desired Version: $($desired.'$version')" -ForegroundColor White
Write-Host "• Reported Version: $($reported.'$version')" -ForegroundColor White

# Check if device is receiving updates
if ($deviceTwin.connectionState -eq "Disconnected") {
    Write-Host "⚠️  Device is DISCONNECTED - it won't receive updates!" -ForegroundColor Red
    Write-Host "💡 The device needs to be online to receive device twin changes" -ForegroundColor Yellow
} elseif ($desired.'$version' -gt $reported.'$version') {
    Write-Host "⏳ Device twin update PENDING - device hasn't acknowledged the changes yet" -ForegroundColor Yellow
    Write-Host "💡 Wait a few minutes or restart the service: sudo systemctl restart wellmonitor" -ForegroundColor Yellow
} else {
    Write-Host "✅ Device twin is synchronized" -ForegroundColor Green
}

# Force a device twin update by sending a direct method
Write-Host "`n🔄 Sending direct method to force configuration refresh..." -ForegroundColor Yellow
try {
    $result = az iot hub invoke-device-method --device-id $DeviceId --hub-name $HubName --method-name "RefreshConfiguration" --method-payload '{}' --timeout 30 2>$null
    if ($result) {
        Write-Host "✅ Direct method sent successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Direct method failed (device may not support it)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Direct method not available" -ForegroundColor Yellow
}

Write-Host "`n📝 Recommendations:" -ForegroundColor Cyan
Write-Host "1. Restart service: sudo systemctl restart wellmonitor" -ForegroundColor White
Write-Host "2. Check logs: sudo journalctl -u wellmonitor -f" -ForegroundColor White
Write-Host "3. Verify new debug images use updated path and settings" -ForegroundColor White
Write-Host "4. Images should be in: /var/lib/wellmonitor/debug_images/" -ForegroundColor White
