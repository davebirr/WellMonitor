# Fix overexposed camera settings for red LED display
param(
    [Parameter(Mandatory=$false)]
    [string]$IoTHubName = "RTHIoTHub",
    
    [Parameter(Mandatory=$false)]
    [string]$DeviceId = "rpi4b-1407well01"
)

Write-Host "🔧 Fixing Overexposed Camera Settings for Red LEDs" -ForegroundColor Blue
Write-Host "=================================================" -ForegroundColor Blue

# Reduced sensitivity settings for red LEDs in dark room
$patch = @{
    properties = @{
        desired = @{
            # Reduce gain significantly (was 12.0, now 4.0)
            cameraGain = 4.0
            
            # Reduce shutter speed significantly (was 50000, now 10000 = 10ms)
            cameraShutterSpeedMicroseconds = 10000
            
            # Keep manual exposure and white balance off for consistency
            cameraAutoExposure = $false
            cameraAutoWhiteBalance = $false
            
            # Moderate brightness/contrast for red LEDs
            cameraBrightness = 50
            cameraContrast = 40
            cameraSaturation = 20
            
            # Ensure debug images are enabled
            debugImageSaveEnabled = $true
            debugMode = $true
        }
    }
} | ConvertTo-Json -Depth 5

Write-Host "📝 Applying reduced sensitivity settings:" -ForegroundColor Yellow
Write-Host "   • Gain: 12.0 → 4.0 (much less sensitive)" -ForegroundColor White
Write-Host "   • Shutter: 50ms → 10ms (shorter exposure)" -ForegroundColor White
Write-Host "   • Brightness: 70 → 50 (reduced)" -ForegroundColor White
Write-Host "   • Contrast: 50 → 40 (reduced)" -ForegroundColor White
Write-Host "   • Saturation: 30 → 20 (reduced)" -ForegroundColor White

try {
    # Update the device twin
    az iot hub device-twin update --hub-name $IoTHubName --device-id $DeviceId --set $patch
    
    Write-Host "✅ Camera settings updated successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next Steps:" -ForegroundColor Blue
    Write-Host "1. Wait 1-2 minutes for device to receive the update" -ForegroundColor White
    Write-Host "2. Restart service: sudo systemctl restart wellmonitor" -ForegroundColor White
    Write-Host "3. Wait for new images: ls -la /var/lib/wellmonitor/debug_images/" -ForegroundColor White
    Write-Host "4. If still overexposed, we can reduce gain further to 2.0 or 1.0" -ForegroundColor White
    Write-Host ""
    Write-Host "🎯 Goal: Images should show red LED digits clearly without being all white" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to update device twin: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
