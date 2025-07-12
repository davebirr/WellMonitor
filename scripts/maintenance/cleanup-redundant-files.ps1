# WellMonitor Scripts and Documentation Cleanup
# Removes redundant files after consolidation

param(
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Scripts = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Docs = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$All = $false
)

$ErrorActionPreference = "Stop"

# Colors for output
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"

function Write-Header {
    param([string]$Text)
    Write-Host "`n🧹 $Text" -ForegroundColor $Green
    Write-Host ("=" * ($Text.Length + 3)) -ForegroundColor $Green
}

function Write-Action {
    param([string]$Action, [string]$File)
    if ($WhatIf) {
        Write-Host "WOULD $Action`: $File" -ForegroundColor $Yellow
    } else {
        Write-Host "$Action`: $File" -ForegroundColor $Cyan
    }
}

function Remove-FileIfExists {
    param([string]$FilePath, [string]$Reason)
    
    if (Test-Path $FilePath) {
        Write-Action "REMOVE" "$FilePath - $Reason"
        if (-not $WhatIf) {
            Remove-Item $FilePath -Force
        }
        return $true
    }
    return $false
}

# Get repository root
$RepoRoot = "c:\Users\davidb\1Repositories\wellmonitor"
$ScriptsRoot = Join-Path $RepoRoot "scripts"
$DocsRoot = Join-Path $RepoRoot "docs"

if ($WhatIf) {
    Write-Header "DRY RUN - What Would Be Removed"
} else {
    Write-Header "Cleaning Up Redundant Files"
}

$removedCount = 0

# Clean up scripts directory
if ($Scripts -or $All) {
    Write-Header "Scripts Directory Cleanup"
    
    # Installation scripts - now replaced by installation/install-wellmonitor.sh
    $installationScripts = @(
        "setup-wellmonitor-service.sh",
        "setup-wellmonitor-service-improved.sh", 
        "install-wellmonitor-secure.sh",
        "wellmonitor-service.sh",
        "deploy-to-pi.sh"
    )
    
    foreach ($script in $installationScripts) {
        $path = Join-Path $ScriptsRoot $script
        if (Remove-FileIfExists $path "Replaced by installation/install-wellmonitor.sh") {
            $removedCount++
        }
    }
    
    # Diagnostic scripts - now replaced by diagnostics/diagnose-system.sh  
    $diagnosticScripts = @(
        "simple-debug-fix.sh",
        "manual-debug-fix.sh",
        "analyze-debug-images.sh"
    )
    
    foreach ($script in $diagnosticScripts) {
        $path = Join-Path $ScriptsRoot $script
        if (Remove-FileIfExists $path "Replaced by diagnostics/diagnose-system.sh") {
            $removedCount++
        }
    }
    
    # Configuration scripts - now replaced by configuration/update-device-twin.ps1
    $configScripts = @(
        "update-debug-image-path.sh",
        "optimize-led-camera.sh"
    )
    
    foreach ($script in $configScripts) {
        $path = Join-Path $ScriptsRoot $script
        if (Remove-FileIfExists $path "Replaced by configuration/update-device-twin.ps1") {
            $removedCount++
        }
    }
    
    # Maintenance/setup scripts - now organized in appropriate directories
    $maintenanceScripts = @(
        "fix-wellmonitor-service.sh",
        "prepare-secure-install.sh",
        "secure-home-access.sh",
        "quick-sync.sh",
        "setup-git-hooks.sh"
    )
    
    foreach ($script in $maintenanceScripts) {
        $path = Join-Path $ScriptsRoot $script
        if (Remove-FileIfExists $path "Functionality moved to organized directories") {
            $removedCount++
        }
    }
    
    # Installation dependencies - can be removed as they're handled by main installer
    $dependencyScripts = @(
        "install-tesseract-pi.sh",
        "install-python-ocr.sh"
    )
    
    foreach ($script in $dependencyScripts) {
        $path = Join-Path $ScriptsRoot $script
        if (Remove-FileIfExists $path "Dependencies handled by main installation script") {
            $removedCount++
        }
    }
}

# Clean up docs directory
if ($Docs -or $All) {
    Write-Header "Documentation Directory Cleanup"
    
    # Legacy documentation files - now consolidated into organized guides
    $legacyDocs = @(
        "RaspberryPi-Camera-Setup.md",
        "RaspberryPiDatabaseTesting.md", 
        "RaspberryPiDeploymentGuide.md",
        "REORGANIZATION_PLAN.md"
    )
    
    foreach ($doc in $legacyDocs) {
        $path = Join-Path $DocsRoot $doc
        if (Remove-FileIfExists $path "Content consolidated into organized guides") {
            $removedCount++
        }
    }
}

# Summary
Write-Header "Cleanup Summary"

if ($removedCount -gt 0) {
    if ($WhatIf) {
        Write-Host "Would remove $removedCount redundant files" -ForegroundColor $Yellow
        Write-Host "`nTo actually perform cleanup, run without -WhatIf flag:" -ForegroundColor $Cyan
        Write-Host "  .\scripts\maintenance\cleanup-redundant-files.ps1 -All" -ForegroundColor $Cyan
    } else {
        Write-Host "Successfully removed $removedCount redundant files" -ForegroundColor $Green
        Write-Host "`nFiles have been cleaned up. The organized directory structure now contains:" -ForegroundColor $Cyan
        Write-Host "• scripts/installation/ - 3 focused installation scripts" -ForegroundColor $Cyan
        Write-Host "• scripts/configuration/ - 4 device twin and settings scripts" -ForegroundColor $Cyan  
        Write-Host "• scripts/diagnostics/ - 8 comprehensive diagnostic tools" -ForegroundColor $Cyan
        Write-Host "• scripts/maintenance/ - 3 maintenance utilities" -ForegroundColor $Cyan
        Write-Host "• docs/deployment/ - Installation and service management guides" -ForegroundColor $Cyan
        Write-Host "• docs/configuration/ - Device twin and settings guides" -ForegroundColor $Cyan
        Write-Host "• docs/development/ - Development and testing guides" -ForegroundColor $Cyan
        Write-Host "• docs/reference/ - API and technical reference" -ForegroundColor $Cyan
    }
} else {
    Write-Host "No redundant files found to remove" -ForegroundColor $Yellow
}

Write-Host "`nConsolidation Benefits:" -ForegroundColor $Green
Write-Host "• Reduced maintenance overhead through elimination of duplication" -ForegroundColor $Cyan
Write-Host "• Improved discoverability with logical organization" -ForegroundColor $Cyan
Write-Host "• Enhanced usability with consistent interfaces" -ForegroundColor $Cyan
Write-Host "• Better quality through unified error handling and documentation" -ForegroundColor $Cyan
