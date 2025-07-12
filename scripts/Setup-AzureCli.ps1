# Azure CLI Setup Script
# Fixes PATH issues and validates Azure CLI installation

Write-Host "🔧 Azure CLI Setup and Validation" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host

# Check if Azure CLI is installed
$azureCliPaths = @(
    "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd",
    "C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
)

$azPath = $null
foreach ($path in $azureCliPaths) {
    if (Test-Path $path) {
        $azPath = $path
        Write-Host "✅ Found Azure CLI at: $path" -ForegroundColor Green
        break
    }
}

if (-not $azPath) {
    Write-Host "❌ Azure CLI not found. Installing..." -ForegroundColor Red
    Write-Host "Running: winget install -e --id Microsoft.AzureCLI" -ForegroundColor Yellow
    
    try {
        winget install -e --id Microsoft.AzureCLI
        Write-Host "✅ Azure CLI installed successfully" -ForegroundColor Green
        
        # Check again after installation
        foreach ($path in $azureCliPaths) {
            if (Test-Path $path) {
                $azPath = $path
                break
            }
        }
    }
    catch {
        Write-Host "❌ Failed to install Azure CLI: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Please install manually from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
        exit 1
    }
}

# Add to current session PATH
$azDir = Split-Path $azPath -Parent
if ($env:PATH -notlike "*$azDir*") {
    $env:PATH += ";$azDir"
    Write-Host "✅ Added Azure CLI to current session PATH" -ForegroundColor Green
}

# Test Azure CLI
Write-Host
Write-Host "Testing Azure CLI..." -ForegroundColor Yellow
try {
    $version = & $azPath --version 2>&1 | Select-String "azure-cli" | ForEach-Object { $_.ToString().Trim() }
    Write-Host "✅ Azure CLI working: $version" -ForegroundColor Green
}
catch {
    Write-Host "❌ Azure CLI test failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Check if user is logged in
Write-Host
Write-Host "Checking Azure authentication..." -ForegroundColor Yellow
try {
    $account = & $azPath account show 2>&1
    if ($LASTEXITCODE -eq 0) {
        $accountInfo = $account | ConvertFrom-Json
        Write-Host "✅ Logged in as: $($accountInfo.user.name)" -ForegroundColor Green
        Write-Host "   Subscription: $($accountInfo.name)" -ForegroundColor White
    } else {
        Write-Host "⚠️  Not logged in to Azure" -ForegroundColor Yellow
        Write-Host "Run: az login" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "⚠️  Could not check Azure login status" -ForegroundColor Yellow
    Write-Host "Run: az login" -ForegroundColor Cyan
}

# Add to permanent PATH (requires elevated privileges)
Write-Host
Write-Host "Adding Azure CLI to permanent PATH..." -ForegroundColor Yellow

try {
    # Get current user PATH
    $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    
    if ($userPath -notlike "*$azDir*") {
        [Environment]::SetEnvironmentVariable("PATH", "$userPath;$azDir", "User")
        Write-Host "✅ Added Azure CLI to permanent user PATH" -ForegroundColor Green
        Write-Host "   Restart your terminal or IDE to use 'az' command directly" -ForegroundColor Yellow
    } else {
        Write-Host "✅ Azure CLI already in permanent PATH" -ForegroundColor Green
    }
}
catch {
    Write-Host "⚠️  Could not update permanent PATH: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   You may need to restart as administrator or manually add to PATH" -ForegroundColor Yellow
}

Write-Host
Write-Host "🎯 Setup Summary:" -ForegroundColor Green
Write-Host "• Azure CLI Path: $azPath" -ForegroundColor White
Write-Host "• Current Session: Ready to use 'az' commands" -ForegroundColor White
Write-Host "• Permanent PATH: Updated (restart terminal to use globally)" -ForegroundColor White
Write-Host
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. If not logged in: az login" -ForegroundColor White
Write-Host "2. Test LED camera scripts: .\scripts\Update-LedCameraSettings.ps1" -ForegroundColor White
Write-Host "3. Run optimization: .\scripts\Test-LedCameraOptimization.ps1" -ForegroundColor White

Write-Host
Write-Host "✅ Azure CLI setup complete!" -ForegroundColor Green
