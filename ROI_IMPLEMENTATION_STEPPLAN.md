# ROI Implementation Step-by-Step Plan

## Overview
This document provides a detailed, step-by-step plan to implement Region of Interest (ROI) processing for 7-segment LED display monitoring in the WellMonitor system.

## Phase 1: Foundation and Core Infrastructure (Week 1-2)

### Step 1.1: Create ROI Configuration Models
**Duration**: 1 day  
**Dependencies**: None  
**Files to Create/Modify**:
- `src/WellMonitor.Device/Models/RegionOfInterestOptions.cs`
- `src/WellMonitor.Device/Models/RoiCoordinates.cs`

**Tasks**:
1. Create `RegionOfInterestOptions.cs`:
   ```csharp
   public class RegionOfInterestOptions
   {
       public bool EnableRoi { get; set; } = true;
       public RoiCoordinates RoiPercent { get; set; } = new();
       public bool EnableAutoDetection { get; set; } = false;
       public int LedBrightnessThreshold { get; set; } = 180;
       public int ExpansionMargin { get; set; } = 10;
   }
   ```

2. Create `RoiCoordinates.cs`:
   ```csharp
   public class RoiCoordinates
   {
       public double X { get; set; } = 0.25;
       public double Y { get; set; } = 0.40;
       public double Width { get; set; } = 0.50;
       public double Height { get; set; } = 0.20;
   }
   ```

3. Add validation attributes and XML documentation
4. Create unit tests for model validation

**Deliverables**:
- ✅ ROI configuration models with validation
- ✅ Unit tests for models
- ✅ Documentation comments

### Step 1.2: Update Dependency Injection
**Duration**: 0.5 days  
**Dependencies**: Step 1.1  
**Files to Modify**:
- `src/WellMonitor.Device/Program.cs`

**Tasks**:
1. Register `RegionOfInterestOptions` in DI container
2. Configure options pattern with validation
3. Add configuration binding from device twin
4. Test DI configuration

**Code Changes**:
```csharp
// In Program.cs
builder.Services.Configure<RegionOfInterestOptions>(
    builder.Configuration.GetSection("RegionOfInterest"));
builder.Services.AddSingleton<IValidateOptions<RegionOfInterestOptions>, 
    RegionOfInterestOptionsValidator>();
```

**Deliverables**:
- ✅ ROI options registered in DI
- ✅ Configuration validation in place
- ✅ Integration tests for DI setup

### Step 1.3: Enhance ICameraService Interface
**Duration**: 0.5 days  
**Dependencies**: Step 1.1  
**Files to Modify**:
- `src/WellMonitor.Device/Services/ICameraService.cs`

**Tasks**:
1. Add ROI-specific methods to interface:
   ```csharp
   Task<byte[]> CaptureImageWithRoiAsync(CancellationToken cancellationToken = default);
   Task<byte[]> ExtractRoiFromImageAsync(byte[] fullImage, CancellationToken cancellationToken = default);
   Task SaveRoiDebugImagesAsync(byte[] fullImage, byte[] roiImage, CancellationToken cancellationToken = default);
   ```

2. Add ROI validation methods
3. Update interface documentation

**Deliverables**:
- ✅ Enhanced ICameraService interface
- ✅ Method signatures for ROI processing
- ✅ Documentation updates

### Step 1.4: Implement Core ROI Extraction Logic
**Duration**: 2 days  
**Dependencies**: Steps 1.1-1.3  
**Files to Modify**:
- `src/WellMonitor.Device/Services/CameraService.cs`

**Tasks**:
1. Add ROI extraction method:
   ```csharp
   public async Task<byte[]> ExtractRoiFromImageAsync(byte[] fullImage, CancellationToken cancellationToken = default)
   {
       using var image = Image.Load(fullImage);
       
       var x = (int)(image.Width * _roiOptions.RoiPercent.X);
       var y = (int)(image.Height * _roiOptions.RoiPercent.Y);
       var width = (int)(image.Width * _roiOptions.RoiPercent.Width);
       var height = (int)(image.Height * _roiOptions.RoiPercent.Height);
       
       // Validate ROI bounds
       ValidateRoiBounds(x, y, width, height, image.Width, image.Height);
       
       // Crop to ROI
       image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));
       
       using var ms = new MemoryStream();
       await image.SaveAsJpegAsync(ms, cancellationToken);
       return ms.ToArray();
   }
   ```

2. Implement ROI validation logic
3. Add error handling and logging
4. Create comprehensive unit tests

**Deliverables**:
- ✅ Working ROI extraction method
- ✅ ROI bounds validation
- ✅ Error handling and logging
- ✅ Unit tests with mock data

### Step 1.5: Implement ROI Debug Image Saving
**Duration**: 1 day  
**Dependencies**: Step 1.4  
**Files to Modify**:
- `src/WellMonitor.Device/Services/CameraService.cs`

**Tasks**:
1. Create debug image saving method:
   ```csharp
   public async Task SaveRoiDebugImagesAsync(byte[] fullImage, byte[] roiImage, CancellationToken cancellationToken = default)
   {
       var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
       var debugPath = _cameraOptions.DebugImagePath;
       
       // Save full image with ROI overlay
       await SaveFullImageWithRoiOverlayAsync(fullImage, $"{debugPath}/roi_overlay_{timestamp}.jpg");
       
       // Save extracted ROI
       await File.WriteAllBytesAsync($"{debugPath}/roi_extracted_{timestamp}.jpg", roiImage, cancellationToken);
   }
   ```

2. Implement ROI overlay visualization
3. Add debug image cleanup logic
4. Test debug image generation

**Deliverables**:
- ✅ ROI debug image saving
- ✅ ROI overlay visualization
- ✅ Automatic cleanup of old debug images
- ✅ Integration tests for debug functionality

## Phase 2: Integration and Configuration (Week 3)

### Step 2.1: Update Device Twin Service
**Duration**: 1 day  
**Dependencies**: Phase 1 complete  
**Files to Modify**:
- `src/WellMonitor.Device/Services/DeviceTwinService.cs`

**Tasks**:
1. Add ROI property mapping to device twin:
   ```csharp
   private void ProcessRegionOfInterestProperties(TwinCollection desired)
   {
       if (desired.Contains("RegionOfInterest"))
       {
           var roiSection = desired["RegionOfInterest"];
           // Map ROI properties from device twin to configuration
       }
   }
   ```

2. Add ROI configuration validation
3. Update reported properties to include ROI status
4. Test device twin property updates

**Deliverables**:
- ✅ Device twin ROI property mapping
- ✅ Configuration validation
- ✅ Reported properties updates
- ✅ Integration tests with mock device twin

### Step 2.2: Update MonitoringBackgroundService
**Duration**: 1 day  
**Dependencies**: Step 2.1  
**Files to Modify**:
- `src/WellMonitor.Device/Services/MonitoringBackgroundService.cs`

**Tasks**:
1. Modify monitoring loop to use ROI:
   ```csharp
   private async Task ProcessMonitoringCycleAsync(CancellationToken cancellationToken)
   {
       // Capture full image
       var fullImage = await _cameraService.CaptureImageAsync(cancellationToken);
       
       // Extract ROI if enabled
       var imageToProcess = _roiOptions.EnableRoi 
           ? await _cameraService.ExtractRoiFromImageAsync(fullImage, cancellationToken)
           : fullImage;
       
       // Process with OCR
       var ocrResult = await _ocrService.ExtractTextAsync(imageToProcess, cancellationToken);
       
       // Save debug images if enabled
       if (_roiOptions.EnableRoi && _debugOptions.SaveDebugImages)
       {
           await _cameraService.SaveRoiDebugImagesAsync(fullImage, imageToProcess, cancellationToken);
       }
   }
   ```

2. Update error handling for ROI failures
3. Add ROI metrics to telemetry
4. Test end-to-end monitoring with ROI

**Deliverables**:
- ✅ ROI integrated into monitoring loop
- ✅ Fallback handling for ROI failures
- ✅ ROI metrics in telemetry
- ✅ End-to-end integration tests

### Step 2.3: Create ROI Configuration Validation Service
**Duration**: 1 day  
**Dependencies**: Step 2.1  
**Files to Create**:
- `src/WellMonitor.Device/Services/RoiValidationService.cs`

**Tasks**:
1. Create validation service:
   ```csharp
   public class RoiValidationService
   {
       public ValidationResult ValidateRoiConfiguration(RegionOfInterestOptions options)
       {
           var errors = new List<string>();
           
           // Validate coordinate ranges (0.0 - 1.0)
           if (options.RoiPercent.X < 0 || options.RoiPercent.X > 1)
               errors.Add("ROI X coordinate must be between 0.0 and 1.0");
           
           // Validate ROI size
           if (options.RoiPercent.Width <= 0 || options.RoiPercent.Width > 1)
               errors.Add("ROI Width must be between 0.0 and 1.0");
           
           // Add more validation rules...
           
           return new ValidationResult(errors);
       }
   }
   ```

2. Implement comprehensive validation rules
3. Add validation to configuration updates
4. Create validation unit tests

**Deliverables**:
- ✅ ROI configuration validation service
- ✅ Comprehensive validation rules
- ✅ Integration with device twin updates
- ✅ Unit tests for all validation scenarios

## Phase 3: PowerShell Configuration Tools (Week 4)

### Step 3.1: Create ROI Calibration PowerShell Script
**Duration**: 2 days  
**Dependencies**: Phase 2 complete  
**Files to Create**:
- `scripts/configuration/calibrate-roi.ps1`

**Tasks**:
1. Create calibration script:
   ```powershell
   param(
       [Parameter(Mandatory=$true)]
       [string]$IoTHubName,
       
       [Parameter(Mandatory=$true)]
       [string]$DeviceId,
       
       [Parameter(Mandatory=$false)]
       [double]$RoiX = 0.30,
       
       [Parameter(Mandatory=$false)]
       [double]$RoiY = 0.35,
       
       [Parameter(Mandatory=$false)]
       [double]$RoiWidth = 0.40,
       
       [Parameter(Mandatory=$false)]
       [double]$RoiHeight = 0.25
   )
   
   # Validate parameters
   # Update device twin with ROI configuration
   # Test ROI configuration
   # Generate calibration report
   ```

2. Add parameter validation
3. Implement device twin update logic
4. Add calibration testing and reporting

**Deliverables**:
- ✅ ROI calibration PowerShell script
- ✅ Parameter validation and help
- ✅ Device twin integration
- ✅ Calibration testing and reporting

### Step 3.2: Create Interactive ROI Setup Script
**Duration**: 1 day  
**Dependencies**: Step 3.1  
**Files to Create**:
- `scripts/configuration/interactive-roi-setup.ps1`

**Tasks**:
1. Create interactive script with prompts:
   ```powershell
   # Interactive ROI coordinate input
   Write-Host "ROI Calibration Assistant" -ForegroundColor Green
   Write-Host "Please provide the following information about your LED display:"
   
   $roiX = Read-Host "ROI X coordinate (0.0-1.0, 0=left edge)"
   $roiY = Read-Host "ROI Y coordinate (0.0-1.0, 0=top edge)"
   # ... more interactive prompts
   ```

2. Add input validation and help text
3. Implement preview functionality
4. Add save/load configuration profiles

**Deliverables**:
- ✅ Interactive ROI setup script
- ✅ User-friendly prompts and validation
- ✅ Configuration preview functionality
- ✅ Profile save/load capability

### Step 3.3: Update Existing Device Twin Scripts
**Duration**: 1 day  
**Dependencies**: Step 3.1  
**Files to Modify**:
- `scripts/configuration/update-device-twin.ps1`

**Tasks**:
1. Add ROI configuration type:
   ```powershell
   # Add ROI configuration option
   if ($ConfigType -eq "roi") {
       $desiredProperties = @{
           "RegionOfInterest" = @{
               "EnableRoi" = $true
               "RoiPercent" = @{
                   "X" = $RoiX
                   "Y" = $RoiY
                   "Width" = $RoiWidth
                   "Height" = $RoiHeight
               }
           }
       }
   }
   ```

2. Add ROI parameter support
3. Update help documentation
4. Test ROI configuration updates

**Deliverables**:
- ✅ ROI support in device twin script
- ✅ Parameter validation and help
- ✅ Updated script documentation
- ✅ Integration tests

## Phase 4: Advanced Features and Optimization (Week 5)

### Step 4.1: Implement Automatic LED Detection
**Duration**: 2 days  
**Dependencies**: Phase 3 complete  
**Files to Modify**:
- `src/WellMonitor.Device/Services/CameraService.cs`

**Tasks**:
1. Create LED brightness detection method:
   ```csharp
   private List<LedRegion> DetectLedRegions(Image<Rgba32> image)
   {
       var regions = new List<LedRegion>();
       
       // Convert to grayscale for brightness analysis
       var grayImage = image.Clone(ctx => ctx.Grayscale());
       
       // Find bright regions that could be LEDs
       for (int y = 0; y < grayImage.Height - 50; y += 10)
       {
           for (int x = 0; x < grayImage.Width - 100; x += 10)
           {
               var brightness = AnalyzeBrightnessInRegion(grayImage, x, y, 100, 50);
               if (brightness > _roiOptions.LedBrightnessThreshold)
               {
                   regions.Add(new LedRegion { Bounds = new Rectangle(x, y, 100, 50), Brightness = brightness });
               }
           }
       }
       
       return regions;
   }
   ```

2. Implement brightness analysis algorithms
3. Add auto-detection validation
4. Create unit tests with sample images

**Deliverables**:
- ✅ Automatic LED detection algorithm
- ✅ Brightness analysis and validation
- ✅ Fallback to manual ROI
- ✅ Unit tests with sample images

### Step 4.2: Implement ROI Quality Metrics
**Duration**: 1 day  
**Dependencies**: Step 4.1  
**Files to Create**:
- `src/WellMonitor.Device/Models/RoiQualityMetrics.cs`
- Update `src/WellMonitor.Device/Services/TelemetryService.cs`

**Tasks**:
1. Create quality metrics model:
   ```csharp
   public class RoiQualityMetrics
   {
       public double LedCoveragePercentage { get; set; }
       public double BackgroundNoiseLevel { get; set; }
       public double OcrConfidenceImprovement { get; set; }
       public long ProcessingTimeMs { get; set; }
       public DateTime LastCalibration { get; set; }
   }
   ```

2. Implement metrics calculation
3. Add metrics to telemetry data
4. Create metrics dashboard queries

**Deliverables**:
- ✅ ROI quality metrics model
- ✅ Metrics calculation algorithms
- ✅ Telemetry integration
- ✅ Azure IoT Hub dashboard queries

### Step 4.3: Add ROI Profile Management
**Duration**: 1 day  
**Dependencies**: Step 4.1  
**Files to Create**:
- `src/WellMonitor.Device/Models/RoiProfile.cs`
- Update device twin configuration

**Tasks**:
1. Create profile management:
   ```csharp
   public class RoiProfile
   {
       public string Name { get; set; }
       public string Description { get; set; }
       public RoiCoordinates Coordinates { get; set; }
       public string PumpType { get; set; }
       public DateTime Created { get; set; }
   }
   ```

2. Implement profile switching via device twin
3. Add profile validation and testing
4. Create profile management scripts

**Deliverables**:
- ✅ ROI profile management system
- ✅ Profile switching via device twin
- ✅ Profile validation and testing
- ✅ PowerShell profile management tools

## Phase 5: Testing and Documentation (Week 6)

### Step 5.1: Comprehensive Integration Testing
**Duration**: 2 days  
**Dependencies**: Phase 4 complete  
**Files to Create**:
- `tests/WellMonitor.Device.Tests/Integration/RoiIntegrationTests.cs`

**Tasks**:
1. Create end-to-end ROI tests:
   ```csharp
   [Test]
   public async Task RoiProcessing_WithValidConfiguration_ImprovesOcrAccuracy()
   {
       // Arrange: Set up ROI configuration
       // Act: Process test images with and without ROI
       // Assert: Verify accuracy improvement
   }
   ```

2. Test ROI with various image samples
3. Performance benchmarking tests
4. Error handling and edge case tests

**Deliverables**:
- ✅ Comprehensive integration test suite
- ✅ Performance benchmark results
- ✅ Edge case and error handling tests
- ✅ Test documentation and reports

### Step 5.2: Performance Testing and Optimization
**Duration**: 1 day  
**Dependencies**: Step 5.1  

**Tasks**:
1. Benchmark ROI vs full-image processing
2. Memory usage analysis
3. CPU usage optimization
4. Processing speed improvements

**Performance Targets**:
- **3-5x faster OCR processing** with ROI
- **30-50% reduction in CPU usage**
- **<100ms ROI extraction time**
- **>95% OCR accuracy on test images**

**Deliverables**:
- ✅ Performance benchmark report
- ✅ Optimization recommendations
- ✅ Resource usage analysis
- ✅ Performance monitoring setup

### Step 5.3: Documentation Updates
**Duration**: 1 day  
**Dependencies**: Step 5.2  

**Tasks**:
1. Update configuration guide with ROI setup
2. Add ROI troubleshooting section
3. Create ROI calibration video/guide
4. Update API documentation

**Documentation Updates**:
- ✅ Configuration guide ROI section
- ✅ Troubleshooting guide ROI issues
- ✅ Installation guide ROI calibration
- ✅ API reference ROI endpoints

## Phase 6: Deployment and Monitoring (Week 7)

### Step 6.1: Staging Environment Testing
**Duration**: 1 day  
**Dependencies**: Phase 5 complete  

**Tasks**:
1. Deploy ROI implementation to staging
2. Test with real hardware setup
3. Validate device twin configuration
4. Monitor system performance

**Deliverables**:
- ✅ Staging deployment successful
- ✅ Hardware compatibility verified
- ✅ Device twin configuration tested
- ✅ Performance monitoring active

### Step 6.2: Production Deployment Preparation
**Duration**: 1 day  
**Dependencies**: Step 6.1  

**Tasks**:
1. Create deployment checklist
2. Prepare rollback procedures
3. Update production scripts
4. Schedule deployment window

**Deliverables**:
- ✅ Deployment checklist and procedures
- ✅ Rollback plan and scripts
- ✅ Production deployment scripts
- ✅ Deployment schedule and communication

### Step 6.3: Production Deployment and Validation
**Duration**: 1 day  
**Dependencies**: Step 6.2  

**Tasks**:
1. Execute production deployment
2. Validate ROI functionality
3. Monitor system health
4. Update documentation as needed

**Deliverables**:
- ✅ Production deployment complete
- ✅ ROI functionality validated
- ✅ System health monitoring active
- ✅ Post-deployment report

## Success Criteria and Validation

### Technical Success Criteria
- ✅ **OCR Accuracy**: >95% confidence on LED readings with ROI
- ✅ **Performance**: 3-5x faster OCR processing
- ✅ **CPU Usage**: 30-50% reduction in OCR CPU usage
- ✅ **False Positives**: <1% false readings from background text
- ✅ **Configuration**: Remote ROI calibration via device twin

### Operational Success Criteria
- ✅ **Deployment**: >95% successful ROI deployments
- ✅ **Calibration**: >90% successful remote calibrations
- ✅ **Support**: 50% reduction in OCR accuracy support tickets
- ✅ **Setup Time**: 50% reduction in installation time

### Quality Assurance Checkpoints

#### After Phase 1 (Foundation)
- [ ] ROI extraction working with sample images
- [ ] Configuration models validated
- [ ] Basic integration tests passing

#### After Phase 2 (Integration)
- [ ] Device twin ROI configuration working
- [ ] End-to-end monitoring with ROI functional
- [ ] Debug images generated correctly

#### After Phase 3 (Tools)
- [ ] PowerShell calibration scripts functional
- [ ] Interactive ROI setup working
- [ ] Device twin updates successful

#### After Phase 4 (Advanced Features)
- [ ] Auto-detection algorithm working
- [ ] Quality metrics collecting
- [ ] Profile management functional

#### After Phase 5 (Testing)
- [ ] All integration tests passing
- [ ] Performance targets met
- [ ] Documentation complete

#### After Phase 6 (Deployment)
- [ ] Production deployment successful
- [ ] Monitoring and alerting active
- [ ] Post-deployment validation complete

## Risk Mitigation

### High-Risk Items
1. **Image Processing Performance**: Regular benchmarking and optimization
2. **ROI Calibration Complexity**: Comprehensive tooling and documentation
3. **Hardware Compatibility**: Testing on multiple Pi configurations
4. **Configuration Complexity**: Validation and fallback mechanisms

### Mitigation Strategies
- **Incremental Implementation**: Each phase builds on previous work
- **Comprehensive Testing**: Integration tests at each phase
- **Fallback Mechanisms**: Full-image processing if ROI fails
- **Documentation**: Clear setup and troubleshooting guides

## Resource Requirements

### Development Team
- **Lead Developer**: 6 weeks full-time
- **QA Engineer**: 2 weeks testing support
- **DevOps Engineer**: 1 week deployment support

### Infrastructure
- **Development Pi**: For testing and validation
- **Staging Environment**: Mirrors production setup
- **Test Images**: Various LED display samples
- **Performance Monitoring**: Azure Application Insights

## Timeline Summary

| Phase | Duration | Key Deliverables | Dependencies |
|-------|----------|------------------|-------------|
| Phase 1 | Week 1-2 | Core ROI infrastructure | None |
| Phase 2 | Week 3 | Integration and configuration | Phase 1 |
| Phase 3 | Week 4 | PowerShell tools | Phase 2 |
| Phase 4 | Week 5 | Advanced features | Phase 3 |
| Phase 5 | Week 6 | Testing and documentation | Phase 4 |
| Phase 6 | Week 7 | Deployment and monitoring | Phase 5 |

**Total Timeline**: 7 weeks (35 working days)

## Next Steps

### Immediate Actions (This Week)
1. **Review and approve** this implementation plan
2. **Set up development environment** for ROI implementation
3. **Create project branch** for ROI development
4. **Begin Phase 1, Step 1.1**: Create ROI configuration models

### Communication Plan
- **Weekly progress reviews** with stakeholders
- **Phase completion demos** for validation
- **Documentation updates** after each phase
- **Deployment coordination** with operations team

This comprehensive plan provides a clear roadmap for implementing ROI functionality that will significantly improve the accuracy and performance of 7-segment LED display monitoring in the WellMonitor system.
