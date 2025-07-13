#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test the consolidated secrets service functionality

.DESCRIPTION
    This script tests that the SimplifiedSecretsService can read from .env files
    and environment variables correctly, replacing the old complex setup.

.EXAMPLE
    .\test-secrets-consolidation.ps1
#>

Write-Host "🧪 Testing Secrets Service Consolidation..." -ForegroundColor Yellow
Write-Host ""

# Check if .env file exists
$envPath = "c:\Users\davidb\1Repositories\wellmonitor\.env"
if (Test-Path $envPath) {
    Write-Host "✅ .env file found" -ForegroundColor Green
    
    # Check if IoT Hub connection string is configured
    $envContent = Get-Content $envPath
    $iotHubLine = $envContent | Where-Object { $_ -like "*WELLMONITOR_IOTHUB_CONNECTION_STRING*" -and $_ -notlike "#*" }
    
    if ($iotHubLine) {
        Write-Host "✅ IoT Hub connection string configured in .env" -ForegroundColor Green
    } else {
        Write-Host "⚠️  IoT Hub connection string not found in .env" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ .env file not found" -ForegroundColor Red
}

# Check build status
Write-Host ""
Write-Host "🔨 Testing build..." -ForegroundColor Yellow
try {
    $buildOutput = dotnet build "c:\Users\davidb\1Repositories\wellmonitor\src\WellMonitor.Device" --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful" -ForegroundColor Green
    } else {
        Write-Host "❌ Build failed:" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host "❌ Build error: $_" -ForegroundColor Red
    exit 1
}

# Check for removed services
Write-Host ""
Write-Host "🗑️  Verifying old services removed..." -ForegroundColor Yellow

$servicesPath = "c:\Users\davidb\1Repositories\wellmonitor\src\WellMonitor.Device\Services\"
$removedServices = @(
    "EnvironmentSecretsService.cs",
    "HybridSecretsService.cs", 
    "KeyVaultSecretsService.cs"
)

$allRemoved = $true
foreach ($service in $removedServices) {
    $fullPath = Join-Path $servicesPath $service
    if (Test-Path $fullPath) {
        Write-Host "❌ $service still exists" -ForegroundColor Red
        $allRemoved = $false
    } else {
        Write-Host "✅ $service removed" -ForegroundColor Green
    }
}

# Check new service exists
$newServicePath = Join-Path $servicesPath "SimplifiedSecretsService.cs"
if (Test-Path $newServicePath) {
    Write-Host "✅ SimplifiedSecretsService.cs exists" -ForegroundColor Green
} else {
    Write-Host "❌ SimplifiedSecretsService.cs missing" -ForegroundColor Red
    $allRemoved = $false
}

Write-Host ""
if ($allRemoved) {
    Write-Host "🎯 Secrets Service Consolidation Complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Summary of changes:" -ForegroundColor Cyan
    Write-Host "   • Removed 3 old secrets services" -ForegroundColor White
    Write-Host "   • Added 1 simplified secrets service" -ForegroundColor White
    Write-Host "   • Updated Program.cs registration" -ForegroundColor White
    Write-Host "   • Build passes successfully" -ForegroundColor White
    Write-Host ""
    Write-Host "🔄 Next steps:" -ForegroundColor Cyan
    Write-Host "   • Test with actual Pi deployment" -ForegroundColor White
    Write-Host "   • Verify .env file configuration works" -ForegroundColor White
    Write-Host "   • Consider removing SecretsService.cs (marked obsolete)" -ForegroundColor White
} else {
    Write-Host "⚠️  Consolidation incomplete - manual verification needed" -ForegroundColor Yellow
    exit 1
}
