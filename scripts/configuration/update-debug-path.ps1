# Update debug image path in device twin to new secure location
param(
    [Parameter(Mandatory=$false)]
    [string]$IoTHubName = "RTHIoTHub",
    
    [Parameter(Mandatory=$false)]
    [string]$DeviceId = "rpi4b-1407well01"
)

Write-Host "🔧 Updating Debug Image Path in Device Twin" -ForegroundColor Blue
Write-Host "===========================================" -ForegroundColor Blue

# Update the debug image path to the new secure location
$patch = @{
    properties = @{
        desired = @{
            cameraDebugImagePath = "/var/lib/wellmonitor/debug_images"
        }
    }
} | ConvertTo-Json -Depth 5

Write-Host "📝 Updating debug image path to: /var/lib/wellmonitor/debug_images" -ForegroundColor Yellow

try {
    # Update the device twin
    az iot hub device-twin update --hub-name $IoTHubName --device-id $DeviceId --set $patch
    
    Write-Host "✅ Debug image path updated successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next Steps:" -ForegroundColor Blue
    Write-Host "1. Wait 1-2 minutes for device to receive the update" -ForegroundColor White
    Write-Host "2. Restart service: sudo systemctl restart wellmonitor" -ForegroundColor White
    Write-Host "3. Verify images: ls -la /var/lib/wellmonitor/debug_images/" -ForegroundColor White
}
catch {
    Write-Host "❌ Failed to update device twin: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
