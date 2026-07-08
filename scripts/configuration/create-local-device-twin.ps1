#!/usr/bin/env pwsh
# Create a local development device twin for testing

param(
    [string]$DeviceName = "LAPTOP-FBVH49A7",
    [string]$IoTHubName = "",  # Will auto-detect from .env or use RTHIoTHub
    [string]$ResourceGroup = "your-resource-group"  # Replace with your resource group
)

$ErrorActionPreference = "Stop"

# Function to extract IoT Hub name from connection string or .env file
function Get-IoTHubName {
    param([string]$ProvidedName)
    
    if (-not [string]::IsNullOrEmpty($ProvidedName)) {
        return $ProvidedName
    }
    
    # Try to read from .env file
    $envPath = Join-Path $PSScriptRoot "../../.env"
    if (Test-Path $envPath) {
        Write-Host "📖 Reading configuration from .env file..." -ForegroundColor Yellow
        $envContent = Get-Content $envPath
        $connectionStringLine = $envContent | Where-Object { $_ -like "*WELLMONITOR_IOTHUB_CONNECTION_STRING*" -and $_ -notlike "#*" }
        
        if ($connectionStringLine) {
            # Extract HostName from connection string: HostName=YourHub.azure-devices.net;...
            if ($connectionStringLine -match 'HostName=([^.]+)\.azure-devices\.net') {
                $hubName = $matches[1]
                Write-Host "✅ Found IoT Hub name in .env: $hubName" -ForegroundColor Green
                return $hubName
            }
        }
        
        # Also check for explicit hub name variable
        $hubNameLine = $envContent | Where-Object { $_ -like "*WELLMONITOR_IOTHUB_NAME*" -and $_ -notlike "#*" }
        if ($hubNameLine -and $hubNameLine -match '=(.+)$') {
            $hubName = $matches[1].Trim('"').Trim("'")
            Write-Host "✅ Found IoT Hub name in .env: $hubName" -ForegroundColor Green
            return $hubName
        }
    }
    
    # Default fallback
    Write-Host "⚠️  No IoT Hub name found in .env, using default: RTHIoTHub" -ForegroundColor Yellow
    return "RTHIoTHub"
}

# Auto-detect IoT Hub name
$IoTHubName = Get-IoTHubName -ProvidedName $IoTHubName

Write-Host "🔧 Creating Local Development Device Twin" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Device: $DeviceName" -ForegroundColor Yellow
Write-Host "IoT Hub: $IoTHubName" -ForegroundColor Yellow
Write-Host ""

try {
    # Check if device already exists
    Write-Host "📋 Checking if device already exists..." -ForegroundColor Green
    $existingDevice = az iot hub device-identity show --device-id $DeviceName --hub-name $IoTHubName 2>$null
    
    if ($existingDevice) {
        Write-Host "✅ Device '$DeviceName' already exists" -ForegroundColor Green
    } else {
        Write-Host "📝 Creating new device identity..." -ForegroundColor Green
        az iot hub device-identity create --device-id $DeviceName --hub-name $IoTHubName --auth-method shared_private_key
        Write-Host "✅ Device identity created successfully" -ForegroundColor Green
    }

    Write-Host "🔧 Configuring device twin for local development..." -ForegroundColor Green
    
    # Local development device twin configuration
    $deviceTwinPatch = @{
        properties = @{
            desired = @{
                # Well Monitor Core Settings (use safe test values)
                currentThreshold = 2.0  # Lower threshold for testing
                cycleTimeThreshold = 60  # Longer cycle time for testing
                relayDebounceMs = 1000
                syncIntervalMinutes = 10  # More frequent sync for testing
                logRetentionDays = 7
                ocrMode = "tesseract"  # Use offline OCR for local testing
                powerAppEnabled = $false  # Disable PowerApp for local testing
                
                # Debug Configuration (ENABLED for local testing)
                debugMode = $true
                debugImageSaveEnabled = $true
                debugImagePath = "debug_images"  # Relative path for local dev
                verboseLogging = $true
                enableVerboseOcrLogging = $true
                debugImageRetentionDays = 3  # Keep fewer debug images locally
                logLevel = "Debug"  # More verbose logging for development
                
                # Camera Configuration (optimized for local testing)
                Camera = @{
                    Width = 1920
                    Height = 1080
                    Quality = 85  # Lower quality for faster local testing
                    TimeoutMs = 8000
                    WarmupTimeMs = 1000  # Faster warmup for development
                    Rotation = 0
                    Brightness = 50
                    Contrast = 0
                    Saturation = 0
                    Gain = 1.0  # Low gain for normal lighting
                    ShutterSpeedMicroseconds = 0  # Auto shutter
                    AutoExposure = $true  # Enable auto exposure for development
                    AutoWhiteBalance = $true  # Enable auto white balance
                    EnablePreview = $false
                    DebugImagePath = "debug_images"  # Local debug path
                }
                
                # OCR Configuration (optimized for development)
                OCR = @{
                    Provider = "Tesseract"  # Use offline OCR for local testing
                    MinimumConfidence = 0.6  # Lower confidence for testing
                    MaxRetryAttempts = 2  # Fewer retries for faster testing
                    TimeoutSeconds = 15  # Shorter timeout for development
                    EnablePreprocessing = $true
                    
                    # Image preprocessing for development
                    ImagePreprocessing = @{
                        EnableScaling = $true
                        ScaleFactor = 2.0
                        BinaryThreshold = 128
                        ContrastFactor = 1.2
                        BrightnessAdjustment = 5
                    }
                }
                
                # Monitoring Configuration (faster intervals for testing)
                monitoringIntervalSeconds = 60  # Check every minute for testing
                telemetryIntervalMinutes = 2   # Send telemetry every 2 minutes
                syncIntervalHours = 1          # Sync every hour
                dataRetentionDays = 7          # Keep less data locally
                
                # Web Dashboard Configuration (local development)
                webPort = 5000
                webAllowNetworkAccess = $true  # Allow network access for testing
                webBindAddress = "0.0.0.0"     # Bind to all interfaces
                webEnableHttps = $false        # HTTP only for local dev
                webEnableAuthentication = $false  # No auth for local testing
                
                # Alert Configuration (relaxed for testing)
                alertDryCountThreshold = 5     # More tolerant for testing
                alertRcycCountThreshold = 3
                alertMaxRetryAttempts = 3
                alertCooldownMinutes = 5       # Shorter cooldown for testing
                
                # Image Quality (relaxed for development)
                imageQualityMinThreshold = 0.5  # Lower quality threshold
                imageQualityBrightnessMin = 20
                imageQualityBrightnessMax = 200
                imageQualityContrastMin = 0.3
                imageQualityNoiseMax = 0.8
                
                # Environment marker
                environment = "development"
                lastUpdated = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        }
    }

    # Convert to JSON and update device twin
    $jsonPatch = $deviceTwinPatch | ConvertTo-Json -Depth 10
    
    Write-Host "📝 Updating device twin with local development configuration..." -ForegroundColor Green
    $jsonPatch | az iot hub device-twin update --device-id $DeviceName --hub-name $IoTHubName --set @-

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Local development device twin configured successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "🔍 Local Development Configuration:" -ForegroundColor Cyan
        Write-Host "  • Device Name: LAPTOP-FBVH49A7" -ForegroundColor Green
        Write-Host "  • Debug Mode: ENABLED" -ForegroundColor Green
        Write-Host "  • Debug Images: ENABLED (debug_images/)" -ForegroundColor Green
        Write-Host "  • OCR Provider: Tesseract (offline)" -ForegroundColor Green
        Write-Host "  • Camera: Auto exposure/white balance" -ForegroundColor Green
        Write-Host "  • Monitoring: Every 60 seconds" -ForegroundColor Green
        Write-Host "  • Web Dashboard: Port 5000, no auth" -ForegroundColor Green
        Write-Host "  • Logging: Debug level with verbose OCR" -ForegroundColor Green
        Write-Host ""
        Write-Host "🔑 Next Steps:" -ForegroundColor Yellow
        Write-Host "1. Get connection string: az iot hub device-identity connection-string show --device-id $DeviceName --hub-name $IoTHubName" -ForegroundColor White
        Write-Host "2. Add to your .env file: WELLMONITOR_DEVICE_CONNECTION_STRING=..." -ForegroundColor White
        Write-Host "3. Or set environment variable: export WELLMONITOR_DEVICE_CONNECTION_STRING=..." -ForegroundColor White
        Write-Host "4. Run: dotnet run" -ForegroundColor White
        Write-Host "5. Check logs for device twin sync messages" -ForegroundColor White
    } else {
        Write-Host "❌ Failed to configure device twin!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Error creating local device twin: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎉 Local development device twin setup complete!" -ForegroundColor Green
