#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy latest WellMonitor changes to Raspberry Pi

.DESCRIPTION
    This script pulls the latest changes on the Raspberry Pi, builds the project,
    and restarts the service. It also provides status information and monitoring commands.

.PARAMETER DeviceHost
    IP address or hostname of the Raspberry Pi (default: 192.168.7.44)

.PARAMETER DeviceUser
    SSH username for the Raspberry Pi (default: davidb)

.EXAMPLE
    .\sync-to-device.ps1
    .\sync-to-device.ps1 -DeviceHost 192.168.1.100

.NOTES
    Requires SSH access to the Raspberry Pi and git/dotnet installed on the device.
#>

param(
    [string]$DeviceHost = "192.168.7.44",
    [string]$DeviceUser = "davidb",
    [string]$RemotePath = "/home/davidb/wellmonitor",
    [string]$BuildConfig = "Release"
)

Write-Host "🚀 Syncing WellMonitor changes to Raspberry Pi..." -ForegroundColor Green
Write-Host "📡 Target: $DeviceUser@$DeviceHost" -ForegroundColor Cyan

# Check if we can reach the device
Write-Host "📡 Checking device connectivity..." -ForegroundColor Yellow
try {
    $ping = Test-Connection -ComputerName $DeviceHost -Count 1 -Quiet
    if (-not $ping) {
        throw "Device not reachable"
    }
    Write-Host "✅ Device is reachable" -ForegroundColor Green
} catch {
    Write-Error "❌ Cannot reach device at $DeviceHost"
    Write-Host "💡 Please check:" -ForegroundColor Yellow
    Write-Host "   • Device is powered on and connected to network" -ForegroundColor White
    Write-Host "   • IP address is correct" -ForegroundColor White
    Write-Host "   • SSH is enabled on the device" -ForegroundColor White
    exit 1
}

# Pull latest changes on the device
Write-Host "📥 Pulling latest changes on device..." -ForegroundColor Yellow
try {
    ssh "$DeviceUser@$DeviceHost" "cd $RemotePath && git pull origin main"
    Write-Host "✅ Git pull completed" -ForegroundColor Green
} catch {
    Write-Error "❌ Failed to pull changes: $_"
    exit 1
}

# Build the project on the device
Write-Host "🔨 Building project on device..." -ForegroundColor Yellow
try {
    ssh "$DeviceUser@$DeviceHost" "cd $RemotePath && dotnet build -c $BuildConfig"
    Write-Host "✅ Build completed" -ForegroundColor Green
} catch {
    Write-Error "❌ Build failed: $_"
    exit 1
}

# Restart the service to pick up changes
Write-Host "🔄 Restarting WellMonitor service..." -ForegroundColor Yellow
try {
    ssh "$DeviceUser@$DeviceHost" "sudo systemctl restart wellmonitor.service"
    Start-Sleep -Seconds 3  # Give service time to start
    Write-Host "✅ Service restarted" -ForegroundColor Green
} catch {
    Write-Error "❌ Failed to restart service: $_"
    exit 1
}

# Check service status
Write-Host "🔍 Checking service status..." -ForegroundColor Yellow
ssh "$DeviceUser@$DeviceHost" "sudo systemctl status wellmonitor.service --no-pager -l"

Write-Host ""
Write-Host "✅ Deployment complete!" -ForegroundColor Green
Write-Host ""
Write-Host "🔍 To monitor the service:" -ForegroundColor Cyan
Write-Host "   ssh $DeviceUser@$DeviceHost" -ForegroundColor White
Write-Host "   sudo journalctl -u wellmonitor.service -f" -ForegroundColor White
Write-Host ""
Write-Host "🖼️  To check debug images:" -ForegroundColor Cyan
Write-Host "   ssh $DeviceUser@$DeviceHost" -ForegroundColor White
Write-Host "   sudo ls -la /var/lib/wellmonitor/debug_images/" -ForegroundColor White
Write-Host ""
Write-Host "📊 To check device twin sync:" -ForegroundColor Cyan
Write-Host "   ssh $DeviceUser@$DeviceHost" -ForegroundColor White
Write-Host "   ./scripts/diagnostics/troubleshoot-device-twin-sync.sh" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Expected results:" -ForegroundColor Yellow
Write-Host "   • Device twin sync should work (no more disconnected state)" -ForegroundColor White
Write-Host "   • Camera properties should be recognized (no more 'unknown property' warnings)" -ForegroundColor White
Write-Host "   • Debug images should be properly exposed (no more white images)" -ForegroundColor White
Write-Host "   • Red LED digits should be clearly visible for OCR" -ForegroundColor White
