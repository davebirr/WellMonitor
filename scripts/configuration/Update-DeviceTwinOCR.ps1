# Azure IoT Hub Device Twin OCR Configuration Script
# This script helps you update your device twin with OCR settings

param(
    [Parameter(Mandatory=$true)]
    [string]$DeviceId,
    
    [Parameter(Mandatory=$true)]
    [string]$IoTHubName,
    
    [Parameter(Mandatory=$false)]
    [string]$OcrProvider = "Tesseract",
    
    [Parameter(Mandatory=$false)]
    [double]$MinimumConfidence = 0.7,
    
    [Parameter(Mandatory=$false)]
    [string]$TesseractLanguage = "eng",
    
    [Parameter(Mandatory=$false)]
    [int]$TesseractEngineMode = 3,
    
    [Parameter(Mandatory=$false)]
    [int]$PageSegmentationMode = 7,
    
    [Parameter(Mandatory=$false)]
    [string]$CharWhitelist = "0123456789.DryAMPSrcyc ",
    
    [Parameter(Mandatory=$false)]
    [int]$ThresholdValue = 128,
    
    [Parameter(Mandatory=$false)]
    [switch]$UseAdvancedSettings
)

Write-Host "🔧 Updating Device Twin OCR Configuration" -ForegroundColor Green
Write-Host "Device ID: $DeviceId" -ForegroundColor Cyan
Write-Host "IoT Hub: $IoTHubName" -ForegroundColor Cyan
Write-Host "OCR Provider: $OcrProvider" -ForegroundColor Cyan

# Check if Azure CLI is installed
try {
    $azVersion = az --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI not found"
    }
    Write-Host "✅ Azure CLI found" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure CLI not found. Please install Azure CLI first." -ForegroundColor Red
    Write-Host "Download from: https://aka.ms/installazurecliwindows" -ForegroundColor Yellow
    exit 1
}

# Check if logged in to Azure
try {
    $account = az account show --query "name" -o tsv 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Not logged in"
    }
    Write-Host "✅ Logged in to Azure as: $account" -ForegroundColor Green
} catch {
    Write-Host "❌ Not logged in to Azure. Please run 'az login' first." -ForegroundColor Red
    exit 1
}

# Basic OCR configuration
$basicConfig = @{
    "ocrProvider" = $OcrProvider
    "ocrMinimumConfidence" = $MinimumConfidence
    "ocrTesseractLanguage" = $TesseractLanguage
    "ocrTesseractEngineMode" = $TesseractEngineMode
    "ocrTesseractPageSegmentationMode" = $PageSegmentationMode
    "ocrTesseractCharWhitelist" = $CharWhitelist
}

# Advanced configuration
$advancedConfig = @{
    "ocrImagePreprocessing" = @{
        "enableGrayscale" = $true
        "enableContrastEnhancement" = $true
        "contrastFactor" = 1.5
        "enableBrightnessAdjustment" = $true
        "brightnessAdjustment" = 10
        "enableNoiseReduction" = $true
        "enableEdgeEnhancement" = $false
        "enableScaling" = $true
        "scaleFactor" = 2.0
        "enableBinaryThresholding" = $true
        "binaryThreshold" = $ThresholdValue
    }
    "ocrRetrySettings" = @{
        "maxRetries" = 3
        "retryDelayMs" = 1000
    }
}

# Combine configurations
$config = $basicConfig
if ($UseAdvancedSettings) {
    $config += $advancedConfig
    Write-Host "📊 Using advanced OCR settings" -ForegroundColor Yellow
}

Write-Host "🔄 Updating device twin..." -ForegroundColor Yellow

try {
    # Build the Azure CLI command
    $setParams = @()
    foreach ($key in $config.Keys) {
        $value = $config[$key]
        if ($value -is [hashtable]) {
            # Handle nested objects
            $jsonValue = $value | ConvertTo-Json -Compress
            $setParams += "--set `"properties.desired.$key=$jsonValue`""
        } else {
            # Handle simple values
            if ($value -is [string]) {
                $setParams += "--set `"properties.desired.$key=`"$value`"`""
            } else {
                $setParams += "--set `"properties.desired.$key=$value`""
            }
        }
    }
    
    $azCommand = "az iot hub device-twin update --device-id `"$DeviceId`" --hub-name `"$IoTHubName`" " + ($setParams -join " ")
    
    Write-Host "Executing: $azCommand" -ForegroundColor Gray
    
    # Execute the command
    $result = Invoke-Expression $azCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Device twin updated successfully!" -ForegroundColor Green
        
        # Show the updated configuration
        Write-Host "`n📋 Updated OCR Configuration:" -ForegroundColor Cyan
        foreach ($key in $config.Keys) {
            $value = $config[$key]
            if ($value -is [hashtable]) {
                Write-Host "  $key:" -ForegroundColor White
                foreach ($subKey in $value.Keys) {
                    Write-Host "    $subKey`: $($value[$subKey])" -ForegroundColor Gray
                }
            } else {
                Write-Host "  $key`: $value" -ForegroundColor White
            }
        }
        
        Write-Host "`n🎯 Next Steps:" -ForegroundColor Green
        Write-Host "1. Monitor your device logs for OCR configuration updates" -ForegroundColor White
        Write-Host "2. Check the device twin reported properties for confirmation" -ForegroundColor White
        Write-Host "3. Test OCR with a sample image" -ForegroundColor White
        Write-Host "4. Fine-tune settings based on your LED display characteristics" -ForegroundColor White
        
    } else {
        Write-Host "❌ Failed to update device twin" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Error updating device twin: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n📚 For more detailed configuration options, see:" -ForegroundColor Yellow
Write-Host "docs/DeviceTwin-OCR-Configuration.md" -ForegroundColor Cyan
