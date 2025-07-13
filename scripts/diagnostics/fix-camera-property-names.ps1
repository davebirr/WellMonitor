#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fix camera property names in device twin to match CameraOptions model

.DESCRIPTION
    The device twin was using property names with "camera" prefix (cameraGain, cameraShutterSpeedMicroseconds)
    but the CameraOptions model expects properties without the prefix (Gain, ShutterSpeedMicroseconds).
    This script updates the device twin with the correct property names.

.EXAMPLE
    .\fix-camera-property-names.ps1

.NOTES
    Based on CameraOptions.cs analysis:
    - cameraGain -> Gain
    - cameraShutterSpeedMicroseconds -> ShutterSpeedMicroseconds  
    - cameraAutoExposure -> AutoExposure
    - cameraAutoWhiteBalance -> AutoWhiteBalance
    
    Uses .env file for configuration (standard practice, replaces secrets.json)
#>

function Get-EnvVariable {
    param(
        [string]$Name,
        [string]$EnvFilePath = ".env"
    )
    
    # First try environment variable
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ($value) {
        return $value
    }
    
    # Then try .env file
    $rootPath = Split-Path (Split-Path (Get-Location) -Parent) -Parent
    $envPath = Join-Path $rootPath $EnvFilePath
    
    if (Test-Path $envPath) {
        $envContent = Get-Content $envPath
        foreach ($line in $envContent) {
            if ($line -match "^$Name\s*=\s*(.+)$") {
                return $matches[1].Trim('"').Trim("'")
            }
        }
    }
    
    return $null
}

# Check if Azure CLI is available
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI is not installed or not in PATH. Please run Setup-AzureCli.ps1 first."
    exit 1
}

# Check if IoT extension is installed
$extensions = az extension list --output json | ConvertFrom-Json
if (-not ($extensions | Where-Object { $_.name -eq "azure-iot" })) {
    Write-Warning "Azure IoT extension not found. Installing..."
    az extension add --name azure-iot
}

# Get connection string from .env file or environment
$connectionString = Get-EnvVariable -Name "WELLMONITOR_IOTHUB_CONNECTION_STRING"

if (-not $connectionString) {
    Write-Error "WELLMONITOR_IOTHUB_CONNECTION_STRING not found in environment or .env file"
    Write-Host "💡 Create a .env file in the repository root with:" -ForegroundColor Yellow
    Write-Host "WELLMONITOR_IOTHUB_CONNECTION_STRING=HostName=YourIoTHub.azure-devices.net;DeviceId=YourDevice;SharedAccessKey=YourKey" -ForegroundColor Gray
    exit 1
}

$deviceId = "wellmonitor-pi"

Write-Host "🔧 Fixing camera property names in device twin..." -ForegroundColor Yellow

# Create the correct device twin patch with proper property names
$deviceTwinPatch = @{
    properties = @{
        desired = @{
            # Camera settings with correct property names (matching CameraOptions.cs)
            Camera = @{
                # Reduce gain significantly for overexposed red LEDs
                Gain = 0.5                          # Was cameraGain = 12.0 (way too high)
                ShutterSpeedMicroseconds = 5000     # Was cameraShutterSpeedMicroseconds = 50000 (too long)
                AutoExposure = $false               # Was cameraAutoExposure = false
                AutoWhiteBalance = $false           # Was cameraAutoWhiteBalance = false
                Brightness = 30                     # Reduce from 50
                Contrast = 20                       # Reduce from 40
                DebugImagePath = "/var/lib/wellmonitor/debug_images"
            }
        }
    }
}

# Convert to JSON
$jsonPatch = $deviceTwinPatch | ConvertTo-Json -Depth 10

Write-Host "Device twin patch:" -ForegroundColor Cyan
Write-Host $jsonPatch -ForegroundColor Gray

try {
    # Parse connection string to get hub name and device ID
    $hubName = ($connectionString -split ';' | Where-Object { $_ -like "HostName=*" }) -replace "HostName=", "" -replace ".azure-devices.net", ""
    $actualDeviceId = ($connectionString -split ';' | Where-Object { $_ -like "DeviceId=*" }) -replace "DeviceId=", ""
    
    Write-Host "🔍 Using Hub: $hubName, Device: $actualDeviceId" -ForegroundColor Cyan
    
    # Apply the device twin update using hub name and device ID
    Write-Host "📡 Updating device twin with correct camera property names..." -ForegroundColor Blue
    
    az iot hub device-twin update `
        --hub-name $hubName `
        --device-id $actualDeviceId `
        --set $jsonPatch
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Device twin updated successfully with correct camera property names!" -ForegroundColor Green
        Write-Host "🔍 Key changes:" -ForegroundColor Yellow
        Write-Host "  • cameraGain (12.0) -> Gain (0.5) - Much lower for bright LEDs" -ForegroundColor White
        Write-Host "  • cameraShutterSpeedMicroseconds (50000) -> ShutterSpeedMicroseconds (5000) - Faster shutter" -ForegroundColor White
        Write-Host "  • cameraAutoExposure -> AutoExposure" -ForegroundColor White
        Write-Host "  • cameraAutoWhiteBalance -> AutoWhiteBalance" -ForegroundColor White
        Write-Host "  • Reduced Brightness (30) and Contrast (20) for LED environment" -ForegroundColor White
        Write-Host ""
        Write-Host "🔄 The WellMonitor service should pick up these changes automatically." -ForegroundColor Cyan
        Write-Host "🖼️  New debug images should be properly exposed for red LED digits." -ForegroundColor Cyan
    } else {
        Write-Error "Failed to update device twin. Check Azure CLI configuration."
        exit 1
    }
} catch {
    Write-Error "Error updating device twin: $_"
    exit 1
}

Write-Host ""
Write-Host "🎯 Camera optimization complete!" -ForegroundColor Green
Write-Host "Monitor the debug images in /var/lib/wellmonitor/debug_images/ for improvement." -ForegroundColor Yellow
