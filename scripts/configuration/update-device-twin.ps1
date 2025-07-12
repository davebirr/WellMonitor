# WellMonitor Device Twin Management Script
# Comprehensive tool for managing all device twin settings

param(
    [Parameter(Mandatory=$true)]
    [string]$IoTHubName,
    
    [Parameter(Mandatory=$true)]
    [string]$DeviceId,
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionName = $null,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("camera", "ocr", "debug", "led", "monitoring", "all", "view")]
    [string]$ConfigType = "view",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("tesseract", "azure")]
    [string]$OcrProvider = "tesseract",
    
    [Parameter(Mandatory=$false)]
    [switch]$LedOptimization,
    
    [Parameter(Mandatory=$false)]
    [switch]$DebugMode,
    
    [Parameter(Mandatory=$false)]
    [string]$CustomPath = $null
)

# Colors for output
$Green = "Green"
$Red = "Red" 
$Yellow = "Yellow"
$Cyan = "Cyan"
$White = "White"
$Blue = "Blue"

function Write-Header {
    param([string]$Text)
    Write-Host "`n🔧 $Text" -ForegroundColor $Green
    Write-Host ("=" * ($Text.Length + 3)) -ForegroundColor $Green
}

function Write-Success {
    param([string]$Text)
    Write-Host "✅ $Text" -ForegroundColor $Green
}

function Write-Error {
    param([string]$Text)
    Write-Host "❌ $Text" -ForegroundColor $Red
}

function Write-Warning {
    param([string]$Text)
    Write-Host "⚠️  $Text" -ForegroundColor $Yellow
}

function Write-Info {
    param([string]$Text)
    Write-Host "ℹ️  $Text" -ForegroundColor $Cyan
}

# Initialize Azure CLI
function Initialize-AzureCli {
    Write-Header "Azure CLI Initialization"
    
    # Check if Azure CLI is available
    $azPath = "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
    if (-not (Test-Path $azPath)) {
        $azPath = "az" # Fallback to PATH
    }
    
    try {
        & $azPath --version 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Azure CLI found"
        } else {
            throw "Azure CLI not working"
        }
    } catch {
        Write-Error "Azure CLI not found or not working"
        Write-Warning "Please install Azure CLI from: https://aka.ms/installazurecliwindows"
        return $false
    }
    
    # Set subscription if provided
    if ($SubscriptionName) {
        Write-Info "Setting subscription: $SubscriptionName"
        & $azPath account set --subscription $SubscriptionName
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to set subscription"
            return $false
        }
    }
    
    # Check IoT extension
    $extensions = & $azPath extension list --query "[?name=='azure-iot'].name" -o tsv 2>$null
    if (-not $extensions -or $extensions -notcontains "azure-iot") {
        Write-Info "Installing Azure IoT extension..."
        & $azPath extension add --name azure-iot
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Azure IoT extension installed"
        } else {
            Write-Error "Failed to install Azure IoT extension"
            return $false
        }
    } else {
        Write-Success "Azure IoT extension available"
    }
    
    return $azPath
}

# Get current device twin
function Get-DeviceTwin {
    param([string]$AzPath)
    
    Write-Header "Current Device Twin Settings"
    
    try {
        $deviceTwin = & $AzPath iot hub device-twin show --hub-name $IoTHubName --device-id $DeviceId 2>$null | ConvertFrom-Json
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Device twin retrieved successfully"
            return $deviceTwin
        } else {
            throw "Failed to retrieve device twin"
        }
    } catch {
        Write-Error "Could not retrieve device twin: $($_.Exception.Message)"
        return $null
    }
}

# Display current settings
function Show-CurrentSettings {
    param($DeviceTwin)
    
    if (-not $DeviceTwin) {
        Write-Warning "No device twin data available"
        return
    }
    
    $desired = $DeviceTwin.properties.desired
    
    Write-Host "`n📷 Camera Settings:" -ForegroundColor $Blue
    @("cameraWidth", "cameraHeight", "cameraQuality", "cameraTimeoutMs", "cameraGain", "cameraShutterSpeedMicroseconds", "cameraAutoExposure", "cameraAutoWhiteBalance", "cameraBrightness", "cameraContrast", "cameraSaturation") | ForEach-Object {
        $value = $desired.$_ 
        if ($null -ne $value) {
            Write-Host "  $($_): $value" -ForegroundColor $White
        }
    }
    
    Write-Host "`n🔤 OCR Settings:" -ForegroundColor $Blue
    @("ocrProvider", "ocrMinimumConfidence", "tesseractLanguage", "tesseractEngineMode", "pageSegmentationMode", "characterWhitelist", "preprocessingEnabled", "thresholdValue") | ForEach-Object {
        $value = $desired.$_
        if ($null -ne $value) {
            Write-Host "  $($_): $value" -ForegroundColor $White
        }
    }
    
    Write-Host "`n🔧 Debug Settings:" -ForegroundColor $Blue
    @("debugMode", "debugImageSaveEnabled", "cameraDebugImagePath", "debugImagePath") | ForEach-Object {
        $value = $desired.$_
        if ($null -ne $value) {
            Write-Host "  $($_): $value" -ForegroundColor $White
        }
    }
    
    Write-Host "`n⏱️ Monitoring Settings:" -ForegroundColor $Blue
    @("monitoringIntervalSeconds", "telemetryIntervalSeconds", "syncIntervalHours") | ForEach-Object {
        $value = $desired.$_
        if ($null -ne $value) {
            Write-Host "  $($_): $value" -ForegroundColor $White
        }
    }
}

# Apply camera settings
function Set-CameraSettings {
    param([string]$AzPath, [bool]$LedOptimized = $false)
    
    Write-Header "Camera Settings Configuration"
    
    if ($LedOptimized) {
        Write-Info "Applying LED-optimized settings for dark environments"
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
        }
    } else {
        Write-Info "Applying standard camera settings"
        $cameraSettings = @{
            "cameraWidth" = 1920
            "cameraHeight" = 1080
            "cameraQuality" = 85
            "cameraTimeoutMs" = 2000
            "cameraWarmupTimeMs" = 2000
            "cameraBrightness" = 50
            "cameraContrast" = 30
            "cameraSaturation" = 20
            "cameraGain" = 1.0
            "cameraShutterSpeedMicroseconds" = 10000
            "cameraAutoExposure" = $true
            "cameraAutoWhiteBalance" = $true
        }
    }
    
    $updateArgs = @("iot", "hub", "device-twin", "update", "--hub-name", $IoTHubName, "--device-id", $DeviceId)
    
    foreach ($setting in $cameraSettings.GetEnumerator()) {
        $updateArgs += @("--set", "properties.desired.$($setting.Key)=$($setting.Value)")
        Write-Host "  $($setting.Key): $($setting.Value)" -ForegroundColor $White
    }
    
    try {
        & $AzPath @updateArgs | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Camera settings applied successfully"
        } else {
            throw "Failed to update camera settings"
        }
    } catch {
        Write-Error "Error applying camera settings: $($_.Exception.Message)"
    }
}

# Apply OCR settings
function Set-OcrSettings {
    param([string]$AzPath, [string]$Provider = "tesseract")
    
    Write-Header "OCR Settings Configuration"
    Write-Info "Setting OCR provider to: $Provider"
    
    if ($Provider -eq "tesseract") {
        $ocrSettings = @{
            "ocrProvider" = "tesseract"
            "ocrMinimumConfidence" = 0.7
            "tesseractLanguage" = "eng"
            "tesseractEngineMode" = 3
            "pageSegmentationMode" = 7
            "characterWhitelist" = "0123456789.DryAMPSrcyc "
            "preprocessingEnabled" = $true
            "thresholdValue" = 128
            "ocrRetryAttempts" = 3
            "ocrTimeoutMs" = 30000
        }
    } else {
        $ocrSettings = @{
            "ocrProvider" = "azure"
            "ocrMinimumConfidence" = 0.6
            "azureOcrEndpoint" = "https://your-region.cognitiveservices.azure.com/"
            "preprocessingEnabled" = $true
            "ocrRetryAttempts" = 2
            "ocrTimeoutMs" = 10000
        }
        Write-Warning "Don't forget to set Azure OCR endpoint and key in app configuration"
    }
    
    $updateArgs = @("iot", "hub", "device-twin", "update", "--hub-name", $IoTHubName, "--device-id", $DeviceId)
    
    foreach ($setting in $ocrSettings.GetEnumerator()) {
        $updateArgs += @("--set", "properties.desired.$($setting.Key)=$($setting.Value)")
        Write-Host "  $($setting.Key): $($setting.Value)" -ForegroundColor $White
    }
    
    try {
        & $AzPath @updateArgs | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "OCR settings applied successfully"
        } else {
            throw "Failed to update OCR settings"
        }
    } catch {
        Write-Error "Error applying OCR settings: $($_.Exception.Message)"
    }
}

# Apply debug settings
function Set-DebugSettings {
    param([string]$AzPath, [bool]$EnableDebug = $true, [string]$ImagePath = $null)
    
    Write-Header "Debug Settings Configuration"
    
    if (-not $ImagePath) {
        $ImagePath = "debug_images"  # Relative path for portability
    }
    
    $debugSettings = @{
        "debugMode" = $EnableDebug
        "debugImageSaveEnabled" = $EnableDebug
        "cameraDebugImagePath" = $ImagePath
        "debugImagePath" = $ImagePath
        "verboseLogging" = $EnableDebug
    }
    
    Write-Info "Debug mode: $EnableDebug"
    Write-Info "Image path: $ImagePath"
    
    $updateArgs = @("iot", "hub", "device-twin", "update", "--hub-name", $IoTHubName, "--device-id", $DeviceId)
    
    foreach ($setting in $debugSettings.GetEnumerator()) {
        $updateArgs += @("--set", "properties.desired.$($setting.Key)=$($setting.Value)")
        Write-Host "  $($setting.Key): $($setting.Value)" -ForegroundColor $White
    }
    
    try {
        & $AzPath @updateArgs | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Debug settings applied successfully"
        } else {
            throw "Failed to update debug settings"
        }
    } catch {
        Write-Error "Error applying debug settings: $($_.Exception.Message)"
    }
}

# Apply monitoring settings
function Set-MonitoringSettings {
    param([string]$AzPath)
    
    Write-Header "Monitoring Settings Configuration"
    
    $monitoringSettings = @{
        "monitoringIntervalSeconds" = 30
        "telemetryIntervalSeconds" = 300
        "syncIntervalHours" = 1
        "dataRetentionDays" = 30
        "alertThresholdDryAmps" = 2.0
        "alertThresholdRcycMinutes" = 5
        "energyCalculationEnabled" = $true
        "relayControlEnabled" = $true
    }
    
    Write-Info "Standard monitoring intervals for production use"
    
    $updateArgs = @("iot", "hub", "device-twin", "update", "--hub-name", $IoTHubName, "--device-id", $DeviceId)
    
    foreach ($setting in $monitoringSettings.GetEnumerator()) {
        $updateArgs += @("--set", "properties.desired.$($setting.Key)=$($setting.Value)")
        Write-Host "  $($setting.Key): $($setting.Value)" -ForegroundColor $White
    }
    
    try {
        & $AzPath @updateArgs | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Monitoring settings applied successfully"
        } else {
            throw "Failed to update monitoring settings"
        }
    } catch {
        Write-Error "Error applying monitoring settings: $($_.Exception.Message)"
    }
}

# Main execution
Write-Header "WellMonitor Device Twin Management"
Write-Info "IoT Hub: $IoTHubName"
Write-Info "Device: $DeviceId"
Write-Info "Configuration Type: $ConfigType"

# Initialize Azure CLI
$azPath = Initialize-AzureCli
if (-not $azPath) {
    exit 1
}

# Get current device twin
$deviceTwin = Get-DeviceTwin -AzPath $azPath

# Execute based on configuration type
switch ($ConfigType) {
    "view" {
        Show-CurrentSettings -DeviceTwin $deviceTwin
    }
    "camera" {
        Set-CameraSettings -AzPath $azPath -LedOptimized:$LedOptimization
    }
    "ocr" {
        Set-OcrSettings -AzPath $azPath -Provider $OcrProvider
    }
    "debug" {
        Set-DebugSettings -AzPath $azPath -EnableDebug:$DebugMode -ImagePath $CustomPath
    }
    "led" {
        Write-Info "Applying LED optimization (camera + debug)"
        Set-CameraSettings -AzPath $azPath -LedOptimized:$true
        Set-DebugSettings -AzPath $azPath -EnableDebug:$true
    }
    "monitoring" {
        Set-MonitoringSettings -AzPath $azPath
    }
    "all" {
        Write-Info "Applying all configuration settings"
        Set-CameraSettings -AzPath $azPath -LedOptimized:$LedOptimization
        Set-OcrSettings -AzPath $azPath -Provider $OcrProvider
        Set-DebugSettings -AzPath $azPath -EnableDebug:$DebugMode
        Set-MonitoringSettings -AzPath $azPath
    }
}

Write-Header "Configuration Complete"
Write-Success "Device twin updated successfully"
Write-Info "Wait 1-2 minutes for device to apply new settings"
Write-Info "Restart WellMonitor service: sudo systemctl restart wellmonitor"

if ($ConfigType -ne "view") {
    Write-Host "`n📋 Next Steps:" -ForegroundColor $Blue
    Write-Host "1. Monitor service logs: sudo journalctl -u wellmonitor -f" -ForegroundColor $White
    Write-Host "2. Check debug images: ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/" -ForegroundColor $White
    Write-Host "3. Verify settings applied: ./scripts/configuration/update-device-twin.ps1 -ConfigType view" -ForegroundColor $White
    Write-Host "4. Test camera capture manually if needed" -ForegroundColor $White
}
