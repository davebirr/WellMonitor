# PowerShell script to update device twin with extended monitoring, quality, alert, and debug settings
# Run this script to configure your device twin with additional remote configuration options

param(
    [Parameter(Mandatory=$true)]
    [string]$DeviceId,
    
    [Parameter(Mandatory=$true)]
    [string]$IoTHubName,
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = ""
)

Write-Host "🔧 Updating Device Twin with Extended Configuration Settings..." -ForegroundColor Green
Write-Host "Device ID: $DeviceId" -ForegroundColor Yellow
Write-Host "IoT Hub: $IoTHubName" -ForegroundColor Yellow

# Extended configuration settings
$ExtendedConfig = @{
    # Monitoring & Telemetry Intervals
    "monitoringIntervalSeconds" = 30
    "telemetryIntervalMinutes" = 5
    "syncIntervalHours" = 1
    "dataRetentionDays" = 30
    
    # OCR Performance Settings
    "ocrMaxRetryAttempts" = 3
    "ocrTimeoutSeconds" = 30
    "ocrEnablePreprocessing" = $true
    
    # Image Quality Validation
    "imageQualityMinThreshold" = 0.7
    "imageQualityBrightnessMin" = 50
    "imageQualityBrightnessMax" = 200
    "imageQualityContrastMin" = 0.3
    "imageQualityNoiseMax" = 0.5
    
    # Alert Configuration
    "alertDryCountThreshold" = 3
    "alertRcycCountThreshold" = 2
    "alertMaxRetryAttempts" = 5
    "alertCooldownMinutes" = 15
    
    # Debug & Logging Settings
    "debugMode" = $false
    "debugImageSaveEnabled" = $false
    "debugImageRetentionDays" = 7
    "logLevel" = "Information"
    "enableVerboseOcrLogging" = $false
}

Write-Host "`n📋 Configuration to apply:" -ForegroundColor Cyan

# Display the configuration that will be applied
foreach ($key in $ExtendedConfig.Keys) {
    $value = $ExtendedConfig[$key]
    Write-Host "  $key = $value" -ForegroundColor Gray
}

Write-Host "`n🚀 Applying configuration updates..." -ForegroundColor Green

try {
    # Build the az command based on whether resource group is specified
    $baseCommand = "az iot hub device-twin update --device-id $DeviceId --hub-name $IoTHubName"
    
    if ($ResourceGroup) {
        $baseCommand += " --resource-group $ResourceGroup"
    }
    
    # Apply each configuration setting
    foreach ($key in $ExtendedConfig.Keys) {
        $value = $ExtendedConfig[$key]
        
        # Handle boolean values
        if ($value -is [bool]) {
            $value = $value.ToString().ToLower()
        }
        
        $command = "$baseCommand --set properties.desired.$key=$value"
        
        Write-Host "  Updating $key..." -ForegroundColor Yellow
        Invoke-Expression $command
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to update $key" -ForegroundColor Red
            return
        }
    }
    
    Write-Host "`n✅ Device twin updated successfully!" -ForegroundColor Green
    
    Write-Host "`n📊 Extended Configuration Summary:" -ForegroundColor Cyan
    Write-Host "  🔄 Monitoring Intervals:" -ForegroundColor White
    Write-Host "    - Monitoring: 30 seconds" -ForegroundColor Gray
    Write-Host "    - Telemetry: 5 minutes" -ForegroundColor Gray
    Write-Host "    - Sync: 1 hour" -ForegroundColor Gray
    Write-Host "    - Data Retention: 30 days" -ForegroundColor Gray
    
    Write-Host "  🎯 OCR Performance:" -ForegroundColor White
    Write-Host "    - Max Retries: 3" -ForegroundColor Gray
    Write-Host "    - Timeout: 30 seconds" -ForegroundColor Gray
    Write-Host "    - Preprocessing: Enabled" -ForegroundColor Gray
    
    Write-Host "  📷 Image Quality:" -ForegroundColor White
    Write-Host "    - Min Threshold: 0.7" -ForegroundColor Gray
    Write-Host "    - Brightness: 50-200" -ForegroundColor Gray
    Write-Host "    - Min Contrast: 0.3" -ForegroundColor Gray
    Write-Host "    - Max Noise: 0.5" -ForegroundColor Gray
    
    Write-Host "  🚨 Alert Thresholds:" -ForegroundColor White
    Write-Host "    - Dry Count: 3 readings" -ForegroundColor Gray
    Write-Host "    - RCyc Count: 2 readings" -ForegroundColor Gray
    Write-Host "    - Max Retries: 5" -ForegroundColor Gray
    Write-Host "    - Cooldown: 15 minutes" -ForegroundColor Gray
    
    Write-Host "  🔍 Debug Settings:" -ForegroundColor White
    Write-Host "    - Debug Mode: Disabled" -ForegroundColor Gray
    Write-Host "    - Image Save: Disabled" -ForegroundColor Gray
    Write-Host "    - Log Level: Information" -ForegroundColor Gray
    Write-Host "    - Verbose OCR: Disabled" -ForegroundColor Gray
    
    Write-Host "`n🎉 Your device twin now has comprehensive remote configuration!" -ForegroundColor Green
    Write-Host "💡 Restart your device application to apply the new settings." -ForegroundColor Yellow
    
} catch {
    Write-Host "❌ Error updating device twin: $($_.Exception.Message)" -ForegroundColor Red
}
