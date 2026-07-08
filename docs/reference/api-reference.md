# API Reference

This document describes the WellMonitor API endpoints, commands, telemetry, and data structures.

## Azure IoT Hub Integration

### Device Twin Properties

The WellMonitor device supports 39+ configurable parameters through Azure IoT Hub device twin. See [Configuration Guide](../configuration/configuration-guide.md) for complete details.

#### Camera Configuration
```json
{
  "properties": {
    "desired": {
      "Camera": {
        "Gain": 1.0,
        "ShutterSpeedMicroseconds": 8000,
        "AutoExposure": false,
        "AutoWhiteBalance": false,
        "Width": 1920,
        "Height": 1080,
        "DebugImagePath": "debug_images"
      }
    }
  }
}
```

#### OCR Configuration
```json
{
  "properties": {
    "desired": {
      "Ocr": {
        "Provider": "Tesseract",
        "ConfidenceThreshold": 60.0,
        "PreprocessingEnabled": true,
        "ContrastEnhancement": 1.5,
        "NoiseReduction": true,
        "ScalingFactor": 2.0,
        "BinaryThreshold": 128
      }
    }
  }
}
```

#### Monitoring Configuration
```json
{
  "properties": {
    "desired": {
      "Monitoring": {
        "CaptureIntervalSeconds": 30,
        "TelemetryIntervalSeconds": 300,
        "SyncIntervalSeconds": 3600,
        "DataRetentionDays": 30
      }
    }
  }
}
```

### Direct Methods

#### Power Cycle Command
```json
{
  "methodName": "PowerCycle",
  "payload": {
    "reason": "Manual override from PowerApp",
    "userId": "tenant@example.com"
  }
}
```

**Response:**
```json
{
  "status": 200,
  "payload": {
    "success": true,
    "message": "Power cycle completed successfully",
    "timestamp": "2025-07-13T14:30:22Z",
    "cycleId": "cycle-abc123"
  }
}
```

#### Get Device Status
```json
{
  "methodName": "GetStatus",
  "payload": {}
}
```

**Response:**
```json
{
  "status": 200,
  "payload": {
    "pumpStatus": "Normal",
    "currentDraw": 12.5,
    "lastReading": "2025-07-13T14:30:22Z",
    "confidence": 95.2,
    "systemHealth": "Healthy",
    "uptime": "5d 12h 30m"
  }
}
```

### Telemetry Messages

#### Pump Reading Telemetry
```json
{
  "deviceId": "wellmonitor-001",
  "timestamp": "2025-07-13T14:30:22Z",
  "messageType": "pumpReading",
  "data": {
    "currentDraw": 12.5,
    "status": "Normal",
    "confidence": 95.2,
    "temperature": 35.2,
    "voltage": 238.5,
    "powerConsumption": 2.98,
    "dailyKwh": 18.2,
    "monthlyKwh": 425.8
  }
}
```

#### Alert Telemetry
```json
{
  "deviceId": "wellmonitor-001",
  "timestamp": "2025-07-13T14:30:22Z",
  "messageType": "alert",
  "data": {
    "alertType": "DryWell",
    "severity": "High",
    "description": "Pump showing 'Dry' status",
    "currentDraw": 8.1,
    "expectedCurrent": 12.5,
    "duration": "PT2M30S",
    "actionRequired": true
  }
}
```

#### System Health Telemetry
```json
{
  "deviceId": "wellmonitor-001",
  "timestamp": "2025-07-13T14:30:22Z",
  "messageType": "systemHealth",
  "data": {
    "cpuUsage": 15.2,
    "memoryUsage": 68.4,
    "diskUsage": 42.1,
    "temperature": 52.3,
    "cameraStatus": "Connected",
    "ocrStatus": "Active",
    "lastSuccessfulReading": "2025-07-13T14:29:52Z",
    "uptimeSeconds": 456321
  }
}
```

## Error Codes

### Device Twin Update Errors
- **400**: Invalid configuration format
- **404**: Property not found
- **422**: Validation failed
- **500**: Internal service error

### Direct Method Errors
- **400**: Invalid method parameters
- **403**: Method not allowed in current state
- **404**: Method not found
- **408**: Method timeout
- **500**: Internal service error

### OCR Error Codes
- **OCR_001**: Image capture failed
- **OCR_002**: OCR processing failed
- **OCR_003**: Low confidence reading
- **OCR_004**: No text detected
- **OCR_005**: Invalid text format

## PowerApp Integration

### Webhook Endpoints
WellMonitor can send webhook notifications to PowerApp:

```http
POST https://your-powerapp-endpoint.azurewebsites.net/api/well-alert
Content-Type: application/json
Authorization: Bearer {api-key}

{
  "deviceId": "wellmonitor-001",
  "alertType": "RapidCycling",
  "timestamp": "2025-07-13T14:30:22Z",
  "severity": "Medium",
  "data": {
    "cycleCount": 15,
    "timeWindow": "PT10M",
    "powerCycleInitiated": true
  }
}
```

## Data Export APIs

### Daily Summary Export
Available via Azure IoT Hub message routing or direct database query:

```json
{
  "date": "2025-07-13",
  "deviceId": "wellmonitor-001",
  "summary": {
    "totalRuntime": "PT8H45M",
    "totalKwh": 18.2,
    "averageCurrent": 12.1,
    "cycleCount": 12,
    "alertCount": 0,
    "uptimePercentage": 98.5
  }
}
```

### Monthly Billing Report
```json
{
  "month": "2025-07",
  "deviceId": "wellmonitor-001",
  "billing": {
    "totalKwh": 425.8,
    "peakKwh": 1.85,
    "offPeakKwh": 423.95,
    "estimatedCost": 89.47,
    "days": 13,
    "averageDailyKwh": 32.75
  }
}
```

## Rate Limits

- **Device Twin Updates**: 100 per hour
- **Direct Methods**: 30 per minute
- **Telemetry Messages**: 8000 per day
- **Webhook Calls**: 1000 per hour

## Best Practices

1. **Use device twin for configuration**, not direct methods
2. **Batch telemetry messages** when possible
3. **Implement exponential backoff** for retries
4. **Validate all inputs** before processing
5. **Use appropriate severity levels** for alerts
6. **Monitor rate limits** to avoid throttling
