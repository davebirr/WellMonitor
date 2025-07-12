# Scripts Consolidation Plan

## Current State Analysis

**📊 Script Count:** 35+ scripts with significant overlap and redundancy
**🔍 Issues Identified:**
- Multiple scripts doing similar tasks (e.g., 4+ service setup scripts)
- Scattered diagnostic scripts that could be unified
- Legacy scripts from development iterations
- Inconsistent naming conventions (.sh vs .ps1 duplicates)
- No clear organization or entry points

## Script Categories Analysis

### 🔧 **Installation & Deployment (11 scripts → 3 scripts)**
**Current Scripts:**
- `install-wellmonitor-complete.sh` ✅ (keep - primary installer)
- `install-wellmonitor-secure.sh` ❌ (redundant - covered by complete)
- `prepare-secure-install.sh` ❌ (legacy)
- `deploy-to-pi.sh` ❌ (redundant)
- `Deploy-ToPi.ps1` ❌ (Windows version of above)
- `sync-and-run.sh` ✅ (keep - development workflow)
- `quick-sync.sh` ❌ (redundant with sync-and-run)
- `setup-wellmonitor-service.sh` ❌ (legacy)
- `setup-wellmonitor-service-improved.sh` ❌ (legacy)
- `install-tesseract-pi.sh` ❌ (covered in complete installer)
- `install-python-ocr.sh` ❌ (covered in complete installer)

**Consolidated Scripts:**
1. `install-wellmonitor.sh` (rename from complete, primary installer)
2. `sync-and-run.sh` (keep for development)
3. `build-and-deploy.ps1` (Windows development deployment)

### 🩺 **Diagnostics & Troubleshooting (8 scripts → 2 scripts)**
**Current Scripts:**
- `diagnose-service.sh` ✅ (keep as base)
- `diagnose-camera.sh` ❌ (merge into unified diagnostic)
- `diagnose-debug-image-path.sh` ❌ (merge)
- `Diagnose-DebugImagePath.ps1` ❌ (Windows duplicate)
- `debug-executable.sh` ❌ (merge)
- `debug-systemd.sh` ❌ (merge)
- `analyze-debug-images.sh` ❌ (merge)
- `test-ocr.sh` + `test-ocr.ps1` ❌ (merge)

**Consolidated Scripts:**
1. `diagnose-system.sh` (unified diagnostics)
2. `test-components.sh` (camera, OCR, database testing)

### ⚙️ **Configuration & Device Twin (7 scripts → 2 scripts)**
**Current Scripts:**
- `Setup-AzureCli.ps1` ✅ (keep - essential for Windows dev)
- `Update-LedCameraSettings.ps1` ❌ (merge into device twin manager)
- `Update-DeviceTwinOCR.ps1` ❌ (merge)
- `Update-DebugImagePath.ps1` ❌ (merge)
- `Test-LedCameraOptimization.ps1` ❌ (merge)
- `optimize-led-camera.sh` ❌ (merge)
- `fix-camera-settings.sh` ❌ (merge)

**Consolidated Scripts:**
1. `Setup-AzureCli.ps1` (keep - Windows Azure CLI setup)
2. `Manage-DeviceTwin.ps1` (unified device twin management)

### 🔧 **System Fixes & Maintenance (9 scripts → 2 scripts)**
**Current Scripts:**
- `fix-wellmonitor-service.sh` ❌ (legacy)
- `fix-script-permissions.sh` ❌ (one-time fix)
- `simple-debug-fix.sh` ❌ (legacy)
- `manual-debug-fix.sh` ❌ (legacy)
- `secure-home-access.sh` ❌ (security issue, should not exist)
- `update-debug-image-path.sh` ❌ (covered by device twin)
- `wellmonitor-service.sh` ❌ (legacy)
- `setup-git-hooks.sh` ❌ (development setup)
- `Cleanup-Documentation.ps1` ✅ (keep - utility)

**Consolidated Scripts:**
1. `system-maintenance.sh` (system fixes and maintenance)
2. `Cleanup-Documentation.ps1` (keep existing)

## Proposed Consolidated Structure

```
scripts/
├── installation/
│   ├── install-wellmonitor.sh          # Primary installer (renamed from complete)
│   ├── sync-and-run.sh                 # Development workflow  
│   └── build-and-deploy.ps1            # Windows development deployment
├── diagnostics/
│   ├── diagnose-system.sh              # Unified system diagnostics
│   └── test-components.sh              # Camera, OCR, database testing
├── configuration/
│   ├── Setup-AzureCli.ps1             # Windows Azure CLI setup
│   └── Manage-DeviceTwin.ps1          # Unified device twin management
├── maintenance/
│   ├── system-maintenance.sh          # System fixes and maintenance
│   └── Cleanup-Documentation.ps1      # Documentation cleanup (existing)
└── README.md                          # Scripts documentation and usage guide
```

## Implementation Plan

### Phase 1: Create New Consolidated Scripts
1. **installation/install-wellmonitor.sh** - Rename and enhance existing complete installer
2. **diagnostics/diagnose-system.sh** - Merge all diagnostic capabilities
3. **configuration/Manage-DeviceTwin.ps1** - Unified device twin operations
4. **scripts/README.md** - Complete usage documentation

### Phase 2: Remove Legacy Scripts (22 scripts to remove)
- All legacy installation scripts except primary
- All individual diagnostic scripts 
- All individual device twin scripts
- All legacy fix/debug scripts
- Duplicate .ps1/.sh versions

### Phase 3: Update Documentation
- Update installation guide references
- Update troubleshooting guide references  
- Create scripts usage documentation
- Update main README script references

## Benefits of Consolidation

### **User Experience**
- ✅ **Clear entry points** - 8 focused scripts vs 35+ scattered
- ✅ **Logical organization** - categorized by purpose
- ✅ **Reduced confusion** - no more guessing which script to use
- ✅ **Better discoverability** - organized directory structure

### **Maintenance**
- ✅ **77% reduction** in script count (35+ → 8)
- ✅ **Single source of truth** for each function
- ✅ **Easier updates** - fewer files to maintain
- ✅ **Better testing** - focused, comprehensive scripts

### **Development Workflow**
- ✅ **Clear development vs production** workflows
- ✅ **Comprehensive diagnostics** in single tool
- ✅ **Unified device twin management**
- ✅ **Better Windows/Linux separation**

## Script Feature Consolidation

### **Unified Diagnostics Features**
- System status checks
- Service diagnostics  
- Camera testing
- OCR validation
- Database connectivity
- Azure IoT Hub connection
- Debug image analysis
- Performance monitoring

### **Unified Device Twin Management**
- LED camera optimization
- OCR configuration
- Debug image path setup
- Configuration testing and validation
- Batch configuration updates
- Configuration backup/restore

### **Comprehensive Installation**
- System preparation
- Dependencies installation
- Secure service setup
- Configuration migration
- Service registration and startup
- Validation and testing

## Risk Mitigation

### **Backward Compatibility**
- Keep primary installer name recognizable
- Maintain key development workflows
- Document migration path for existing users
- Provide deprecation notices for removed scripts

### **Testing Strategy**
- Test each consolidated script thoroughly
- Validate all merged functionality works
- Test on both development and production environments
- Create rollback plan if issues found

## Next Steps

1. **Create consolidated scripts** with enhanced functionality
2. **Test consolidated scripts** on development environment
3. **Update documentation** to reference new scripts
4. **Remove legacy scripts** after validation
5. **Update CI/CD pipelines** if they reference old scripts

This consolidation will transform the scripts from a confusing collection of 35+ files into a focused, well-organized toolkit of 8 comprehensive scripts that follow industry best practices.
