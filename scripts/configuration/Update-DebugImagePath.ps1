#!/usr/bin/env pwsh

<#
.SYNOPSIS
Update Azure IoT Hub device twin with correct debug image path for Pi deployment

.DESCRIPTION
This script updates the device twin to use the correct absolute path for debug images
on the Raspberry Pi: /home/davidb/WellMonitor/src/WellMonitor.Device/debug_images

.PARAMETER DeviceName
The name of the IoT device (default: rpi4b-1407well01)

.PARAMETER IoTHubName
The name of the IoT Hub (default: WellMonitorIoTHub-dev)

.PARAMETER ResourceGroup
The name of the resource group (default: WellMonitor-dev)

.EXAMPLE
./Update-DebugImagePath.ps1
./Update-DebugImagePath.ps1 -DeviceName "my-device" -IoTHubName "my-hub"
#>

param(
    [string]$DeviceName = "rpi4b-1407well01",
    [string]$IoTHubName = "WellMonitorIoTHub-dev", 
    [string]$ResourceGroup = "WellMonitor-dev"
)

Write-Host "🔧 Updating Debug Image Path Configuration" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Device: $DeviceName" -ForegroundColor Yellow
Write-Host "IoT Hub: $IoTHubName" -ForegroundColor Yellow
Write-Host "Resource Group: $ResourceGroup" -ForegroundColor Yellow
Write-Host ""

try {
    Write-Host "📝 Updating device twin with absolute debug image path..." -ForegroundColor Green
    
    $deviceTwinUpdate = @{
        "properties" = @{
            "desired" = @{
                "debugImageSaveEnabled" = $true
                "cameraDebugImagePath" = "/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"
            }
        }
    } | ConvertTo-Json -Depth 10

    Write-Host "Device twin update payload:" -ForegroundColor Gray
    Write-Host $deviceTwinUpdate -ForegroundColor Gray
    Write-Host ""

    # Update device twin
    az iot hub device-twin update `
        --device-id $DeviceName `
        --hub-name $IoTHubName `
        --set $deviceTwinUpdate

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Device twin updated successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Debug Image Configuration:" -ForegroundColor Cyan
        Write-Host "- debugImageSaveEnabled: true" -ForegroundColor Green
        Write-Host "- cameraDebugImagePath: /home/davidb/WellMonitor/src/WellMonitor.Device/debug_images" -ForegroundColor Green
        Write-Host ""
        Write-Host "The device will pick up these changes automatically." -ForegroundColor Yellow
        Write-Host "Debug images will now be saved to the correct location." -ForegroundColor Yellow
    } else {
        Write-Host "❌ Failed to update device twin!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Error updating device twin: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎉 Debug image path configuration completed!" -ForegroundColor Green
