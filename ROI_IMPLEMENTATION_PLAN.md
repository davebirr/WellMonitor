# ROI (Region of Interest) Implementation Plan

## Problem Statement
The current OCR system processes entire camera images, which typically contain:
- Control switches and buttons (irrelevant text)
- Warning labels and equipment markings
- Background wiring and equipment
- Status lights and indicators
- Reflective surfaces

**Result**: OCR confusion, false positives, and reduced accuracy for the actual 7-segment LED display.

## Solution: ROI-Focused Processing

### Core Strategy
Extract only the relevant portion of the image containing the 7-segment LED display before OCR processing.

### Technical Implementation

#### 1. Enhanced Configuration Model
```csharp
// Add to WellMonitor.Device/Models/
public class RegionOfInterestOptions
{
    public bool EnableRoi { get; set; } = true;
    public RoiCoordinates RoiPercent { get; set; } = new();
    public bool EnableAutoDetection { get; set; } = false;
    public int LedBrightnessThreshold { get; set; } = 180;
    public int ExpansionMargin { get; set; } = 10;
}

public class RoiCoordinates
{
    public double X { get; set; } = 0.25;      // 25% from left
    public double Y { get; set; } = 0.40;      // 40% from top  
    public double Width { get; set; } = 0.50;  // 50% width
    public double Height { get; set; } = 0.20; // 20% height
}
```

#### 2. Enhanced Camera Service
```csharp
// Extend existing CameraService.cs
public class CameraService
{
    private readonly RegionOfInterestOptions _roiOptions;
    
    public async Task<byte[]> CaptureImageWithRoiAsync(CancellationToken cancellationToken = default)
    {
        // Capture full image first
        var fullImage = await CaptureImageAsync(cancellationToken);
        
        // Apply ROI extraction if enabled
        if (_roiOptions.EnableRoi)
        {
            return await ExtractRoiAsync(fullImage, cancellationToken);
        }
        
        return fullImage;
    }
    
    private async Task<byte[]> ExtractRoiAsync(byte[] fullImage, CancellationToken cancellationToken)
    {
        using var image = Image.Load(fullImage);
        
        // Calculate pixel coordinates from percentages
        var x = (int)(image.Width * _roiOptions.RoiPercent.X);
        var y = (int)(image.Height * _roiOptions.RoiPercent.Y);
        var width = (int)(image.Width * _roiOptions.RoiPercent.Width);
        var height = (int)(image.Height * _roiOptions.RoiPercent.Height);
        
        // Crop to ROI
        image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));
        
        // Save debug images showing ROI
        await SaveRoiDebugImages(image, fullImage, cancellationToken);
        
        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}
```

#### 3. Device Twin Integration
```json
{
  "properties": {
    "desired": {
      "RegionOfInterest": {
        "EnableRoi": true,
        "RoiPercent": {
          "X": 0.30,
          "Y": 0.35,
          "Width": 0.40,
          "Height": 0.25
        },
        "EnableAutoDetection": false,
        "ExpansionMargin": 15
      }
    }
  }
}
```

#### 4. Enhanced Debug Output
```
debug_images/
├── pump_reading_20250713_143022.jpg      # Original full image
├── roi_extracted_20250713_143022.jpg     # Cropped ROI only
├── roi_overlay_20250713_143022.jpg       # Full image with ROI boundary
└── roi_processed_20250713_143022.jpg     # ROI after preprocessing
```

## Implementation Steps

### Phase 1: Core ROI Infrastructure (Priority: High)
1. **Add ROI Configuration Models**
   - Create `RegionOfInterestOptions.cs`
   - Add to dependency injection
   - Integrate with `DeviceTwinService`

2. **Enhance Camera Service**
   - Add ROI extraction method
   - Implement percentage-based coordinates
   - Add ROI debug image saving

3. **Update OCR Pipeline**
   - Modify `MonitoringBackgroundService` to use ROI
   - Update image processing workflow
   - Add ROI metrics to telemetry

### Phase 2: Configuration and Calibration (Priority: High)
1. **Device Twin Integration**
   - Add ROI properties to device twin schema
   - Update configuration validation
   - Add ROI property mapping

2. **Calibration Tools**
   - Create PowerShell ROI calibration script
   - Add ROI coordinate calculation helpers
   - Implement ROI validation checks

### Phase 3: Advanced Features (Priority: Medium)
1. **Auto-Detection**
   - Implement LED brightness detection
   - Add automatic ROI positioning
   - Fallback to manual ROI if auto fails

2. **Multiple ROI Profiles**
   - Support named ROI configurations
   - Enable switching between profiles
   - Profile validation and testing

### Phase 4: Monitoring and Optimization (Priority: Medium)
1. **Performance Metrics**
   - Track ROI processing speed improvements
   - Monitor OCR accuracy changes
   - Add ROI coverage quality metrics

2. **Adaptive ROI**
   - Monitor LED position stability
   - Auto-adjust ROI based on detection confidence
   - Alert when ROI recalibration needed

## Expected Benefits

### Immediate Impact (Phase 1)
- **3-5x Faster OCR Processing**: Smaller images = faster processing
- **Elimination of False Positives**: No OCR on switches, labels
- **Higher OCR Confidence**: Focus on relevant content only
- **Reduced CPU Usage**: Less data to process

### Operational Impact (Phase 2)
- **Remote Calibration**: Adjust ROI via device twin without site visits
- **Consistent Setup**: Standardized ROI calibration process
- **Better Troubleshooting**: ROI debug images for problem diagnosis
- **Scalable Deployment**: Easy setup for multiple devices

### Long-term Benefits (Phase 3-4)
- **Self-Calibrating System**: Automatic ROI adjustment
- **Multiple Pump Support**: Different ROI profiles for different pumps
- **Predictive Maintenance**: ROI quality metrics for camera alignment
- **Enhanced Reliability**: Adaptive system reduces manual intervention

## Resource Requirements

### Development Time
- **Phase 1**: 2-3 weeks (core implementation)
- **Phase 2**: 1-2 weeks (configuration and tools)
- **Phase 3**: 2-3 weeks (advanced features)
- **Phase 4**: 1-2 weeks (monitoring and optimization)

### Testing Requirements
- Multiple test installations with different camera angles
- Various LED display types and mounting positions
- Performance testing on Raspberry Pi hardware
- Long-term stability testing

### Documentation Updates
- Configuration guide ROI section (✅ completed)
- Architecture overview ROI strategy (✅ completed) 
- Installation guide ROI calibration steps
- Troubleshooting guide ROI issues
- API reference ROI endpoints

## Risk Mitigation

### Technical Risks
- **Incorrect ROI Configuration**: Provide validation and debug images
- **Camera Movement**: ROI monitoring and alerts for recalibration
- **Performance Impact**: Benchmark and optimize image processing
- **Backward Compatibility**: Make ROI optional with fallback to full image

### Operational Risks
- **Complex Calibration**: Provide automated tools and clear documentation
- **Remote Troubleshooting**: Enhanced debug images and telemetry
- **Multiple Device Management**: Standardized configuration templates
- **Field Support**: Clear escalation procedures for ROI issues

## Success Metrics

### Technical Metrics
- **OCR Accuracy Improvement**: Target >95% confidence on LED readings
- **Processing Speed**: Target 3x improvement in OCR processing time
- **False Positive Reduction**: Target <1% false readings from background text
- **CPU Usage Reduction**: Target 30-50% reduction in OCR CPU usage

### Operational Metrics
- **Deployment Success Rate**: >95% successful ROI calibrations
- **Remote Calibration Success**: >90% ROI adjustments work without site visit
- **Support Ticket Reduction**: 50% reduction in OCR accuracy issues
- **Installation Time**: 50% reduction in on-site OCR setup time

## Conclusion

ROI implementation is a **critical enhancement** that addresses the core challenge of 7-segment LED monitoring in complex environments. The focused approach will dramatically improve accuracy, performance, and operational efficiency while providing the foundation for advanced features like auto-calibration and predictive maintenance.

**Recommendation**: Implement Phase 1 and 2 as highest priority to realize immediate benefits, then proceed with advanced features based on field experience and customer feedback.
