# LED Camera Optimization PowerShell Script
# Updates Azure IoT Hub device twin with settings optimized for LED capture in low light

param(
    [Parameter(Mandatory=$true)]
    [string]$IoTHubName,
    
    [Parameter(Mandatory=$true)]
    [string]$DeviceId,
    
    [string]$SubscriptionName = $null
)

Write-Host "🔧 LED Camera Optimization - Device Twin Update" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host

# Set subscription if provided
if ($SubscriptionName) {
    Write-Host "Setting subscription: $SubscriptionName" -ForegroundColor Yellow
    az account set --subscription $SubscriptionName
}

Write-Host "Updating device twin with LED-optimized camera settings..." -ForegroundColor Yellow
Write-Host "IoT Hub: $IoTHubName" -ForegroundColor Cyan
Write-Host "Device: $DeviceId" -ForegroundColor Cyan
Write-Host

# LED-optimized camera settings for dark basement with red 7-segment displays
$cameraSettings = @{
    "cameraWidth" = 1280
    "cameraHeight" = 720
    "cameraQuality" = 85
    "cameraTimeoutMs" = 5000
    "cameraWarmupTimeMs" = 3000
    "cameraBrightness" = 70
    "cameraContrast" = 50
    "cameraSaturation" = 30
    "cameraGain" = 12.0
    "cameraShutterSpeedMicroseconds" = 50000
    "cameraAutoExposure" = $false
    "cameraAutoWhiteBalance" = $false
    "cameraDebugImagePath" = "/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"
}

# Debug settings to enable image saving
$debugSettings = @{
    "debugImageSaveEnabled" = $true
    "debugMode" = $true
}

Write-Host "Applying camera settings:" -ForegroundColor Yellow
foreach ($setting in $cameraSettings.GetEnumerator()) {
    Write-Host "  $($setting.Key): $($setting.Value)" -ForegroundColor White
}

Write-Host
Write-Host "Applying debug settings:" -ForegroundColor Yellow
foreach ($setting in $debugSettings.GetEnumerator()) {
    Write-Host "  $($setting.Key): $($setting.Value)" -ForegroundColor White
}

Write-Host
Write-Host "Updating device twin..." -ForegroundColor Yellow

try {
    # Ensure Azure CLI is available
    $azPath = "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
    if (-not (Test-Path $azPath)) {
        $azPath = "az" # Fallback to PATH
    }
    
    # Build the az command with all settings - use & operator for paths with spaces
    $azArgs = @(
        "iot", "hub", "device-twin", "update",
        "--hub-name", $IoTHubName,
        "--device-id", $DeviceId
    )
    
    # Add camera settings
    foreach ($setting in $cameraSettings.GetEnumerator()) {
        $azArgs += @("--set", "properties.desired.$($setting.Key)=$($setting.Value)")
    }
    
    # Add debug settings
    foreach ($setting in $debugSettings.GetEnumerator()) {
        $azArgs += @("--set", "properties.desired.$($setting.Key)=$($setting.Value)")
    }
    
    # Execute the command
    & $azPath @azArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Device twin updated successfully!" -ForegroundColor Green
        Write-Host
        Write-Host "LED Optimization Settings Applied:" -ForegroundColor Green
        Write-Host "• Resolution: 1280x720 (reduced for better performance)" -ForegroundColor White
        Write-Host "• Gain: 12.0 (high sensitivity for low light)" -ForegroundColor White
        Write-Host "• Shutter: 50ms (longer exposure for dark environments)" -ForegroundColor White
        Write-Host "• Brightness: +70% (enhanced for LED visibility)" -ForegroundColor White
        Write-Host "• Contrast: +50% (better LED-to-background separation)" -ForegroundColor White
        Write-Host "• Saturation: +30% (enhanced red LED visibility)" -ForegroundColor White
        Write-Host "• Auto Exposure: OFF (manual control for consistent LED capture)" -ForegroundColor White
        Write-Host "• Auto White Balance: OFF (consistent red LED color)" -ForegroundColor White
        Write-Host
        Write-Host "Expected improvements:" -ForegroundColor Cyan
        Write-Host "• Clearer red LED digits in dark environments" -ForegroundColor White
        Write-Host "• Reduced grey/fuzzy appearance" -ForegroundColor White
        Write-Host "• Better contrast for OCR processing" -ForegroundColor White
        Write-Host "• More stable image capture timing" -ForegroundColor White
        Write-Host
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "1. Wait 1-2 minutes for device to apply new settings" -ForegroundColor White
        Write-Host "2. Restart WellMonitor service: sudo systemctl restart wellmonitor" -ForegroundColor White
        Write-Host "3. Monitor debug images: ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/" -ForegroundColor White
        Write-Host "4. Compare new captures with previous ones" -ForegroundColor White
        
    } else {
        Write-Host "❌ Failed to update device twin" -ForegroundColor Red
        Write-Host "Please check Azure CLI authentication and IoT Hub permissions" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error updating device twin: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host
Write-Host "Physical Setup Recommendations:" -ForegroundColor Yellow
Write-Host "📏 Position camera 2-4 inches from LED display" -ForegroundColor White
Write-Host "📐 Ensure camera is perpendicular to display surface" -ForegroundColor White
Write-Host "💡 Consider adding small LED strip for supplemental lighting" -ForegroundColor White
Write-Host "🔧 Ensure display is always powered during monitoring" -ForegroundColor White
Write-Host "📱 Use stable mount to prevent camera movement/blur" -ForegroundColor White
