#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy enhanced device twin configuration logging to Raspberry Pi

.DESCRIPTION
    This script builds the WellMonitor.Device project and deploys the enhanced 
    configuration logging features to the Raspberry Pi. The improvements include:
    
    - Detailed camera configuration logging with device twin vs default tracking
    - Support for nested Camera properties (Camera.Gain, etc.)
    - Backward compatibility with legacy flat properties
    - Warnings for default values and problematic settings
    - Hourly configuration summary reports

.EXAMPLE
    .\deploy-improved-logging.ps1

.NOTES
    Requires SSH access to raspberrypi.local as user 'pi'
#>

param(
    [string]$PiHost = "pi@raspberrypi.local",
    [string]$ServiceName = "wellmonitor"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Deploying Enhanced Configuration Logging" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

try {
    # Build the project first
    Write-Host "📦 Building WellMonitor.Device project..." -ForegroundColor Yellow
    
    dotnet build src/WellMonitor.Device/WellMonitor.Device.csproj -c Release -r linux-arm64
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "✅ Build successful" -ForegroundColor Green
    
    # Stop the service
    Write-Host "⏸️ Stopping WellMonitor service on Raspberry Pi..." -ForegroundColor Yellow
    ssh $PiHost "sudo systemctl stop $ServiceName" 2>$null
    
    # Copy the updated binaries
    Write-Host "📋 Copying updated binaries to Raspberry Pi..." -ForegroundColor Yellow
    scp -r "src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/*" "${PiHost}:/opt/wellmonitor/"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to copy binaries to Raspberry Pi"
    }
    
    # Set correct permissions
    Write-Host "🔒 Setting correct permissions..." -ForegroundColor Yellow
    ssh $PiHost "sudo chown -R wellmonitor:wellmonitor /opt/wellmonitor/"
    ssh $PiHost "sudo chmod +x /opt/wellmonitor/WellMonitor.Device"
    
    # Start the service
    Write-Host "▶️ Starting WellMonitor service..." -ForegroundColor Yellow
    ssh $PiHost "sudo systemctl start $ServiceName"
    
    # Wait a moment for startup
    Start-Sleep -Seconds 3
    
    # Check service status
    Write-Host "📊 Checking service status..." -ForegroundColor Yellow
    ssh $PiHost "sudo systemctl status $ServiceName --no-pager -l"
    
    Write-Host ""
    Write-Host "📋 Recent logs with enhanced configuration logging:" -ForegroundColor Cyan
    ssh $PiHost "sudo journalctl -u $ServiceName --since '1 minute ago' --no-pager"
    
    Write-Host ""
    Write-Host "✅ Enhanced configuration logging deployed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🔍 Key improvements:" -ForegroundColor Yellow
    Write-Host "   • Detailed camera configuration logging with device twin vs default tracking" -ForegroundColor White
    Write-Host "   • Nested Camera property support (Camera.Gain, Camera.ShutterSpeedMicroseconds, etc.)" -ForegroundColor White
    Write-Host "   • Backward compatibility with legacy flat properties (cameraGain, etc.)" -ForegroundColor White
    Write-Host "   • Warnings for default values not found in device twin" -ForegroundColor White
    Write-Host "   • Warnings for potentially problematic camera settings" -ForegroundColor White
    Write-Host "   • Hourly configuration summary reports" -ForegroundColor White
    Write-Host "   • Enhanced device twin version tracking" -ForegroundColor White
    Write-Host ""
    Write-Host "📊 To monitor configuration logs in real-time:" -ForegroundColor Cyan
    Write-Host "   ssh $PiHost `"sudo journalctl -u $ServiceName -f`"" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔧 To trigger an immediate configuration update:" -ForegroundColor Cyan
    Write-Host "   Update the device twin in Azure IoT Hub - changes will be logged on next cycle" -ForegroundColor Gray
    
} catch {
    Write-Error "Deployment failed: $_"
    exit 1
}
