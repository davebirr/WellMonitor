#!/usr/bin/env pwsh

<#
.SYNOPSIS
Diagnose debug image path configuration on Raspberry Pi

.DESCRIPTION
This script checks the current debug image path configuration and shows
where files would be saved based on the current app directory structure.
#>

Write-Host "🔍 Debug Image Path Diagnostics" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

# Show current working directory
Write-Host "Current Working Directory:" -ForegroundColor Yellow
Write-Host "  $PWD" -ForegroundColor White
Write-Host ""

# Show expected app directory structure
Write-Host "Expected App Structure:" -ForegroundColor Yellow
Write-Host "  ~/WellMonitor                                    (repo root)" -ForegroundColor Gray
Write-Host "  ~/WellMonitor/src/WellMonitor.Device             (project root)" -ForegroundColor Gray  
Write-Host "  ~/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/  (app base dir)" -ForegroundColor Gray
Write-Host "  ~/WellMonitor/src/WellMonitor.Device/debug_images          (desired debug dir)" -ForegroundColor Green
Write-Host ""

# Show what relative paths would resolve to
$baseDir = "/home/davidb/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0"
$relativePath = "debug_images"
$absolutePath = "/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"

Write-Host "Path Resolution:" -ForegroundColor Yellow
Write-Host "  App Base Directory: $baseDir" -ForegroundColor White
Write-Host "  Relative Path 'debug_images' would resolve to:" -ForegroundColor Gray
Write-Host "    $baseDir/$relativePath" -ForegroundColor Red
Write-Host "  Absolute Path (recommended):" -ForegroundColor Gray  
Write-Host "    $absolutePath" -ForegroundColor Green
Write-Host ""

# Check if directories exist
Write-Host "Directory Status:" -ForegroundColor Yellow
$projectDir = "/home/davidb/WellMonitor/src/WellMonitor.Device"
$desiredDebugDir = "$projectDir/debug_images"
$currentRelativeDir = "$baseDir/$relativePath"

if (Test-Path $projectDir) {
    Write-Host "  ✅ Project directory exists: $projectDir" -ForegroundColor Green
} else {
    Write-Host "  ❌ Project directory missing: $projectDir" -ForegroundColor Red
}

if (Test-Path $desiredDebugDir) {
    Write-Host "  ✅ Desired debug directory exists: $desiredDebugDir" -ForegroundColor Green
} else {
    Write-Host "  ⚠️  Desired debug directory missing: $desiredDebugDir" -ForegroundColor Yellow
    Write-Host "     (Will be created automatically when needed)" -ForegroundColor Gray
}

if (Test-Path $currentRelativeDir) {
    Write-Host "  ⚠️  Current relative path exists: $currentRelativeDir" -ForegroundColor Yellow
    Write-Host "     (This may be where debug images are currently saved)" -ForegroundColor Gray
} else {
    Write-Host "  ℹ️  Current relative path doesn't exist: $currentRelativeDir" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Recommendation:" -ForegroundColor Cyan
Write-Host "  Run: ./scripts/Update-DebugImagePath.ps1" -ForegroundColor Green
Write-Host "  This will set cameraDebugImagePath to the absolute path:" -ForegroundColor Gray
Write-Host "  $absolutePath" -ForegroundColor Green
