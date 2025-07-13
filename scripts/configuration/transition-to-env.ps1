#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Transition from secrets.json to .env file for configuration

.DESCRIPTION
    This script helps transition the project from using secrets.json to using .env files:
    - Removes secrets.json from the project
    - Updates .gitignore to ignore .env files
    - Provides guidance on configuration changes needed

.EXAMPLE
    .\transition-to-env.ps1

.NOTES
    This script should be run from the repository root directory.
    The .env file should already be created and configured before running this script.
#>

$repositoryRoot = Split-Path (Split-Path (Get-Location) -Parent) -Parent

Write-Host "🔄 Transitioning from secrets.json to .env configuration..." -ForegroundColor Yellow
Write-Host "Repository root: $repositoryRoot" -ForegroundColor Gray

# Check if .env file exists
$envPath = Join-Path $repositoryRoot ".env"
if (-not (Test-Path $envPath)) {
    Write-Error ".env file not found at $envPath. Please create it first."
    exit 1
}

Write-Host "✅ .env file found" -ForegroundColor Green

# Remove secrets.json if it exists
$secretsPath = Join-Path $repositoryRoot "src\WellMonitor.Device\secrets.json"
if (Test-Path $secretsPath) {
    Write-Host "🗑️  Removing secrets.json..." -ForegroundColor Yellow
    Remove-Item $secretsPath -Force
    Write-Host "✅ secrets.json removed" -ForegroundColor Green
} else {
    Write-Host "ℹ️  secrets.json not found (already removed)" -ForegroundColor Blue
}

# Update .gitignore to ensure .env is ignored
$gitignorePath = Join-Path $repositoryRoot ".gitignore"
if (Test-Path $gitignorePath) {
    $gitignoreContent = Get-Content $gitignorePath
    
    # Check if .env is already ignored
    if ($gitignoreContent -notcontains ".env") {
        Write-Host "📝 Adding .env to .gitignore..." -ForegroundColor Yellow
        Add-Content $gitignorePath "`n# Environment variables (sensitive data)`n.env"
        Write-Host "✅ .env added to .gitignore" -ForegroundColor Green
    } else {
        Write-Host "ℹ️  .env already in .gitignore" -ForegroundColor Blue
    }
} else {
    Write-Host "⚠️  .gitignore not found" -ForegroundColor Yellow
}

# Check for any remaining references to secrets.json in code
Write-Host "🔍 Checking for remaining secrets.json references..." -ForegroundColor Yellow

$remainingReferences = @()

# Search in C# files
$csharpFiles = Get-ChildItem $repositoryRoot -Recurse -Include "*.cs" | 
    Where-Object { $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" }

foreach ($file in $csharpFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "secrets\.json") {
        $remainingReferences += $file.FullName
    }
}

# Search in PowerShell files
$psFiles = Get-ChildItem $repositoryRoot -Recurse -Include "*.ps1"
foreach ($file in $psFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "secrets\.json") {
        $remainingReferences += $file.FullName
    }
}

if ($remainingReferences.Count -gt 0) {
    Write-Host "⚠️  Found remaining references to secrets.json:" -ForegroundColor Yellow
    foreach ($ref in $remainingReferences) {
        Write-Host "   $ref" -ForegroundColor Gray
    }
    Write-Host "📝 These files may need manual updates to use environment variables instead." -ForegroundColor Yellow
} else {
    Write-Host "✅ No remaining references to secrets.json found" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎯 Transition to .env configuration complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Summary of changes:" -ForegroundColor Cyan
Write-Host "   • secrets.json removed from project" -ForegroundColor White
Write-Host "   • .env file is the new configuration source" -ForegroundColor White
Write-Host "   • .env added to .gitignore (if not already present)" -ForegroundColor White
Write-Host ""
Write-Host "🔐 Security benefits:" -ForegroundColor Cyan
Write-Host "   • .env files are not committed to version control" -ForegroundColor White
Write-Host "   • Standard practice for environment-specific configuration" -ForegroundColor White
Write-Host "   • Better separation of configuration from code" -ForegroundColor White
Write-Host ""
Write-Host "🔄 Next steps:" -ForegroundColor Cyan
Write-Host "   • Ensure all team members have their own .env file" -ForegroundColor White
Write-Host "   • Update deployment processes to use environment variables" -ForegroundColor White
Write-Host "   • Consider using Azure Key Vault for production secrets" -ForegroundColor White

if ($remainingReferences.Count -gt 0) {
    Write-Host ""
    Write-Host "⚠️  Action required: Review and update files that still reference secrets.json" -ForegroundColor Yellow
}
