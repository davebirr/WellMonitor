# Camera Exposure Fix Summary

## Issue Identified
The WellMonitor application was failing with camera capture errors:
```
ERROR: *** Invalid exposure mode:off ***
System.InvalidOperationException: Both libcamera-still and rpicam-still failed to capture image
```

## Root Cause
The `CameraService.cs` was using `--exposure off` which is **NOT a valid exposure mode** for libcamera-still/rpicam-still.

Valid exposure modes include:
- `normal` (default auto-exposure)
- `barcode` (high contrast for reading text/numbers - **ideal for LED displays**)
- `sport`, `night`, `candlelight`, etc.

**Invalid modes:**
- `off` (this was causing the failure)

## Fix Applied

### CameraService.cs Changes
**Before:**
```csharp
args.Add("--exposure");
args.Add("off");  // ❌ INVALID - causes ERROR: *** Invalid exposure mode:off ***
```

**After:**
```csharp
if (cameraOptions.ShutterSpeedMicroseconds > 0)
{
    // Use 'barcode' mode for manual shutter - optimal for LED displays
    args.Add("--exposure");
    args.Add("barcode");  // ✅ VALID - perfect for LED displays
}
else if (!cameraOptions.AutoExposure)
{
    // Use 'normal' mode as safe fallback
    args.Add("--exposure");
    args.Add("normal");   // ✅ VALID - always works
}
```

### Script Updates
Also fixed diagnostic scripts that were using `--exposure off`:
- `Test-LedCameraOptimization.ps1` - now uses `--exposure barcode`
- `test-camera-fix.sh` - now tests valid exposure modes

## Benefits of the Fix

1. **Eliminates Camera Failures**: No more "Invalid exposure mode:off" errors
2. **Better LED Display Performance**: `barcode` mode is specifically designed for high-contrast text/number reading
3. **Improved Compatibility**: Uses only valid libcamera exposure modes
4. **Consistent Behavior**: Proper fallback to `normal` mode when needed

## Testing

The fix includes comprehensive testing for:
- Manual shutter speed scenarios (uses `barcode` mode)
- Non-auto exposure scenarios (uses `normal` mode)
- Fallback compatibility across different Pi hardware

## Deployment

Use the deployment script:
```bash
# On the Raspberry Pi
./scripts/fixes/fix-camera-exposure.sh
```

This will:
1. Test available exposure modes
2. Stop the service
3. Rebuild with the fix
4. Restart the service
5. Verify the fix is working

## Expected Results

After the fix, camera logs should show:
- ✅ `"Manual shutter speed set, using barcode exposure mode for LED displays"`
- ✅ `"Auto exposure disabled, using normal exposure mode"`
- ❌ Should NOT see: `"ERROR: *** Invalid exposure mode:off ***"`

The camera should now successfully capture images for OCR processing without the exposure mode errors.
