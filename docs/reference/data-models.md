# Data Models

This document describes the database schema, data structures, and data flow in the WellMonitor application.

## Database Schema

WellMonitor uses SQLite for local data storage with the following schema:

### PumpReadings Table
Primary table for storing pump monitoring data.

```sql
CREATE TABLE PumpReadings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp DATETIME NOT NULL,
    CurrentDraw REAL NOT NULL,
    Status TEXT NOT NULL,
    Confidence REAL NOT NULL,
    RawText TEXT,
    ImagePath TEXT,
    ProcessingTimeMs INTEGER,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    SyncedAt DATETIME NULL
);

CREATE INDEX IX_PumpReadings_Timestamp ON PumpReadings (Timestamp);
CREATE INDEX IX_PumpReadings_Status ON PumpReadings (Status);
CREATE INDEX IX_PumpReadings_SyncedAt ON PumpReadings (SyncedAt);
```

### PowerCycles Table
Tracks power cycle events for audit and analysis.

```sql
CREATE TABLE PowerCycles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp DATETIME NOT NULL,
    Reason TEXT NOT NULL,
    InitiatedBy TEXT NOT NULL,
    DurationMs INTEGER,
    Success BOOLEAN NOT NULL,
    ErrorMessage TEXT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IX_PowerCycles_Timestamp ON PowerCycles (Timestamp);
```

### SystemHealth Table
System health and performance metrics.

```sql
CREATE TABLE SystemHealth (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp DATETIME NOT NULL,
    CpuUsage REAL NOT NULL,
    MemoryUsage REAL NOT NULL,
    DiskUsage REAL NOT NULL,
    Temperature REAL NOT NULL,
    UptimeSeconds INTEGER NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IX_SystemHealth_Timestamp ON SystemHealth (Timestamp);
```

### DailySummaries Table
Aggregated daily statistics for reporting and billing.

```sql
CREATE TABLE DailySummaries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Date DATE NOT NULL UNIQUE,
    TotalRuntimeMinutes INTEGER NOT NULL,
    TotalKwh REAL NOT NULL,
    AverageCurrent REAL NOT NULL,
    PeakCurrent REAL NOT NULL,
    CycleCount INTEGER NOT NULL,
    AlertCount INTEGER NOT NULL,
    UptimePercentage REAL NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    SyncedAt DATETIME NULL
);

CREATE INDEX IX_DailySummaries_Date ON DailySummaries (Date);
```

## Data Models (C# Classes)

### Core Models

#### PumpReading
```csharp
public class PumpReading
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public double CurrentDraw { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string? RawText { get; set; }
    public string? ImagePath { get; set; }
    public int ProcessingTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}
```

#### PowerCycle
```csharp
public class PowerCycle
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### SystemHealth
```csharp
public class SystemHealth
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public double Temperature { get; set; }
    public int UptimeSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### DailySummary
```csharp
public class DailySummary
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int TotalRuntimeMinutes { get; set; }
    public double TotalKwh { get; set; }
    public double AverageCurrent { get; set; }
    public double PeakCurrent { get; set; }
    public int CycleCount { get; set; }
    public int AlertCount { get; set; }
    public double UptimePercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SyncedAt { get; set; }
}
```

### Configuration Models

#### CameraOptions
```csharp
public class CameraOptions
{
    public double Gain { get; set; } = 1.0;
    public int ShutterSpeedMicroseconds { get; set; } = 8000;
    public bool AutoExposure { get; set; } = false;
    public bool AutoWhiteBalance { get; set; } = false;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public string DebugImagePath { get; set; } = "debug_images";
    public bool SaveDebugImages { get; set; } = true;
}
```

#### OcrOptions
```csharp
public class OcrOptions
{
    public string Provider { get; set; } = "Tesseract";
    public double ConfidenceThreshold { get; set; } = 60.0;
    public bool PreprocessingEnabled { get; set; } = true;
    public double ContrastEnhancement { get; set; } = 1.5;
    public bool NoiseReduction { get; set; } = true;
    public double ScalingFactor { get; set; } = 2.0;
    public int BinaryThreshold { get; set; } = 128;
    public string TesseractDataPath { get; set; } = "/usr/share/tesseract-ocr/4.00/tessdata";
    public string Language { get; set; } = "eng";
    public string PageSegmentationMode { get; set; } = "8";
    public string OcrEngineMode { get; set; } = "3";
}
```

#### MonitoringOptions
```csharp
public class MonitoringOptions
{
    public int CaptureIntervalSeconds { get; set; } = 30;
    public int TelemetryIntervalSeconds { get; set; } = 300;
    public int SyncIntervalSeconds { get; set; } = 3600;
    public int DataRetentionDays { get; set; } = 30;
}
```

#### AlertOptions
```csharp
public class AlertOptions
{
    public double DryWellCurrentThreshold { get; set; } = 9.0;
    public int RapidCycleThresholdCount { get; set; } = 10;
    public int RapidCycleTimeWindowMinutes { get; set; } = 10;
    public int PowerCycleProtectionMinutes { get; set; } = 5;
}
```

### OCR Models

#### OcrResult
```csharp
public class OcrResult
{
    public bool Success { get; set; }
    public string RawText { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Provider { get; set; } = string.Empty;
    public int ProcessingTimeMs { get; set; }
    public PumpReading? ParsedReading { get; set; }
    public string? ErrorMessage { get; set; }
}
```

#### OcrStatistics
```csharp
public class OcrStatistics
{
    public int TotalAttempts { get; set; }
    public int SuccessfulReadings { get; set; }
    public double SuccessRate => TotalAttempts > 0 ? (double)SuccessfulReadings / TotalAttempts * 100 : 0;
    public double AverageConfidence { get; set; }
    public int AverageProcessingTimeMs { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public DateTime LastSuccessfulReading { get; set; }
}
```

## Data Flow

### 1. Image Capture → OCR Processing
```
CameraService → OcrService → PumpStatusAnalyzer → DatabaseService
     ↓               ↓              ↓               ↓
  Raw Image    → Text Extract → Status Parse → Store Reading
```

### 2. Telemetry Pipeline
```
MonitoringService → TelemetryService → Azure IoT Hub → PowerApp
       ↓                   ↓              ↓           ↓
   Local Data    →    Aggregate    →   Cloud Sync → User Display
```

### 3. Configuration Updates
```
Azure Device Twin → DeviceTwinService → ConfigurationValidation → Service Update
        ↓                    ↓                    ↓                    ↓
   Remote Config  →     Parse Settings →    Validate Values  →   Apply Changes
```

## Data Retention Strategy

### Local Storage (SQLite)
- **PumpReadings**: 30 days (configurable)
- **PowerCycles**: 365 days (permanent)
- **SystemHealth**: 7 days
- **DailySummaries**: 2 years

### Cloud Storage (Azure IoT Hub)
- **Real-time Telemetry**: 7 days
- **Device Twin**: Current state only
- **Aggregated Data**: Long-term storage via Azure Functions

### Sync Strategy
1. **High-frequency data** (readings) stored locally
2. **Aggregated summaries** synced to cloud hourly
3. **Critical alerts** sent immediately
4. **Bulk historical data** available on-demand

## Performance Considerations

### Database Optimization
- Indexed timestamp columns for fast queries
- Automatic cleanup of old records
- Batch inserts for bulk operations
- Connection pooling for concurrent access

### Memory Management
- Streaming for large datasets
- Bounded collections for in-memory caches
- Dispose patterns for unmanaged resources
- Background cleanup of debug images

### Network Efficiency
- Compressed telemetry payloads
- Batched message sending
- Retry logic with exponential backoff
- Local queuing during network outages

## Data Validation

### Input Validation
- Current draw: 0.0 - 50.0 amps
- Confidence: 0.0 - 100.0%
- Status: Enum validation
- Timestamps: UTC timezone required

### Business Rules
- Readings older than 5 minutes rejected
- Duplicate readings within 1 second filtered
- Confidence below threshold marked for review
- Status transitions logged for audit trail
