# GitHub Copilot Instructions for Well Monitoring .NET Application

## Project Overview

This project is a .NET application written in C# that runs on a Raspberry Pi 4B. It monitors a water well using a camera and a display device. The device shows the current draw in amps when the pump is running. It displays:
- `'Dry'` when the pump draws less than expected current.
- `'rcyc'` when the pump cycles too quickly.

The Raspberry Pi is registered with Azure IoT Hub and performs the following tasks:
- Captures images of the monitor display using a camera.
- Uses OCR to extract current readings and status messages.
- Calculates energy consumption in kWh per hour, day, and month.
- Detects abnormal states like `'Dry'` and `'rcyc'`.
- Controls an external relay to cycle power when `'rcyc'` is detected.
- Sends telemetry and receives commands via Azure IoT Hub.
- Allows tenants to monitor status and manually cycle power using a PowerApp.


## Project Structure

- Place POCOs (Plain Old CLR Objects), such as options classes and data models, in the `Models` folder (e.g., `src/WellMonitor.Device/Models`). This keeps data contracts and configuration objects organized and reusable across services.

## Coding Guidelines

- Use C# 10 or later with .NET 8 or later.
- Follow standard .NET naming conventions.
- Use async/await for all I/O operations.
- Use dependency injection for services.
- Use `ILogger<T>` for logging.
- Use idomatic C#
- Always suggest best practices
- Use POCOs
- Use DTOs

## Configuration Management

- **Environment Variables**: Use `.env` files for local development and environment variables for production
- **No secrets.json**: Project has migrated away from secrets.json to more secure environment variable approach
- **Configuration Sources**: 
  - `.env` file for local development (not committed to git)
  - Environment variables for production deployment on Raspberry Pi
  - Azure IoT Hub device twin for runtime configuration updates
- **Security**: Sensitive values never committed to version control
- **Standard Practice**: Follows industry standards for configuration management

## Azure Integration

- Use Azure IoT SDK for device communication.
- Send telemetry messages with timestamp, current draw, and status.
- Receive direct method calls to cycle power from PowerApp.
- Use Azure Functions or Logic Apps to bridge PowerApp and IoT Hub if needed.

## Device Twin Configuration

- **Property Name Mapping**: Device twin properties must match exact names in Options classes (e.g., `CameraOptions.cs`)
  - Use `Camera.Gain` not `cameraGain` in device twin
  - Use `Camera.ShutterSpeedMicroseconds` not `cameraShutterSpeedMicroseconds`
  - Always verify property names against actual model classes
- **LED Environment Optimization**: Camera settings optimized for red 7-segment LED displays
  - Low gain values (0.5-2.0) to prevent overexposure
  - Fast shutter speeds (5000-10000 microseconds) for crisp digit capture
  - Disabled auto-exposure and auto-white-balance for consistent results

- **Comprehensive Remote Configuration**: All major settings configurable via Azure IoT Hub device twin
- **39 Configuration Parameters**: Camera settings (11), OCR settings (15+), monitoring intervals (4), image quality (5), alert thresholds (4), debug options (5)
- **Hot Configuration Changes**: Device twin updates apply without service restart via `DeviceTwinService`
- **Validation & Fallbacks**: Comprehensive validation with safe fallback values via `ConfigurationValidationService`
- **Relative Path Support**: Use relative paths (e.g., `debug_images`) instead of absolute paths for portability
- **Configuration Models**: Use dedicated option classes like `CameraOptions`, `OcrOptions`, `MonitoringOptions`, `ImageQualityOptions`, `AlertOptions`, `DebugOptions`
- **Device Twin Files**: 
  - `docs/deployment/device-twin-configuration.md` - Complete device twin configuration guide
  - `scripts/diagnostics/fix-camera-property-names.ps1` - Camera property name correction script
  - `scripts/configuration/update-device-twin.ps1` - Consolidated device twin update script
  - `scripts/diagnostics/troubleshoot-device-twin-sync.sh` - Pi-side device twin diagnostics

## OCR and Image Processing

- **Dual OCR Engine Support**: Use both Tesseract OCR (offline) and Azure Cognitive Services (cloud) with automatic fallback
- **Dynamic OCR Configuration**: Hot-swappable OCR providers via Azure IoT Hub device twin without service restart
- **Enterprise-Grade OCR Service**: Comprehensive OCR implementation with `OcrService`, `TesseractOcrProvider`, `AzureCognitiveServicesOcrProvider`, and `DynamicOcrService`
- **Image Preprocessing**: Advanced image preprocessing with grayscale, contrast enhancement, noise reduction, scaling, and binary thresholding
- **OCR Models**: Use comprehensive OCR models in `src/WellMonitor.Device/Models/OcrModels.cs` including `OcrResult`, `PumpReading`, `OcrStatistics`, etc.
- **OCR Configuration**: OCR settings managed via `OcrOptions` class with full device twin integration
- **Debug Image Support**: Save debug images to `debug_images/` directory (relative path) for OCR tuning and troubleshooting

## Relay Control

- Use GPIO pins on Raspberry Pi to control a relay module.
- Implement a debounce mechanism to avoid rapid toggling.
- Log all relay actions with timestamps.

## PowerApp Integration

- Expose device status and control endpoints via Azure.
- Use Power Automate or Azure Functions to connect PowerApp to IoT Hub.
- Authenticate tenant actions and log manual overrides.

## Hardware Abstraction

- Abstract GPIO and camera access behind interfaces for testability and mocking.
- Use dependency injection for all hardware and cloud services.

## Background Services & Startup

- **Orderly Startup Process**: Dependency validation → Hardware initialization → Background workers
- **Background Services**: 
  - `MonitoringBackgroundService` - Captures images and processes OCR every 30 seconds
  - `TelemetryBackgroundService` - Sends telemetry to Azure IoT Hub every 5 minutes
  - `SyncBackgroundService` - Syncs summary data every 1 hour
- **Startup Services**:
  - `DependencyValidationService` - Validates Azure IoT Hub connection and database
  - `HardwareInitializationService` - Initializes GPIO and camera hardware
- **Service Registration**: All services registered with proper dependency injection in `Program.cs`

## Data Logging & Sync Strategy

- Log high-frequency readings locally (SQLite) for 7–30 days for detailed graphs and troubleshooting.
- Aggregate and store daily/monthly kWh summaries for long-term billing.
- Sync periodic telemetry and summaries to Azure IoT Hub, with local queuing and retry if offline.
- Use local data for graphs; use monthly summaries for billing.
- See `docs/DataLoggingAndSync.md` for schema, retention, and implementation notes.

## Key Implementation Files

### Core Services
- `src/WellMonitor.Device/Services/DeviceTwinService.cs` - Device twin configuration management
- `src/WellMonitor.Device/Services/OcrService.cs` - Main OCR processing service
- `src/WellMonitor.Device/Services/DynamicOcrService.cs` - Hot-swappable OCR configuration
- `src/WellMonitor.Device/Services/CameraService.cs` - Camera capture with debug image support
- `src/WellMonitor.Device/Services/ConfigurationValidationService.cs` - Configuration validation

### Models & Configuration
- `src/WellMonitor.Device/Models/OcrModels.cs` - Comprehensive OCR data models
- `src/WellMonitor.Device/Models/OcrOptions.cs` - OCR configuration options
- `src/WellMonitor.Device/Models/CameraOptions.cs` - Camera configuration options
- `src/WellMonitor.Device/Models/MonitoringOptions.cs` - Monitoring intervals
- `src/WellMonitor.Device/Models/ImageQualityOptions.cs` - Image quality validation
- `src/WellMonitor.Device/Models/AlertOptions.cs` - Alert thresholds
- `src/WellMonitor.Device/Models/DebugOptions.cs` - Debug and logging settings

### Documentation
- `docs/deployment/` - Organized deployment guides and device twin configuration
- `docs/configuration/` - Configuration management and environment setup
- `docs/development/` - Development workflow and testing guides
- `docs/reference/` - Reference documentation and troubleshooting
- `scripts/diagnostics/` - Device diagnostics and troubleshooting tools
- `scripts/configuration/` - Configuration management and device twin updates

## Development & Testing

### OCR Development
- Use `debug_images/` directory for OCR development and testing
- Camera service saves debug images automatically when `cameraDebugImagePath` is configured
- Debug images saved with timestamp format: `pump_reading_20250711_143022.jpg`
- Organize sample images in subdirectories: `normal/`, `idle/`, `dry/`, `rcyc/`, `off/`, `live/`

### Configuration Testing
- Use PowerShell scripts to update device twin configuration
- Test configuration changes without service restart
- Validate all settings with `ConfigurationValidationService`
- Use relative paths for portability across environments
- **Property Name Validation**: Always verify device twin property names match Option class properties
- **Camera LED Optimization**: Test camera settings in actual LED environment, not bright room conditions

### Enterprise Requirements
- **High Reliability**: Dual OCR providers with automatic fallback
- **Remote Management**: Comprehensive device twin configuration (39 parameters)
- **Scalability**: Designed for utility company deployments with multiple devices
- **Monitoring**: Comprehensive statistics and diagnostics
- **Compliance**: Enterprise-grade logging and audit trails

## Common Issues & Solutions

### Device Twin Property Mapping
- **Issue**: Device twin properties not recognized by application
- **Cause**: Property names in device twin don't match Option class property names
- **Solution**: Use exact property names from Option classes (e.g., `Camera.Gain` not `cameraGain`)
- **Prevention**: Always validate property names against source code before device twin updates

### Camera Overexposure in LED Environment
- **Issue**: Debug images appear completely white when monitoring red LED displays
- **Cause**: Camera settings optimized for normal lighting, not bright LED environment
- **Solution**: Reduce gain (0.5-2.0), use fast shutter speeds (5000-10000 μs), disable auto-exposure
- **Prevention**: Test camera settings in actual deployment environment, not development lighting

### Configuration Management
- **Issue**: Configuration scattered across multiple files and approaches
- **Solution**: Standardize on environment variables and .env files, eliminate secrets.json
- **Best Practice**: Use `.env` for development, environment variables for production, device twin for runtime updates
