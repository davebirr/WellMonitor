# Remove legacy documentation files that have been consolidated

# Legacy files in root directory
$legacyRootFiles = @(
    "OCR-Integration-Complete-Summary.md",
    "DEVICE_TWIN_VALIDATION_TESTS_SUMMARY.md", 
    "DEVICE_TWIN_VALIDATION_SUMMARY.md",
    "DEBUG_IMAGE_TROUBLESHOOTING.md"
)

# Legacy files in docs directory  
$legacyDocsFiles = @(
    "Pi-Deployment-Guide.md",
    "Pi-OCR-Troubleshooting.md",
    "Pi-Dependency-Fix.md", 
    "Pi-ARM64-OCR-Solutions.md",
    "OrderlyStartup.md",
    "PiDevelopmentWorkflow.md",
    "OCR-Monitoring-Integration.md",
    "OCR-Integration-Complete.md",
    "OCR-Implementation.md",
    "OCR-Implementation-Complete.md",
    "Enhanced-Device-Twin-Configuration.md",
    "DeviceTwinExtendedConfiguration.md",
    "PythonOcrBridge.md",
    "DeviceTwinCameraConfigurationTesting.md",
    "DeviceTwinCameraConfiguration.md",
    "DeviceTwin-OCR-Configuration.md",
    "ToDos.md",
    "SQLiteDatabaseImplementation.md",
    "service-management.md",
    "SecretsManagement.md",
    "ScriptPermissionsFix.md",
    "REORGANIZATION_PLAN.md",
    "Raspberry Pi 4 Azure IoT Setup Guide.md",
    "QuickStart.md",
    "ProgressLog.md",
    "pi-diagnostics.md",
    "PiSetupSoftwareInstall.md",
    "PiHardwareSetup.md",
    "PiDeploymentLiveConfiguration.md",
    "PiCompleteDeveloperSetup.md",
    "implementation-notes.md",
    "deployment-guide.md",
    "DeviceTwinExample.json",
    "DataModel.md",
    "DataLoggingAndSync.md",
    "DataModel.sqlite.sql",
    "contributing.md"
)

Write-Host "Removing legacy documentation files..." -ForegroundColor Yellow

# Remove root legacy files
foreach ($file in $legacyRootFiles) {
    $path = $file
    if (Test-Path $path) {
        Remove-Item $path -Force
        Write-Host "Removed: $file" -ForegroundColor Green
    } else {
        Write-Host "Not found: $file" -ForegroundColor Gray
    }
}

# Remove docs legacy files
foreach ($file in $legacyDocsFiles) {
    $path = "docs\$file"
    if (Test-Path $path) {
        Remove-Item $path -Force  
        Write-Host "Removed: docs\$file" -ForegroundColor Green
    } else {
        Write-Host "Not found: docs\$file" -ForegroundColor Gray
    }
}

Write-Host "`nDocumentation consolidation complete!" -ForegroundColor Cyan
Write-Host "New structure:"
Write-Host "  docs/README.md - Main documentation index"
Write-Host "  docs/deployment/ - Installation, service management, troubleshooting"  
Write-Host "  docs/configuration/ - Device twin, camera, OCR, Azure setup"
Write-Host "  docs/development/ - Development setup, testing, architecture"
Write-Host "  docs/reference/ - API docs, data models, hardware specs"
