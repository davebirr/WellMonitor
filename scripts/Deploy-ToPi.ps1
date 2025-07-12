# Well Monitor Pi Deployment Script (PowerShell)
# This script builds and deploys the WellMonitor.Device application to Raspberry Pi

param(
    [string]$PiUser = "davidb",
    [string]$PiHost = "rpi4b-1407well01",
    [string]$PiPath = "/home/davidb/WellMonitor",
    [string]$ServiceName = "wellmonitor"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Well Monitor Pi Deployment Starting..." -ForegroundColor Green

# Build project
Write-Host "📦 Building project..." -ForegroundColor Yellow
Set-Location "src\WellMonitor.Device"
dotnet publish -c Release -o .\publish --self-contained false

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Stop service
Write-Host "🔄 Stopping service on Pi..." -ForegroundColor Yellow
ssh "${PiUser}@${PiHost}" "sudo systemctl stop ${ServiceName} || true"

# Deploy files
Write-Host "📤 Deploying files to Pi..." -ForegroundColor Yellow
# Using scp for Windows compatibility
scp -r .\publish\* "${PiUser}@${PiHost}:${PiPath}/"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ File deployment failed!" -ForegroundColor Red
    exit 1
}

# Set permissions
Write-Host "🔧 Setting permissions..." -ForegroundColor Yellow
ssh "${PiUser}@${PiHost}" "chmod +x ${PiPath}/WellMonitor.Device"

# Start service
Write-Host "🔄 Starting service on Pi..." -ForegroundColor Yellow
ssh "${PiUser}@${PiHost}" "sudo systemctl start ${ServiceName}"

# Check status
Write-Host "📊 Checking service status..." -ForegroundColor Yellow
ssh "${PiUser}@${PiHost}" "sudo systemctl status ${ServiceName} --no-pager"

# Show recent logs
Write-Host "📋 Recent logs..." -ForegroundColor Yellow
ssh "${PiUser}@${PiHost}" "sudo journalctl -u ${ServiceName} -n 20 --no-pager"

Write-Host "✅ Deployment complete!" -ForegroundColor Green
Write-Host ""
Write-Host "🔍 To monitor logs: ssh ${PiUser}@${PiHost} 'sudo journalctl -u ${ServiceName} -f'" -ForegroundColor Cyan
Write-Host "🛠️  To check status: ssh ${PiUser}@${PiHost} 'sudo systemctl status ${ServiceName}'" -ForegroundColor Cyan

Set-Location "..\..\"
