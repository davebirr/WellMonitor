# LED Camera Optimization Test Plan
# Complete workflow for optimizing camera settings for red LED displays

param(
    [Parameter(Mandatory=$true)]
    [string]$IoTHubName,
    
    [Parameter(Mandatory=$true)]
    [string]$DeviceId,
    
    [string]$SubscriptionName = $null,
    
    [switch]$RunDiagnosticFirst = $false
)

Write-Host "🎯 LED Camera Optimization Test Plan" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host

Write-Host "This test plan will:" -ForegroundColor Yellow
Write-Host "1. Take baseline captures with current settings" -ForegroundColor White
Write-Host "2. Update device twin with LED-optimized settings" -ForegroundColor White
Write-Host "3. Wait for device to apply new configuration" -ForegroundColor White
Write-Host "4. Take test captures with new settings" -ForegroundColor White
Write-Host "5. Compare results and provide recommendations" -ForegroundColor White
Write-Host

if ($RunDiagnosticFirst) {
    Write-Host "📋 Step 1: Running baseline diagnostic..." -ForegroundColor Cyan
    Write-Host "Taking current captures to compare against optimized settings" -ForegroundColor Yellow
    
    # Create a baseline directory
    $baselineDir = "debug_images/baseline_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Write-Host "Baseline images will be saved to: $baselineDir" -ForegroundColor White
    
    Write-Host
    Write-Host "Run this on your Raspberry Pi:" -ForegroundColor Yellow
    Write-Host @"
# Create baseline directory
mkdir -p ~/WellMonitor/src/WellMonitor.Device/$baselineDir

# Take 3 baseline captures
for i in {1..3}; do
  echo "Taking baseline capture `$i..."
  libcamera-still -o ~/WellMonitor/src/WellMonitor.Device/$baselineDir/baseline_`$i.jpg \
    --width 1920 --height 1080 --quality 95 --timeout 5000
  sleep 2
done

echo "Baseline captures complete. Ready for optimization."
"@ -ForegroundColor Green
    
    Write-Host
    $continue = Read-Host "Press Enter when baseline captures are complete, or 'skip' to continue without baseline"
    if ($continue -eq "skip") {
        Write-Host "Skipping baseline capture..." -ForegroundColor Yellow
    } else {
        Write-Host "✅ Baseline captures completed" -ForegroundColor Green
    }
    Write-Host
}

Write-Host "📋 Step 2: Updating device twin with LED-optimized settings..." -ForegroundColor Cyan

# Call the LED camera settings script
& "$PSScriptRoot\Update-LedCameraSettings.ps1" -IoTHubName $IoTHubName -DeviceId $DeviceId -SubscriptionName $SubscriptionName

Write-Host
Write-Host "📋 Step 3: Waiting for device configuration sync..." -ForegroundColor Cyan
Write-Host "Device twin updates typically take 30-60 seconds to propagate" -ForegroundColor Yellow

for ($i = 60; $i -gt 0; $i--) {
    Write-Progress -Activity "Waiting for device twin sync" -Status "Time remaining: $i seconds" -PercentComplete ((60-$i)/60*100)
    Start-Sleep 1
}
Write-Progress -Activity "Waiting for device twin sync" -Completed

Write-Host "✅ Configuration sync time elapsed" -ForegroundColor Green
Write-Host

Write-Host "📋 Step 4: Test capture with optimized settings" -ForegroundColor Cyan
Write-Host "Run this on your Raspberry Pi to test the new settings:" -ForegroundColor Yellow

$testDir = "debug_images/led_optimized_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Write-Host @"
# Restart the service to apply new settings
sudo systemctl restart wellmonitor

# Wait for service to start
sleep 10

# Create test directory
mkdir -p ~/WellMonitor/src/WellMonitor.Device/$testDir

# Test captures with optimized settings
echo "Testing LED-optimized camera settings..."

# Test 1: Default optimized settings
libcamera-still -o ~/WellMonitor/src/WellMonitor.Device/$testDir/led_test_1.jpg \
  --width 1280 --height 720 --quality 85 --timeout 5000 \
  --gain 12.0 --shutter 50000 --exposure off --awb off \
  --brightness 70 --contrast 50 --saturation 30

# Test 2: Higher gain for very dark conditions
libcamera-still -o ~/WellMonitor/src/WellMonitor.Device/$testDir/led_test_2.jpg \
  --width 1280 --height 720 --quality 85 --timeout 5000 \
  --gain 16.0 --shutter 75000 --exposure off --awb off \
  --brightness 80 --contrast 60 --saturation 40

# Test 3: Moderate settings for comparison
libcamera-still -o ~/WellMonitor/src/WellMonitor.Device/$testDir/led_test_3.jpg \
  --width 1280 --height 720 --quality 85 --timeout 5000 \
  --gain 8.0 --shutter 33000 --exposure off --awb off \
  --brightness 60 --contrast 40 --saturation 20

echo "Test captures complete!"
echo "Images saved to: ~/WellMonitor/src/WellMonitor.Device/$testDir/"
"@ -ForegroundColor Green

Write-Host
Write-Host "📋 Step 5: Monitor live captures" -ForegroundColor Cyan
Write-Host "Check that the WellMonitor service is using the new settings:" -ForegroundColor Yellow
Write-Host @"
# Monitor service logs for camera activity
sudo journalctl -u wellmonitor -f

# Check recent debug images
ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/ | tail -10

# Verify current device twin settings
az iot hub device-twin show --hub-name $IoTHubName --device-id $DeviceId | grep -A 10 camera
"@ -ForegroundColor Green

Write-Host
Write-Host "📋 Expected Results:" -ForegroundColor Yellow
Write-Host "✅ LED digits should appear clearer and more defined" -ForegroundColor Green
Write-Host "✅ Red color should be more vibrant and distinct" -ForegroundColor Green
Write-Host "✅ Background should be darker with better contrast" -ForegroundColor Green
Write-Host "✅ OCR should extract numbers more accurately" -ForegroundColor Green
Write-Host "❌ If images are overexposed (too bright), reduce gain to 8-10" -ForegroundColor Red
Write-Host "❌ If images are still too dark, increase gain to 16-20" -ForegroundColor Red
Write-Host "❌ If images are blurry, reduce shutter speed to 25000-33000μs" -ForegroundColor Red

Write-Host
Write-Host "📋 Troubleshooting:" -ForegroundColor Yellow
Write-Host "• If LEDs appear washed out: Reduce brightness and gain" -ForegroundColor White
Write-Host "• If background is too bright: Increase contrast, reduce brightness" -ForegroundColor White
Write-Host "• If colors are wrong: Check auto white balance is OFF" -ForegroundColor White
Write-Host "• If focus is soft: Ensure camera is 2-4 inches from display" -ForegroundColor White
Write-Host "• If settings not applied: Restart service and check device twin" -ForegroundColor White

Write-Host
Write-Host "🔧 Fine-tuning commands (run on Pi):" -ForegroundColor Cyan

# Determine Azure CLI path
$azPath = "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
if (-not (Test-Path $azPath)) {
    $azPath = "az" # Fallback to PATH
}

Write-Host @"
# For very bright LEDs (reduce gain)
$azPath iot hub device-twin update --hub-name $IoTHubName --device-id $DeviceId --set properties.desired.cameraGain=8.0

# For very dim LEDs (increase gain)
$azPath iot hub device-twin update --hub-name $IoTHubName --device-id $DeviceId --set properties.desired.cameraGain=16.0

# For motion blur (faster shutter)
$azPath iot hub device-twin update --hub-name $IoTHubName --device-id $DeviceId --set properties.desired.cameraShutterSpeedMicroseconds=25000

# For dark images (slower shutter)
$azPath iot hub device-twin update --hub-name $IoTHubName --device-id $DeviceId --set properties.desired.cameraShutterSpeedMicroseconds=75000
"@ -ForegroundColor Green

Write-Host
Write-Host "🎯 Test plan complete!" -ForegroundColor Green
Write-Host "Follow the steps above on your Raspberry Pi and compare the results." -ForegroundColor Yellow
