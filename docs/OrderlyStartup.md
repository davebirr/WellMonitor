# Orderly Startup Process Documentation

## Overview

The Well Monitor Device application implements an orderly startup process to ensure reliable operation on the Raspberry Pi 4B. This process validates dependencies, initializes hardware, and starts background services in the correct sequence.

## Startup Sequence

### 1. Dependency Validation (`DependencyValidationService`)
**Purpose**: Validates all required configuration and secrets before proceeding with startup.

**Validation Steps**:
- ✅ Azure IoT Hub connection string presence
- ⚠️ Azure Storage connection string (optional, warning if missing)
- ✅ Local encryption key presence
- ✅ Database connectivity test

**Fail-Fast Behavior**: If critical dependencies are missing, the application will terminate with detailed error logging.

### 2. Hardware Initialization (`HardwareInitializationService`)
**Purpose**: Initializes and validates physical hardware components.

**Initialization Steps**:
- **GPIO Hardware**: 
  - Initialize GPIO pins for relay control
  - Set relay to safe state (off)
  - Verify relay state can be read
- **Camera Hardware**:
  - Initialize camera module
  - Capture test image to validate functionality
  - Verify image capture returns valid data

**Fail-Fast Behavior**: If hardware initialization fails, the application will terminate as the device cannot function without proper hardware.

### 3. Background Services Startup
**Purpose**: Start all monitoring and data processing background services.

**Service Order**:
1. **MonitoringBackgroundService**: Continuous well monitoring
2. **TelemetryBackgroundService**: Azure IoT telemetry transmission
3. **SyncBackgroundService**: Data synchronization and aggregation

## Background Services Details

### MonitoringBackgroundService
- **Frequency**: Every 30 seconds
- **Functions**:
  - Capture images from camera
  - Process OCR to extract current readings
  - Detect abnormal conditions (`Dry`, `rcyc`)
  - Log readings to local database
  - Trigger relay cycling for rapid cycling conditions

### TelemetryBackgroundService
- **Frequency**: Every 5 minutes
- **Functions**:
  - Send unsent readings to Azure IoT Hub
  - Send relay action logs to Azure
  - Retry failed transmissions
  - Mark successful transmissions as synced

### SyncBackgroundService
- **Frequency**: Every 1 hour
- **Functions**:
  - Aggregate daily/monthly summaries
  - Sync summary data to Azure
  - Maintain data retention policies

## Configuration Requirements

### Required Configuration
```json
{
  "IotHubConnectionString": "HostName=...",
  "LocalEncryptionKey": "base64-encoded-key"
}
```

### Optional Configuration
```json
{
  "AzureStorageConnectionString": "DefaultEndpointsProtocol=https;..."
}
```

## Error Handling Strategy

### Startup Failures
- **Critical Failures**: Application terminates with error code
- **Warning Conditions**: Application starts with reduced functionality
- **Comprehensive Logging**: All startup actions are logged

### Runtime Failures
- **Monitoring Failures**: Logged as errors, monitoring continues
- **Telemetry Failures**: Queued for retry, logged as warnings
- **Hardware Failures**: Logged as errors, may trigger application restart

## Graceful Shutdown

### Shutdown Sequence
1. **Stop Background Services**: Cancel all background operations
2. **Final Data Sync**: Attempt to send remaining telemetry
3. **Hardware Cleanup**: Set relay to safe state
4. **Resource Disposal**: Clean up connections and resources

### Shutdown Actions
- Set relay to OFF state for safety
- Attempt final telemetry transmission
- Flush log buffers
- Close database connections

## Monitoring and Health Checks

### Startup Health Indicators
- All dependency validations pass
- Hardware initialization successful
- Background services start without errors

### Runtime Health Indicators
- Recent successful image captures
- Telemetry transmission success rate
- Database connectivity
- Relay responsiveness

## Troubleshooting Common Issues

### Startup Failures

**"Azure IoT Hub connection string is missing"**
- Check `secrets.json` file exists
- Verify `IotHubConnectionString` is present
- Ensure environment variables are set

**"GPIO hardware initialization failed"**
- Check Raspberry Pi GPIO permissions
- Verify relay module connections
- Check for hardware conflicts

**"Camera hardware initialization failed"**
- Verify camera module is connected
- Check camera is enabled in raspi-config
- Ensure camera permissions are correct

**"Database is not accessible"**
- Check database file permissions
- Verify SQLite installation
- Check disk space availability

### Runtime Issues

**High telemetry failure rate**
- Check internet connectivity
- Verify Azure IoT Hub status
- Check for network firewall issues

**Monitoring service errors**
- Check camera functionality
- Verify OCR processing
- Check database write permissions

## Performance Considerations

### Resource Usage
- **Memory**: Background services use minimal memory
- **CPU**: OCR processing is most intensive operation
- **Storage**: Local database grows over time
- **Network**: Telemetry transmission requires stable connection

### Optimization Strategies
- **Image Processing**: Optimize OCR preprocessing
- **Database**: Regular cleanup of old data
- **Network**: Batch telemetry transmission
- **Retry Logic**: Exponential backoff for failed operations

## Development and Testing

### Unit Testing
- All services have corresponding test files
- Mock hardware interfaces for testing
- Test startup sequence validation

### Integration Testing
- Test full startup sequence
- Validate hardware initialization
- Test graceful shutdown

### Deployment Testing
- Test on actual Raspberry Pi hardware
- Validate all GPIO connections
- Test camera functionality
- Verify Azure connectivity

## Security Considerations

### Secrets Management
- Never commit secrets to source control
- Use environment variables for production
- Encrypt sensitive configuration data
- Rotate connection strings regularly

### Hardware Security
- Secure GPIO pin access
- Validate all hardware inputs
- Implement debouncing for relay control
- Monitor for hardware tampering

## Future Enhancements

### Planned Improvements
- Health check HTTP endpoint
- Remote configuration via device twin
- Predictive maintenance alerts
- Enhanced OCR accuracy
- Real-time dashboard integration

### Scalability Considerations
- Support for multiple well monitoring devices
- Centralized configuration management
- Fleet management capabilities
- Automated deployment strategies
