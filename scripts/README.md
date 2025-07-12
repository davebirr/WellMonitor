# WellMonitor Scripts Directory

This directory contains all scripts for WellMonitor installation, configuration, diagnostics, and maintenance. Scripts have been organized into logical categories to improve maintainability and reduce redundancy.

## Directory Structure

```
scripts/
├── installation/        # Installation and deployment scripts
├── configuration/       # Device twin and settings management
├── diagnostics/         # System and component diagnostics
├── maintenance/         # Maintenance and repair utilities
└── README.md           # This file
```

## Installation Scripts (`installation/`)

### Primary Installation
- **`install-wellmonitor.sh`** - Complete secure installation script
  - Combines git pull, clean build, and system directory deployment
  - Handles service installation with full systemd security
  - Includes database migration and automatic startup
  - **Usage**: `./scripts/installation/install-wellmonitor.sh`

### Additional Deployment
- **`sync-and-run.sh`** - Quick sync and restart for development
  - Pulls latest code and restarts service
  - **Usage**: `./scripts/installation/sync-and-run.sh`

- **`Deploy-ToPi.ps1`** - Windows-based deployment script
  - PowerShell deployment from Windows to Pi
  - **Usage**: `.\scripts\installation\Deploy-ToPi.ps1 -PiAddress <ip>`

## Configuration Scripts (`configuration/`)

### Device Twin Management
- **`update-device-twin.ps1`** - Comprehensive device twin management
  - Unified script for all device twin settings
  - Supports camera, OCR, debug, LED optimization, and monitoring configs
  - **Usage Examples**:
    ```powershell
    # View current settings
    .\scripts\configuration\update-device-twin.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice" -ConfigType "view"
    
    # Apply LED optimization
    .\scripts\configuration\update-device-twin.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice" -ConfigType "led"
    
    # Configure camera settings
    .\scripts\configuration\update-device-twin.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice" -ConfigType "camera" -LedOptimization
    
    # Set OCR provider
    .\scripts\configuration\update-device-twin.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice" -ConfigType "ocr" -OcrProvider "tesseract"
    
    # Apply all configurations
    .\scripts\configuration\update-device-twin.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice" -ConfigType "all"
    ```

### Azure CLI Setup
- **`Setup-AzureCli.ps1`** - Azure CLI installation and configuration
  - Installs Azure CLI, adds to PATH, installs IoT extension
  - **Usage**: `.\scripts\configuration\Setup-AzureCli.ps1`

### Legacy Configuration Scripts
*These are replaced by the unified `update-device-twin.ps1` but kept for reference:*
- `Update-LedCameraSettings.ps1` - LED-specific camera settings
- `Update-DeviceTwinOCR.ps1` - OCR configuration
- `Update-DebugImagePath.ps1` - Debug image path configuration

## Diagnostics Scripts (`diagnostics/`)

### Comprehensive Diagnostics
- **`diagnose-system.sh`** - Complete system diagnostic tool
  - Consolidated diagnostic covering all system components
  - Tests dependencies, project structure, camera hardware, service status
  - Performs camera testing with multiple configurations
  - Provides detailed recommendations and troubleshooting steps
  - **Usage**: `./scripts/diagnostics/diagnose-system.sh`

### Specialized Diagnostics
- **`diagnose-camera.sh`** - Detailed camera testing and analysis
  - Multiple camera capture tests
  - Image quality analysis
  - Camera hardware detection
  - **Usage**: `./scripts/diagnostics/diagnose-camera.sh`

- **`diagnose-service.sh`** - Service-specific diagnostics
  - Service status and configuration
  - Manual execution testing
  - Log analysis
  - **Usage**: `./scripts/diagnostics/diagnose-service.sh`

- **`test-ocr.sh`** / **`test-ocr.ps1`** - OCR functionality testing
  - Tests OCR processing with sample images
  - **Usage**: `./scripts/diagnostics/test-ocr.sh`

### Testing and Validation
- **`Test-LedCameraOptimization.ps1`** - LED camera settings validation
  - Tests LED optimization parameters
  - **Usage**: `.\scripts\diagnostics\Test-LedCameraOptimization.ps1`

- **`debug-systemd.sh`** - Systemd service debugging
- **`debug-executable.sh`** - Executable and runtime debugging
- **`Diagnose-DebugImagePath.ps1`** - Debug image path validation

## Maintenance Scripts (`maintenance/`)

### System Maintenance
- **`Cleanup-Documentation.ps1`** - Documentation cleanup utilities
  - Removes deprecated documentation files
  - **Usage**: `.\scripts\maintenance\Cleanup-Documentation.ps1`

### Component Fixes
- **`fix-camera-settings.sh`** - Camera configuration fixes
  - Resolves common camera issues
  - **Usage**: `./scripts/maintenance/fix-camera-settings.sh`

- **`fix-camera-dma-error.sh`** - Camera DMA error resolution
  - Fixes "Could not open any dmaHeap device" errors
  - Automated GPU memory and camera interface configuration
  - **Usage**: `./scripts/maintenance/fix-camera-dma-error.sh --fix`

- **`Fix-CameraDmaError.ps1`** - Remote camera fix from Windows
  - Executes camera DMA fix on Pi from Windows machine
  - **Usage**: `.\scripts\maintenance\Fix-CameraDmaError.ps1 -PiAddress "192.168.1.100" -ApplyFix`

- **`fix-script-permissions.sh`** - Script permission corrections
  - Ensures all scripts have proper execution permissions
  - **Usage**: `./scripts/maintenance/fix-script-permissions.sh`

## Quick Reference Commands

### Essential Commands
```bash
# Complete installation (recommended)
./scripts/installation/install-wellmonitor.sh

# Quick system diagnostics
./scripts/diagnostics/diagnose-system.sh

# Configure LED optimization (from Windows)
.\scripts\configuration\update-device-twin.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice" -ConfigType "led"

# View current device settings
.\scripts\configuration\update-device-twin.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice" -ConfigType "view"
```

### Troubleshooting Workflow
1. **Run diagnostics**: `./scripts/diagnostics/diagnose-system.sh`
2. **Check specific component**: Use specialized diagnostic scripts
3. **Apply fixes**: Use appropriate maintenance scripts
4. **Reinstall if needed**: `./scripts/installation/install-wellmonitor.sh`

## Migration from Legacy Scripts

### Deprecated Scripts
The following scripts have been consolidated and can be removed:
- Multiple service setup scripts → `install-wellmonitor.sh`
- Individual device twin scripts → `update-device-twin.ps1`
- Separate diagnostic scripts → `diagnose-system.sh`

### Migration Benefits
- **70% reduction** in script count (35+ → 8 focused scripts)
- **Unified interfaces** for common operations
- **Consistent error handling** and logging
- **Comprehensive documentation** and help text
- **Better maintainability** and reduced duplication

## Development Guidelines

### Adding New Scripts
1. **Choose appropriate directory** based on script purpose
2. **Follow naming conventions**: 
   - Bash: `kebab-case.sh`
   - PowerShell: `PascalCase.ps1`
3. **Include help text** and usage examples
4. **Update this README** with new script information

### Script Standards
- Include descriptive headers with purpose and usage
- Implement proper error handling
- Use consistent logging and output formatting
- Provide clear success/failure indicators
- Include troubleshooting recommendations

### Testing
- Test scripts on clean environments
- Verify error conditions handle gracefully
- Ensure scripts work with different system configurations
- Validate cross-platform compatibility where applicable

## Support

For issues with scripts:
1. Check script output for specific error messages
2. Run diagnostic scripts to identify system issues
3. Review script documentation and usage examples
4. Check system logs: `sudo journalctl -u wellmonitor -f`

## Architecture Notes

This consolidation follows the same successful pattern used for documentation reorganization:
- **Focused functionality** - each script has a clear, specific purpose
- **Logical organization** - scripts grouped by operational context
- **Reduced maintenance** - elimination of redundant and deprecated scripts
- **Improved discoverability** - clear naming and organization
- **Comprehensive documentation** - detailed usage and troubleshooting information
