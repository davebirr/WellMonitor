# Camera DMA Error Fix - Remote Execution Script
# Runs camera fix script on Raspberry Pi from Windows

param(
    [Parameter(Mandatory=$true)]
    [string]$PiAddress,
    
    [Parameter(Mandatory=$false)]
    [string]$PiUser = "davidb",
    
    [Parameter(Mandatory=$false)]
    [switch]$ApplyFix = $false
)

Write-Host "🔧 WellMonitor Camera DMA Fix - Remote Execution" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host
Write-Host "Connecting to: $PiUser@$PiAddress" -ForegroundColor Cyan
Write-Host

if ($ApplyFix) {
    Write-Host "⚠️  WARNING: This will apply fixes and may reboot the Pi!" -ForegroundColor Yellow
    $confirm = Read-Host "Continue? (y/N)"
    if ($confirm -ne "y" -and $confirm -ne "Y") {
        Write-Host "Operation cancelled" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "🔧 Applying camera fixes..." -ForegroundColor Yellow
    ssh "$PiUser@$PiAddress" "cd ~/WellMonitor && ./scripts/maintenance/fix-camera-dma-error.sh --fix"
} else {
    Write-Host "🔍 Running camera diagnostics..." -ForegroundColor Yellow
    ssh "$PiUser@$PiAddress" "cd ~/WellMonitor && ./scripts/maintenance/fix-camera-dma-error.sh"
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Camera fix script completed successfully" -ForegroundColor Green
    Write-Host
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Monitor service logs: ssh $PiUser@$PiAddress 'sudo journalctl -u wellmonitor -f'" -ForegroundColor White
    Write-Host "2. Check for new debug images in ~30 seconds" -ForegroundColor White
    Write-Host "3. Run diagnostics: .\scripts\diagnostics\Test-RemoteCamera.ps1 -PiAddress $PiAddress" -ForegroundColor White
} else {
    Write-Host "`n❌ Camera fix script encountered errors" -ForegroundColor Red
    Write-Host "Check the output above for specific issues" -ForegroundColor Yellow
}
