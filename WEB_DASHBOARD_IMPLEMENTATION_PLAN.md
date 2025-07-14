# WellMonitor Web Dashboard Implementation Plan

## Overview

A web-based debugging and monitoring interface for the WellMonitor device that provides:
- **Debug Image Visualization**: Real-time and historical debug images with ROI overlays
- **ROI Calibration Interface**: Interactive ROI positioning and validation
- **Device Statistics Dashboard**: Live system metrics and performance data
- **Configuration Management**: Device twin settings management via web interface
- **Camera Positioning Assistant**: Visual feedback for optimal camera placement

## Problem Statement

### Current Challenges
- **ROI Calibration**: Manual device twin updates require technical expertise
- **Camera Positioning**: No visual feedback for optimal placement
- **Debug Image Access**: Debug images only accessible via SSH/file system
- **System Monitoring**: Limited visibility into device performance
- **Troubleshooting**: Difficult to diagnose OCR and camera issues remotely

### User Personas
1. **Field Technicians**: Need visual camera positioning assistance and ROI calibration
2. **System Administrators**: Require device monitoring and configuration management
3. **Support Engineers**: Need debug image access and system diagnostics
4. **Installers**: Need guided setup and validation tools

## Technical Architecture

### 1. Web Framework Integration
**Add ASP.NET Core Web App to existing console application:**

```csharp
// Program.cs - Add Web API capabilities
var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.ConfigureServices(services =>
        {
            services.AddControllers();
            services.AddSignalR(); // For real-time updates
            services.AddCors();
            // ... existing services
        });
        webBuilder.Configure(app =>
        {
            app.UseRouting();
            app.UseCors();
            app.UseStaticFiles();
            app.MapControllers();
            app.MapHub<DeviceStatusHub>("/statusHub");
            app.MapFallbackToFile("index.html");
        });
        webBuilder.UseUrls("http://0.0.0.0:5000"); // Accessible from network
    })
    .ConfigureServices(/* existing services */);
```

### 2. API Endpoints

#### Debug Images API
```csharp
[ApiController]
[Route("api/[controller]")]
public class DebugImagesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRecentImages([FromQuery] int count = 10)
    
    [HttpGet("{timestamp}")]
    public async Task<IActionResult> GetImage(string timestamp)
    
    [HttpGet("{timestamp}/roi")]
    public async Task<IActionResult> GetImageWithRoi(string timestamp)
}
```

#### Device Status API
```csharp
[ApiController]
[Route("api/[controller]")]
public class DeviceStatusController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCurrentStatus()
    
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] string period = "24h")
    
    [HttpGet("health")]
    public async Task<IActionResult> GetHealthCheck()
}
```

#### ROI Configuration API
```csharp
[ApiController]
[Route("api/[controller]")]
public class RoiController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCurrentRoi()
    
    [HttpPost]
    public async Task<IActionResult> UpdateRoi([FromBody] RoiCoordinates roi)
    
    [HttpPost("test")]
    public async Task<IActionResult> TestRoi([FromBody] RoiCoordinates roi)
    
    [HttpPost("calibrate")]
    public async Task<IActionResult> AutoCalibrateRoi()
}
```

### 3. Frontend Framework
**React.js SPA with real-time updates:**

```
src/WellMonitor.Device/wwwroot/
├── index.html                    # Main SPA entry point
├── css/
│   ├── bootstrap.min.css        # UI framework
│   └── wellmonitor.css          # Custom styles
├── js/
│   ├── react.min.js            # React framework
│   ├── signalr.min.js          # Real-time connection
│   └── wellmonitor.js          # Main application
└── components/
    ├── DebugImageViewer.js     # Image gallery with ROI overlay
    ├── RoiCalibrator.js        # Interactive ROI positioning
    ├── DeviceDashboard.js      # System metrics and status
    ├── ConfigurationPanel.js   # Device twin management
    └── CameraPositioning.js    # Camera alignment assistant
```

## Core Features Implementation

### 1. Debug Image Visualization

#### Enhanced Debug Image Viewer
```javascript
class DebugImageViewer extends React.Component {
    state = {
        images: [],
        selectedImage: null,
        showRoi: true,
        zoom: 1.0,
        roiOverlay: true
    };

    async loadRecentImages() {
        const response = await fetch('/api/debugimages?count=50');
        const images = await response.json();
        this.setState({ images });
    }

    renderImageWithRoi(image) {
        return (
            <div className="image-container">
                <canvas 
                    ref={this.canvasRef}
                    onClick={this.handleImageClick}
                    style={{ 
                        cursor: 'crosshair',
                        transform: `scale(${this.state.zoom})`
                    }}
                />
                <div className="roi-overlay">
                    <div className="roi-rectangle" style={this.getRoiStyle()} />
                </div>
                <div className="image-info">
                    <span>OCR Result: {image.ocrResult}</span>
                    <span>Confidence: {image.confidence}%</span>
                    <span>Status: {image.pumpStatus}</span>
                </div>
            </div>
        );
    }
}
```

#### Debug Image Types
- **Original Image**: Full camera capture
- **ROI Extracted**: Cropped region only
- **ROI Overlay**: Full image with ROI boundary highlighted
- **Processed ROI**: Post-OCR preprocessing result
- **OCR Annotated**: Text detection overlays

### 2. Interactive ROI Calibration

#### Visual ROI Editor
```javascript
class RoiCalibrator extends React.Component {
    state = {
        roi: { x: 0.25, y: 0.40, width: 0.50, height: 0.20 },
        livePreview: null,
        ocrResults: []
    };

    async handleRoiUpdate(newRoi) {
        // Update ROI coordinates
        this.setState({ roi: newRoi });
        
        // Test ROI with live image
        const testResult = await fetch('/api/roi/test', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newRoi)
        });
        
        const result = await testResult.json();
        this.setState({ 
            livePreview: result.testImage,
            ocrResults: [...this.state.ocrResults, result]
        });
    }

    async saveRoiConfiguration() {
        await fetch('/api/roi', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(this.state.roi)
        });
        
        // Update device twin via API
        await fetch('/api/devicetwin/roi', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                "RegionOfInterest": {
                    "RoiPercent": this.state.roi
                }
            })
        });
    }

    renderRoiEditor() {
        return (
            <div className="roi-editor">
                <div className="roi-controls">
                    <h3>ROI Calibration</h3>
                    <div className="coordinates">
                        <label>X: <input type="range" min="0" max="1" step="0.01" 
                                       value={this.state.roi.x} 
                                       onChange={this.handleXChange} /></label>
                        <label>Y: <input type="range" min="0" max="1" step="0.01" 
                                       value={this.state.roi.y} 
                                       onChange={this.handleYChange} /></label>
                        <label>Width: <input type="range" min="0.1" max="1" step="0.01" 
                                           value={this.state.roi.width} 
                                           onChange={this.handleWidthChange} /></label>
                        <label>Height: <input type="range" min="0.1" max="1" step="0.01" 
                                            value={this.state.roi.height} 
                                            onChange={this.handleHeightChange} /></label>
                    </div>
                    <div className="actions">
                        <button onClick={this.captureLiveImage}>Capture Test Image</button>
                        <button onClick={this.autoCalibrate}>Auto-Calibrate</button>
                        <button onClick={this.saveRoiConfiguration}>Save Configuration</button>
                    </div>
                </div>
                <div className="roi-preview">
                    {this.renderImageWithRoi(this.state.livePreview)}
                </div>
                <div className="ocr-results">
                    <h4>OCR Test Results</h4>
                    {this.state.ocrResults.map(this.renderOcrResult)}
                </div>
            </div>
        );
    }
}
```

### 3. Real-Time Device Dashboard

#### System Metrics Display
```javascript
class DeviceDashboard extends React.Component {
    state = {
        status: {},
        statistics: {},
        alerts: []
    };

    componentDidMount() {
        // Connect to SignalR for real-time updates
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/statusHub')
            .build();
            
        this.connection.start().then(() => {
            this.connection.on('StatusUpdate', this.handleStatusUpdate);
            this.connection.on('StatisticsUpdate', this.handleStatisticsUpdate);
            this.connection.on('AlertReceived', this.handleAlert);
        });
    }

    renderDashboard() {
        return (
            <div className="device-dashboard">
                <div className="status-cards">
                    <div className="card pump-status">
                        <h3>Pump Status</h3>
                        <div className={`status-indicator ${this.state.status.pumpStatus}`}>
                            {this.state.status.pumpStatus}
                        </div>
                        <div className="current-reading">
                            {this.state.status.currentAmps} A
                        </div>
                    </div>
                    
                    <div className="card system-health">
                        <h3>System Health</h3>
                        <div className="metrics">
                            <div>CPU: {this.state.statistics.cpuUsage}%</div>
                            <div>Memory: {this.state.statistics.memoryUsage}%</div>
                            <div>Disk: {this.state.statistics.diskUsage}%</div>
                            <div>Temperature: {this.state.statistics.temperature}°C</div>
                        </div>
                    </div>
                    
                    <div className="card ocr-performance">
                        <h3>OCR Performance</h3>
                        <div className="metrics">
                            <div>Confidence: {this.state.statistics.ocrConfidence}%</div>
                            <div>Processing Time: {this.state.statistics.ocrProcessingMs}ms</div>
                            <div>Success Rate: {this.state.statistics.ocrSuccessRate}%</div>
                        </div>
                    </div>
                </div>
                
                <div className="charts">
                    <div className="chart current-history">
                        {this.renderCurrentChart()}
                    </div>
                    <div className="chart energy-consumption">
                        {this.renderEnergyChart()}
                    </div>
                </div>
                
                <div className="recent-alerts">
                    <h3>Recent Alerts</h3>
                    {this.state.alerts.map(this.renderAlert)}
                </div>
            </div>
        );
    }
}
```

### 4. Camera Positioning Assistant

#### Visual Alignment Tool
```javascript
class CameraPositioning extends React.Component {
    state = {
        liveImage: null,
        gridOverlay: true,
        ledDetection: false,
        positioningGuide: true
    };

    async captureLiveImage() {
        const response = await fetch('/api/camera/capture');
        const imageData = await response.json();
        this.setState({ liveImage: imageData });
    }

    renderPositioningGuide() {
        return (
            <div className="positioning-guide">
                <div className="camera-view">
                    <img src={this.state.liveImage} alt="Live camera view" />
                    {this.state.gridOverlay && <div className="grid-overlay" />}
                    {this.state.positioningGuide && this.renderGuideLines()}
                </div>
                
                <div className="positioning-controls">
                    <h3>Camera Positioning Assistant</h3>
                    <div className="checklist">
                        <label>
                            <input type="checkbox" checked={this.checkLedVisible()} readOnly />
                            LED Display Visible
                        </label>
                        <label>
                            <input type="checkbox" checked={this.checkProperLighting()} readOnly />
                            Proper Lighting
                        </label>
                        <label>
                            <input type="checkbox" checked={this.checkFocused()} readOnly />
                            Image in Focus
                        </label>
                        <label>
                            <input type="checkbox" checked={this.checkAlignment()} readOnly />
                            Horizontal Alignment
                        </label>
                    </div>
                    
                    <div className="recommendations">
                        {this.generateRecommendations()}
                    </div>
                </div>
            </div>
        );
    }
}
```

## Implementation Plan

### Phase 1: Core Web Infrastructure (1-2 weeks)
1. **Add ASP.NET Core Web API** to existing console application
2. **Implement Debug Images API** with file serving capabilities
3. **Create basic React SPA** with image viewer component
4. **Set up SignalR** for real-time status updates
5. **Add CORS and security** configuration

### Phase 2: ROI Calibration Interface (1-2 weeks)
1. **Implement ROI API endpoints** for configuration management
2. **Create interactive ROI editor** with live preview
3. **Add OCR testing capabilities** with visual feedback
4. **Integrate device twin updates** via web interface
5. **Add ROI validation and quality checks**

### Phase 3: Device Monitoring Dashboard (1-2 weeks)
1. **Implement device status APIs** with real-time data
2. **Create comprehensive dashboard** with system metrics
3. **Add performance charts** and trend visualization
4. **Implement alert management** with notification system
5. **Add device configuration panel** for all settings

### Phase 4: Camera Positioning Tools (1 week)
1. **Add camera positioning assistant** with visual guides
2. **Implement auto-detection algorithms** for LED displays
3. **Create positioning validation** and quality scoring
4. **Add installation guidance** and setup wizards
5. **Integrate with ROI calibration** workflow

### Phase 5: Production Features (1 week)
1. **Add authentication and authorization** for multi-user access
2. **Implement audit logging** for all configuration changes
3. **Add backup and restore** capabilities for settings
4. **Create mobile-responsive design** for tablet access
5. **Add offline capability** and local storage

## Integration with ROI Implementation

### Synergistic Benefits
1. **Visual ROI Configuration**: Web interface makes ROI setup accessible to non-technical users
2. **Real-time ROI Testing**: Immediate feedback during calibration process
3. **ROI Quality Metrics**: Visual validation of ROI effectiveness
4. **Camera Position Optimization**: Ensures optimal setup before ROI calibration
5. **Debug Image Management**: Easy access to ROI extraction results

### Combined Workflow
1. **Camera Positioning**: Use web interface to position camera optimally
2. **Initial ROI Setup**: Use visual calibration tool to set approximate ROI
3. **ROI Fine-tuning**: Adjust coordinates while viewing live OCR results
4. **Validation**: Confirm ROI effectiveness with historical debug images
5. **Production Monitoring**: Monitor ROI performance via dashboard

## Security Considerations

### Network Access
- **Local Network Only**: Web interface accessible only on local network
- **Authentication**: Optional basic authentication for production environments
- **HTTPS**: Optional SSL certificate for secure access
- **Firewall**: Configurable port access (default: 5000)

### API Security
- **Input Validation**: All API inputs validated and sanitized
- **Rate Limiting**: Prevent abuse of image capture and OCR testing
- **Audit Logging**: All configuration changes logged with timestamps
- **Configuration Backup**: Automatic backup before changes

## Expected Benefits

### Immediate Impact
- **Reduced Setup Time**: Visual tools cut ROI calibration time by 80%
- **Improved Accuracy**: Real-time feedback ensures optimal ROI placement
- **Enhanced Troubleshooting**: Visual debug images accelerate problem diagnosis
- **Better System Visibility**: Real-time monitoring improves operational awareness

### Operational Impact
- **Remote Configuration**: Technicians can adjust settings without SSH access
- **Reduced Site Visits**: Many issues can be diagnosed and resolved remotely
- **Better Documentation**: Visual record of camera positioning and ROI settings
- **Training Efficiency**: Visual tools make training new technicians easier

### Long-term Benefits
- **Scalable Management**: Web interface supports managing multiple devices
- **Customer Self-Service**: Tenants can view status without technical support
- **Predictive Maintenance**: Dashboard metrics enable proactive maintenance
- **Integration Ready**: Web API enables integration with other management systems

## Resource Requirements

### Development Time
- **Phase 1**: 1-2 weeks (Web infrastructure)
- **Phase 2**: 1-2 weeks (ROI calibration)
- **Phase 3**: 1-2 weeks (Device dashboard)
- **Phase 4**: 1 week (Camera positioning)
- **Phase 5**: 1 week (Production features)

### Testing Requirements
- Cross-browser compatibility testing
- Mobile device responsiveness testing
- Network security and firewall testing
- Performance testing with large debug image collections
- User experience testing with field technicians

### Documentation Updates
- Web interface user guide
- API documentation for integration
- Security configuration guide
- Installation and deployment updates
- Troubleshooting guide additions

## Conclusion

The WellMonitor Web Dashboard represents a **critical operational enhancement** that transforms the device from a headless IoT device into a user-friendly, visually manageable system. Combined with the ROI implementation, it provides a complete solution for:

- **Field Technicians**: Visual setup and calibration tools
- **System Administrators**: Comprehensive monitoring and management
- **Support Engineers**: Enhanced troubleshooting capabilities
- **Tenants**: Potential future self-service monitoring

**Recommendation**: Implement this web dashboard in parallel with ROI Phase 1-2 to maximize the benefits of both initiatives and provide immediate value to field operations.

The web interface will become the **primary user interface** for device management, making the WellMonitor system accessible to non-technical users while maintaining enterprise-grade functionality.
