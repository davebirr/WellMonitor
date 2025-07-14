# Architecture Overview

This document provides a comprehensive overview of the WellMonitor system architecture, including components, data flow, and design decisions.

## System Overview

WellMonitor is a .NET 8 IoT application designed for monitoring water well pumps using computer vision and Azure IoT Hub integration. The system runs on Raspberry Pi 4B and provides real-time monitoring, alerting, and remote control capabilities.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    WellMonitor System                           │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │   Camera    │───▶│ ROI Extract │───▶│     OCR     │───┐     │
│  │   Service   │    │   Service   │    │   Service   │   │     │
│  └─────────────┘    └─────────────┘    └─────────────┘   │     │
│         │                   │                   │        │     │
│         │                   ▼                   ▼        ▼     │
│         │            ┌─────────────┐    ┌─────────────┐         │
│         │            │ LED Display │    │   Analysis  │         │
│         │            │ ROI (30%)   │    │   Service   │         │
│         │            └─────────────┘    └─────────────┘         │
│         ▼                                       │              │
│  ┌─────────────────────────────────────────────────────────────┤
│  │              Database Service (SQLite)                     │
│  └─────────────────────────────────────────────────────────────┤
│         │                                       │              │
│         ▼                                       ▼              │
│  ┌─────────────┐                         ┌─────────────┐       │
│  │ Telemetry   │                         │    GPIO     │       │
│  │  Service    │                         │   Service   │       │
│  └─────────────┘                         └─────────────┘       │
│         │                                       │              │
│         ▼                                       ▼              │
└─────────────────────────────────────────────────────────────────┘
         │                                       │
         ▼                                       ▼
┌─────────────┐                         ┌─────────────┐
│ Azure IoT   │                         │   Relay     │
│    Hub      │                         │  Control    │
└─────────────┘                         └─────────────┘
         │
         ▼
┌─────────────┐
│  PowerApp   │
│   (Tenant)  │
└─────────────┘
```

## Core Components

### 1. Camera Service (`CameraService.cs`)
**Purpose**: Captures images from the Raspberry Pi camera module.

**Key Features**:
- Configurable image parameters (resolution, gain, shutter speed)
- Debug image saving with timestamps
- LED-optimized camera settings
- Error handling and retry logic

**Configuration**:
```csharp
public class CameraOptions
{
    public double Gain { get; set; } = 1.0;
    public int ShutterSpeedMicroseconds { get; set; } = 8000;
    public bool AutoExposure { get; set; } = false;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public string DebugImagePath { get; set; } = "debug_images";
}
```

### 2. OCR Service (`OcrService.cs`)
**Purpose**: Extracts text from captured images using multiple OCR providers.

**Architecture**:
- **Primary Provider**: Tesseract OCR (offline)
- **Secondary Provider**: Azure Cognitive Services (cloud)
- **Fallback Provider**: Python OCR (custom implementation)
- **Dynamic Provider**: Hot-swappable via device twin

**Key Features**:
- Image preprocessing (contrast, noise reduction, scaling)
- Confidence scoring and validation
- Multiple engine support with automatic fallback
- Performance metrics and statistics

### 3. Pump Status Analyzer (`PumpStatusAnalyzer.cs`)
**Purpose**: Interprets OCR results and determines pump status.

**Status Detection**:
```csharp
public enum PumpStatus
{
    Normal,      // Standard operation (current > threshold)
    Idle,        // No current draw
    Dry,         // "Dry" text detected
    RapidCycle,  // "rcyc" text detected
    Off,         // "Off" or no display
    Unknown      // Unrecognized state
}
```

**Business Logic**:
- Current threshold analysis
- Pattern recognition for status messages
- Trend analysis for rapid cycling detection
- Alert generation based on conditions

### 4. Database Service (`DatabaseService.cs`)
**Purpose**: Local data persistence using SQLite.

**Schema Design**:
- **PumpReadings**: High-frequency monitoring data
- **PowerCycles**: Audit trail for relay operations
- **SystemHealth**: Performance metrics
- **DailySummaries**: Aggregated statistics

**Data Management**:
- Automatic retention policy (30 days default)
- Indexed queries for performance
- Batch operations for efficiency
- Migration support for schema updates

### 5. Device Twin Service (`DeviceTwinService.cs`)
**Purpose**: Azure IoT Hub configuration management.

**Configuration Management**:
- 39+ configurable parameters
- Hot configuration updates (no restart required)
- Validation and fallback values
- Property mapping between device twin and application models

**Device Twin Structure**:
```json
{
  "properties": {
    "desired": {
      "Camera": { "Gain": 0.5, "ShutterSpeedMicroseconds": 5000 },
      "Ocr": { "Provider": "Tesseract", "ConfidenceThreshold": 60.0 },
      "Monitoring": { "CaptureIntervalSeconds": 30 },
      "Alert": { "DryWellCurrentThreshold": 9.0 }
    }
  }
}
```

### 6. GPIO Service (`GpioService.cs`)
**Purpose**: Hardware control for relay operations.

**Features**:
- Safe relay control with debounce protection
- Power cycle automation
- Emergency stop capability
- Audit logging for all operations

### 7. Telemetry Service (`TelemetryService.cs`)
**Purpose**: Cloud communication and data synchronization.

**Telemetry Types**:
- Real-time pump readings
- System health metrics
- Alert notifications
- Daily/monthly summaries

## Background Services

### 1. MonitoringBackgroundService
**Frequency**: Every 30 seconds (configurable)
**Function**: 
- Capture pump display image
- Process with OCR
- Analyze pump status
- Store reading in database
- Trigger alerts if needed

### 2. TelemetryBackgroundService
**Frequency**: Every 5 minutes (configurable)
**Function**:
- Aggregate recent readings
- Send telemetry to Azure IoT Hub
- Handle connection failures with retry
- Update device twin reported properties

### 3. SyncBackgroundService
**Frequency**: Every 1 hour (configurable)
**Function**:
- Generate daily summaries
- Sync historical data
- Cleanup old records
- Optimize database performance

## Data Flow Architecture

### 1. Image Processing Pipeline
```
Camera Capture → ROI Extraction → Image Preprocessing → OCR Processing → Text Validation → Status Analysis
      ↓               ↓                 ↓                    ↓              ↓              ↓
  Full Image     LED Display     Enhanced ROI Image      Raw Text       Clean Text     Pump Status
  (1920x1080)   Region (30%)      (768x270)            "12.5 AMPS"      "12.5"        Normal
```

### 2. Configuration Management Flow
```
Azure Device Twin → DeviceTwinService → Configuration Validation → Service Updates
        ↓                    ↓                    ↓                    ↓
   Remote Config      Parse Properties      Validate Values      Apply Settings
```

### 3. Alert Processing Flow
```
Status Analysis → Alert Detection → GPIO Control → Telemetry → PowerApp Notification
       ↓              ↓              ↓             ↓              ↓
   Pump Status    Alert Rules    Relay Action   Cloud Sync    User Alert
```

## Security Architecture

### 1. Authentication & Authorization
- **Azure IoT Hub**: X.509 certificates or symmetric keys
- **Device Identity**: Unique device registration
- **PowerApp**: Azure AD integration
- **Local Access**: System user permissions

### 2. Data Protection
- **In Transit**: TLS 1.2+ encryption
- **At Rest**: SQLite database encryption (optional)
- **Configuration**: Environment variables (no hardcoded secrets)
- **Audit Trail**: Comprehensive logging

### 3. Network Security
- **Firewall**: Outbound-only connections
- **VPN**: Optional for management access
- **Updates**: Secure package management
- **Monitoring**: Connection anomaly detection

## Deployment Architecture

### 1. Production Deployment
```
Development Machine → Build Pipeline → Raspberry Pi Deployment
        ↓                    ↓                    ↓
   Source Code          ARM64 Binary        SystemD Service
```

### 2. Service Management
- **SystemD Service**: Automatic startup and restart
- **Service User**: Dedicated user account with minimal permissions
- **File Permissions**: Secure directory structure
- **Environment Variables**: Isolated configuration

### 3. Update Strategy
- **Blue-Green Deployment**: Zero-downtime updates
- **Configuration Backup**: Automatic backup before updates
- **Rollback Support**: Quick rollback to previous version
- **Health Checks**: Verify service after deployment

## Scalability Considerations

### 1. Horizontal Scaling
- **Multiple Devices**: Independent Pi devices per well
- **Azure IoT Hub**: Scales to millions of devices
- **PowerApp**: Multi-tenant user interface
- **Data Aggregation**: Azure Functions for processing

### 2. Performance Optimization
- **Local Processing**: Minimize cloud dependencies
- **Efficient OCR**: Optimized image preprocessing
- **Database Indexing**: Fast query performance
- **Background Processing**: Non-blocking operations

### 3. Resource Management
- **Memory Usage**: Bounded collections and disposal patterns
- **CPU Usage**: Efficient image processing algorithms
- **Storage Usage**: Automatic cleanup and retention policies
- **Network Usage**: Compressed telemetry and batching

## Error Handling & Resilience

### 1. Fault Tolerance
- **OCR Fallback**: Multiple OCR providers
- **Network Resilience**: Local queuing and retry logic
- **Hardware Failure**: GPIO error handling
- **Configuration Errors**: Validation and fallback values

### 2. Recovery Mechanisms
- **Automatic Restart**: SystemD service recovery
- **Data Recovery**: SQLite transaction rollback
- **Configuration Recovery**: Device twin resynchronization
- **Communication Recovery**: Azure IoT SDK retry policies

### 3. Monitoring & Alerting
- **Health Checks**: Regular system health monitoring
- **Performance Metrics**: OCR success rates and processing times
- **Resource Monitoring**: CPU, memory, and disk usage
- **Alert Escalation**: Multiple notification channels

## Integration Points

### 1. Azure Services
- **IoT Hub**: Device communication and management
- **Cognitive Services**: OCR processing (optional)
- **Storage Account**: Backup and archival
- **Functions**: Data processing and integration

### 2. PowerApp Integration
- **Real-time Data**: Live pump status display
- **Control Interface**: Manual pump control
- **Historical Data**: Trend analysis and reporting
- **Alert Management**: Notification preferences

### 3. External Systems
- **SCADA Systems**: Industrial automation integration
- **Billing Systems**: Usage data export
- **Maintenance Systems**: Schedule and alert integration
- **Security Systems**: Access control and monitoring

## Development Architecture

### 1. Project Structure
```
src/
├── WellMonitor.Device/     # Main application
│   ├── Models/            # Data models and options
│   ├── Services/          # Business logic services
│   ├── Utilities/         # Helper classes
│   └── Program.cs         # Application entry point
├── WellMonitor.Shared/     # Shared components
└── tests/                 # Unit and integration tests
```

### 2. Dependency Injection
- **Service Registration**: Centralized DI configuration
- **Interface Abstraction**: Testable and mockable services
- **Lifetime Management**: Appropriate service lifetimes
- **Configuration Binding**: Strongly-typed options

### 3. Testing Strategy
- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end workflow testing
- **Hardware Mocking**: GPIO and camera simulation
- **Configuration Testing**: Device twin validation

## Future Considerations

### 1. Enhanced Features
- **Machine Learning**: Predictive maintenance analysis
- **Computer Vision**: Advanced image analysis
- **Edge Computing**: Local AI processing
- **Multi-Protocol**: Support for additional communication protocols

### 2. Scalability Improvements
- **Container Deployment**: Docker containerization
- **Cloud Processing**: Hybrid edge-cloud architecture
- **Database Scaling**: Distributed data storage
- **API Gateway**: Centralized API management

### 3. Integration Enhancements
- **REST API**: Direct API access
- **GraphQL**: Flexible data querying
- **Event Streaming**: Real-time event processing
- **Mobile Apps**: Native mobile applications

## Region of Interest (ROI) Strategy for 7-Segment LED Displays

### The Challenge
7-segment LED displays typically occupy only a small portion of captured images, which often contain irrelevant elements like:
- Control switches and buttons
- Warning labels and text
- Status indicators and lights
- Background equipment and wiring
- Reflective surfaces and shadows

### ROI Implementation Strategy

#### 1. **Configuration-Based ROI Definition**
```csharp
public class RegionOfInterestOptions
{
    /// <summary>
    /// Enable ROI-based OCR processing
    /// </summary>
    public bool EnableRoi { get; set; } = true;

    /// <summary>
    /// ROI coordinates as percentage of image dimensions (0.0-1.0)
    /// </summary>
    public RoiCoordinates RoiPercent { get; set; } = new();

    /// <summary>
    /// Automatic ROI detection using LED brightness
    /// </summary>
    public bool EnableAutoDetection { get; set; } = false;

    /// <summary>
    /// Minimum LED brightness threshold for auto-detection
    /// </summary>
    public int LedBrightnessThreshold { get; set; } = 180;

    /// <summary>
    /// ROI expansion margin around detected LED area (pixels)
    /// </summary>
    public int ExpansionMargin { get; set; } = 10;
}

public class RoiCoordinates
{
    public double X { get; set; } = 0.25;      // Start at 25% from left
    public double Y { get; set; } = 0.40;      // Start at 40% from top
    public double Width { get; set; } = 0.50;  // 50% of image width
    public double Height { get; set; } = 0.20; // 20% of image height
}
```

#### 2. **Device Twin Configuration**
```json
{
  "RegionOfInterest": {
    "EnableRoi": true,
    "RoiPercent": {
      "X": 0.30,
      "Y": 0.35,
      "Width": 0.40,
      "Height": 0.25
    },
    "EnableAutoDetection": false,
    "LedBrightnessThreshold": 180,
    "ExpansionMargin": 15
  }
}
```

#### 3. **ROI Processing Pipeline**
```
Full Image Capture → ROI Extraction → Preprocessing → OCR → Text Analysis
       ↓                    ↓              ↓           ↓          ↓
   1920x1080         →  768x270    →   Enhanced    →  "12.5"  → Normal Status
                      (configurable)     Image
```

#### 4. **ROI Extraction Service**
Enhanced `CameraService` with ROI capabilities:

```csharp
public class CameraServiceWithRoi : ICameraService
{
    public async Task<byte[]> CaptureImageWithRoiAsync(CancellationToken cancellationToken = default)
    {
        // Capture full image
        var fullImage = await CaptureFullImageAsync(cancellationToken);
        
        // Extract ROI if enabled
        if (_roiOptions.EnableRoi)
        {
            if (_roiOptions.EnableAutoDetection)
            {
                return await ExtractRoiAutoAsync(fullImage, cancellationToken);
            }
            else
            {
                return await ExtractRoiManualAsync(fullImage, cancellationToken);
            }
        }
        
        return fullImage;
    }

    private async Task<byte[]> ExtractRoiManualAsync(byte[] fullImage, CancellationToken cancellationToken)
    {
        using var image = Image.Load(fullImage);
        
        // Calculate pixel coordinates from percentages
        var x = (int)(image.Width * _roiOptions.RoiPercent.X);
        var y = (int)(image.Height * _roiOptions.RoiPercent.Y);
        var width = (int)(image.Width * _roiOptions.RoiPercent.Width);
        var height = (int)(image.Height * _roiOptions.RoiPercent.Height);
        
        // Crop to ROI
        image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));
        
        // Save debug image showing ROI
        if (_debugOptions.SaveRoiImages)
        {
            await SaveRoiDebugImageAsync(image, cancellationToken);
        }
        
        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    private async Task<byte[]> ExtractRoiAutoAsync(byte[] fullImage, CancellationToken cancellationToken)
    {
        using var image = Image.Load<Rgba32>(fullImage);
        
        // Find brightest regions (LED displays)
        var brightRegions = DetectLedRegions(image);
        
        if (brightRegions.Any())
        {
            var ledRegion = brightRegions.OrderByDescending(r => r.Brightness).First();
            
            // Expand region with margin
            var roi = ExpandRoi(ledRegion.Bounds, _roiOptions.ExpansionMargin, image.Size);
            
            // Crop to detected ROI
            image.Mutate(ctx => ctx.Crop(roi));
            
            using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms, cancellationToken);
            return ms.ToArray();
        }
        
        // Fallback to manual ROI if auto-detection fails
        return await ExtractRoiManualAsync(fullImage, cancellationToken);
    }
}
```

### 5. **ROI Calibration Workflow**

#### Manual ROI Setup Process:
1. **Capture Test Images**: Take several full-resolution captures
2. **Analyze Display Position**: Identify consistent LED display location
3. **Calculate ROI Percentages**: Determine optimal crop boundaries
4. **Update Device Twin**: Configure ROI coordinates remotely
5. **Validate Results**: Verify OCR accuracy improvement

#### Calibration Script Example:
```powershell
# PowerShell script for ROI calibration
./scripts/configuration/calibrate-roi.ps1 -DeviceId "wellmonitor-001" -IoTHubName "YourHub"

# Interactive ROI selection (saves coordinates to device twin)
./scripts/configuration/interactive-roi-setup.ps1
```

### 6. **Benefits of ROI Implementation**

#### Performance Improvements:
- **Faster OCR Processing**: Smaller images process 3-5x faster
- **Reduced CPU Usage**: Less image data to analyze
- **Lower Memory Usage**: Smaller image buffers
- **Improved Accuracy**: Focus on relevant content only

#### Accuracy Improvements:
- **Eliminate False Positives**: No OCR on switches, labels, backgrounds
- **Better Text Recognition**: Higher pixel density for LED segments
- **Consistent Results**: Same display area in every frame
- **Reduced Noise**: Fewer competing text elements

#### Maintenance Benefits:
- **Remote Calibration**: Adjust ROI via device twin without site visit
- **Multiple ROI Profiles**: Different settings for various pump types
- **Debug Visualization**: ROI overlay images for troubleshooting
- **Auto-Adjustment**: Self-calibrating based on LED brightness

### 7. **ROI Debug and Validation**

#### Debug Image Output:
```
debug_images/
├── full_capture_20250713_143022.jpg      # Original full image
├── roi_extracted_20250713_143022.jpg     # Cropped ROI only  
├── roi_overlay_20250713_143022.jpg       # Full image with ROI boundary
└── roi_processed_20250713_143022.jpg     # ROI after preprocessing
```

#### ROI Quality Metrics:
- **LED Coverage**: Percentage of 7-segment display within ROI
- **Background Noise**: Amount of non-LED content in ROI
- **OCR Confidence**: Improvement in recognition confidence
- **Processing Speed**: ROI vs full-image processing time

### 8. **Implementation Priority**

This ROI strategy should be implemented as a high-priority enhancement because:

1. **Immediate Impact**: Significant accuracy improvement for LED monitoring
2. **Performance Gains**: Faster processing enables more frequent monitoring
3. **Remote Configuration**: Adjustable via device twin without site visits
4. **Scalability**: Consistent setup process across multiple installations
5. **Cost Reduction**: Fewer false alerts and manual interventions

## Core Components
