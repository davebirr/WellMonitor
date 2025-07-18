#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Updates camera exposure mode configuration in Azure IoT Hub device twin.
    
.DESCRIPTION
    This script updates the camera exposure mode configuration in the device twin.
    It supports both nested and legacy configuration formats.
    
.PARAMETER DeviceId
    The device ID in Azure IoT Hub
    
.PARAMETER HubName
    The name of the Azure IoT Hub
    
.PARAMETER ExposureMode
    The exposure mode to set. Valid values:
    - Auto (default)
    - Normal
    - Sport
    - Night
    - Backlight
    - Spotlight
    - Beach
    - Snow
    - Fireworks
    - Party
    - Candlelight
    - Barcode (recommended for LED displays)
    - Macro
    - Landscape
    - Portrait
    - Antishake
    - FixedFps
    
.PARAMETER UseNestedConfig
    Use nested configuration format (Camera.ExposureMode). Default is false for legacy format.
    
.EXAMPLE
    ./update-camera-exposure-mode.ps1 -DeviceId "wellmonitor-device" -HubName "wellmonitor-hub" -ExposureMode "Barcode"
    
.EXAMPLE
    ./update-camera-exposure-mode.ps1 -DeviceId "wellmonitor-device" -HubName "wellmonitor-hub" -ExposureMode "Auto" -UseNestedConfig
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$DeviceId,
    
    [Parameter(Mandatory = $true)]
    [string]$HubName,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("Auto", "Normal", "Sport", "Night", "Backlight", "Spotlight", "Beach", "Snow", "Fireworks", "Party", "Candlelight", "Barcode", "Macro", "Landscape", "Portrait", "Antishake", "FixedFps")]
    [string]$ExposureMode,
    
    [Parameter(Mandatory = $false)]
    [switch]$UseNestedConfig = $false
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Validate Azure CLI is available
if (-not (Get-Command "az" -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI is not installed or not in PATH. Please install Azure CLI first."
    exit 1
}

# Check if logged in to Azure
$account = az account show --output json 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged in to Azure. Please run 'az login' first."
    exit 1
}

# Verify IoT Hub exists
Write-Host "Verifying IoT Hub '$HubName' exists..." -ForegroundColor Yellow
$hubExists = az iot hub show --name $HubName --output json 2>$null
if (-not $hubExists) {
    Write-Error "IoT Hub '$HubName' not found or not accessible."
    exit 1
}

# Verify device exists
Write-Host "Verifying device '$DeviceId' exists..." -ForegroundColor Yellow
$deviceExists = az iot hub device-identity show --device-id $DeviceId --hub-name $HubName --output json 2>$null
if (-not $deviceExists) {
    Write-Error "Device '$DeviceId' not found in IoT Hub '$HubName'."
    exit 1
}

# Get current device twin
Write-Host "Getting current device twin configuration..." -ForegroundColor Yellow
$deviceTwin = az iot hub device-twin show --device-id $DeviceId --hub-name $HubName --output json | ConvertFrom-Json

# Create the patch based on configuration format
if ($UseNestedConfig) {
    Write-Host "Using nested configuration format (Camera.ExposureMode)..." -ForegroundColor Yellow
    
    # Initialize nested structure if it doesn't exist
    if (-not $deviceTwin.properties.desired.Camera) {
        $deviceTwin.properties.desired | Add-Member -MemberType NoteProperty -Name "Camera" -Value @{}
    }
    
    $patch = @{
        properties = @{
            desired = @{
                Camera = @{
                    ExposureMode = $ExposureMode
                }
            }
        }
    }
    
    Write-Host "Setting Camera.ExposureMode = '$ExposureMode'" -ForegroundColor Green
} else {
    Write-Host "Using legacy configuration format (cameraExposureMode)..." -ForegroundColor Yellow
    
    $patch = @{
        properties = @{
            desired = @{
                cameraExposureMode = $ExposureMode
            }
        }
    }
    
    Write-Host "Setting cameraExposureMode = '$ExposureMode'" -ForegroundColor Green
}

# Convert patch to JSON
$patchJson = $patch | ConvertTo-Json -Depth 5 -Compress

# Update device twin
Write-Host "Updating device twin..." -ForegroundColor Yellow
try {
    $result = az iot hub device-twin update --device-id $DeviceId --hub-name $HubName --set $patchJson --output json
    if ($result) {
        Write-Host "✅ Device twin updated successfully!" -ForegroundColor Green
        Write-Host "Camera exposure mode set to: $ExposureMode" -ForegroundColor Green
        
        # Display exposure mode description
        $descriptions = @{
            "Auto" = "Automatic exposure mode selection"
            "Normal" = "Standard exposure mode for general use"
            "Sport" = "Fast shutter speed for moving subjects"
            "Night" = "Enhanced low-light performance"
            "Backlight" = "Compensates for bright background"
            "Spotlight" = "Optimized for bright spot lighting"
            "Beach" = "Optimized for bright beach/sand conditions"
            "Snow" = "Optimized for bright snow conditions"
            "Fireworks" = "Long exposure for fireworks"
            "Party" = "Indoor party lighting"
            "Candlelight" = "Warm, low-light conditions"
            "Barcode" = "High contrast for barcode/LED reading"
            "Macro" = "Close-up photography"
            "Landscape" = "Wide depth of field"
            "Portrait" = "Shallow depth of field"
            "Antishake" = "Reduced camera shake"
            "FixedFps" = "Fixed frame rate mode"
        }
        
        if ($descriptions.ContainsKey($ExposureMode)) {
            Write-Host "Mode Description: $($descriptions[$ExposureMode])" -ForegroundColor Cyan
        }
        
        Write-Host ""
        Write-Host "The device will automatically pick up this configuration change." -ForegroundColor Yellow
        Write-Host "Monitor the device logs to confirm the new exposure mode is applied." -ForegroundColor Yellow
    } else {
        Write-Error "Failed to update device twin (no result returned)"
    }
} catch {
    Write-Error "Failed to update device twin: $($_.Exception.Message)"
    exit 1
}

# Optional: Show current device twin desired properties
Write-Host ""
Write-Host "Current device twin desired properties:" -ForegroundColor Yellow
$updatedTwin = az iot hub device-twin show --device-id $DeviceId --hub-name $HubName --output json | ConvertFrom-Json
$desired = $updatedTwin.properties.desired

if ($UseNestedConfig -and $desired.Camera) {
    Write-Host "Camera Configuration:" -ForegroundColor Cyan
    $desired.Camera | ConvertTo-Json -Depth 2 | Write-Host
} else {
    Write-Host "Camera-related properties:" -ForegroundColor Cyan
    $desired.PSObject.Properties | Where-Object { $_.Name -like "camera*" } | ForEach-Object {
        Write-Host "  $($_.Name): $($_.Value)" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "✅ Camera exposure mode configuration completed!" -ForegroundColor Green
