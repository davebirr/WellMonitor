# WellMonitor Testing Guide

Comprehensive testing procedures for the WellMonitor application, from unit tests to end-to-end validation.

## Testing Strategy

### Test Pyramid

```
    E2E Tests (System Integration)
         /\
        /  \
   Integration Tests
      /      \
     /        \
   Unit Tests
```

**Unit Tests (60%)**
- Service logic testing
- Configuration validation
- Data model testing
- Mock hardware interfaces

**Integration Tests (30%)**
- Database operations
- Azure IoT Hub integration
- OCR processing with real images
- Hardware interface testing

**End-to-End Tests (10%)**
- Complete monitoring workflows
- Device twin configuration updates
- Alert generation and handling

## Running Tests

### Unit Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/WellMonitor.Device.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Unit

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Integration Tests

```bash
# Run integration tests (requires Azure connection)
dotnet test --filter Category=Integration

# Run database integration tests
dotnet test --filter Category=Database

# Run OCR integration tests
dotnet test --filter Category=OCR
```

### Performance Tests

```bash
# Run performance benchmarks
dotnet test --filter Category=Performance

# Memory usage tests
dotnet test --filter Category=Memory
```

## Test Configuration

### Test Environment Setup

**Test Configuration (appsettings.Test.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=:memory:"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning"
    }
  },
  "Camera": {
    "MockEnabled": true,
    "TestImagePath": "test-images/"
  },
  "OCR": {
    "Provider": "Mock",
    "TestMode": true
  }
}
```

**Test Service Registration:**
```csharp
public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Use in-memory database for tests
        services.AddDbContext<WellMonitorContext>(options =>
            options.UseInMemoryDatabase("TestDatabase"));
            
        // Mock services for testing
        services.AddTransient<ICameraService, MockCameraService>();
        services.AddTransient<IGpioService, MockGpioService>();
        services.AddTransient<IOcrService, MockOcrService>();
    }
}
```

## Unit Test Examples

### Service Logic Testing

```csharp
[TestFixture]
public class MonitoringServiceTests
{
    private MockCameraService _mockCamera;
    private MockOcrService _mockOcr;
    private MonitoringService _monitoringService;

    [SetUp]
    public void Setup()
    {
        _mockCamera = new MockCameraService();
        _mockOcr = new MockOcrService();
        _monitoringService = new MonitoringService(_mockCamera, _mockOcr);
    }

    [Test]
    public async Task ProcessReading_ValidImage_ExtractsCurrentValue()
    {
        // Arrange
        _mockCamera.SetupCaptureResult(success: true, imagePath: "test-4.2-amps.jpg");
        _mockOcr.SetupOcrResult(confidence: 0.95, text: "4.2");

        // Act
        var result = await _monitoringService.ProcessReadingAsync();

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(4.2, result.CurrentDraw);
        Assert.AreEqual(0.95, result.OcrConfidence);
    }

    [Test]
    public async Task ProcessReading_DryCondition_DetectsStatus()
    {
        // Arrange
        _mockOcr.SetupOcrResult(confidence: 0.90, text: "Dry");

        // Act
        var result = await _monitoringService.ProcessReadingAsync();

        // Assert
        Assert.AreEqual("Dry", result.Status);
        Assert.IsTrue(result.RequiresAttention);
    }
}
```

### Configuration Validation Testing

```csharp
[TestFixture]
public class DeviceTwinConfigurationTests
{
    private DeviceTwinValidationService _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new DeviceTwinValidationService();
    }

    [Test]
    public void ValidateConfiguration_ValidCameraSettings_ReturnsValid()
    {
        // Arrange
        var config = new DeviceTwinConfiguration
        {
            CameraGain = 12.0,
            CameraShutterSpeedMicroseconds = 50000,
            CameraWidth = 1280,
            CameraHeight = 720
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsEmpty(result.ValidationErrors);
    }

    [Test]
    public void ValidateConfiguration_InvalidCameraGain_ReturnsInvalid()
    {
        // Arrange
        var config = new DeviceTwinConfiguration
        {
            CameraGain = 25.0  // Invalid: too high
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.Contains("CameraGain must be between 0.0 and 20.0", result.ValidationErrors);
    }
}
```

### Database Testing

```csharp
[TestFixture]
public class DatabaseServiceTests
{
    private WellMonitorContext _context;
    private DatabaseService _databaseService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<WellMonitorContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WellMonitorContext(options);
        _databaseService = new DatabaseService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task SaveReading_ValidReading_SavesSuccessfully()
    {
        // Arrange
        var reading = new Reading
        {
            Timestamp = DateTime.UtcNow,
            CurrentDraw = 4.2,
            Status = "Normal",
            OcrConfidence = 0.95
        };

        // Act
        await _databaseService.SaveReadingAsync(reading);

        // Assert
        var saved = await _context.Readings.FirstOrDefaultAsync();
        Assert.IsNotNull(saved);
        Assert.AreEqual(4.2, saved.CurrentDraw);
    }
}
```

## Integration Test Examples

### Azure IoT Hub Integration

```csharp
[TestFixture]
[Category("Integration")]
public class TelemetryServiceIntegrationTests
{
    private TelemetryService _telemetryService;
    
    [SetUp]
    public void Setup()
    {
        // Requires actual Azure IoT Hub connection string
        var connectionString = Environment.GetEnvironmentVariable("TEST_IOTHUB_CONNECTION_STRING");
        Assume.That(connectionString, Is.Not.Null.And.Not.Empty);
        
        _telemetryService = new TelemetryService(connectionString);
    }

    [Test]
    public async Task SendTelemetry_ValidData_SendsSuccessfully()
    {
        // Arrange
        var telemetryData = new TelemetryMessage
        {
            DeviceId = "test-device",
            CurrentDraw = 4.2,
            Status = "Normal",
            Timestamp = DateTime.UtcNow
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(() => _telemetryService.SendTelemetryAsync(telemetryData));
    }
}
```

### OCR Integration with Real Images

```csharp
[TestFixture]
[Category("Integration")]
public class OcrIntegrationTests
{
    private TesseractOcrService _ocrService;
    
    [SetUp]
    public void Setup()
    {
        _ocrService = new TesseractOcrService();
    }

    [Test]
    public async Task ProcessImage_LedDisplay_ExtractsNumber()
    {
        // Arrange
        var imagePath = Path.Combine("test-images", "led-display-4.2-amps.jpg");
        Assume.That(File.Exists(imagePath));

        // Act
        var result = await _ocrService.ProcessImageAsync(imagePath);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.That(result.ExtractedText, Contains.Substring("4.2"));
        Assert.Greater(result.Confidence, 0.7);
    }

    [Test]
    public async Task ProcessImage_DryDisplay_DetectsDryCondition()
    {
        // Arrange
        var imagePath = Path.Combine("test-images", "display-showing-dry.jpg");
        Assume.That(File.Exists(imagePath));

        // Act
        var result = await _ocrService.ProcessImageAsync(imagePath);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.That(result.ExtractedText.ToLower(), Contains.Substring("dry"));
    }
}
```

## Test Data and Mock Services

### Mock Camera Service

```csharp
public class MockCameraService : ICameraService
{
    private bool _successResult = true;
    private string _imagePathResult = "mock-image.jpg";

    public void SetupCaptureResult(bool success, string imagePath = null)
    {
        _successResult = success;
        _imagePathResult = imagePath ?? _imagePathResult;
    }

    public Task<CaptureResult> CaptureImageAsync()
    {
        return Task.FromResult(new CaptureResult
        {
            Success = _successResult,
            ImagePath = _imagePathResult,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

### Mock OCR Service

```csharp
public class MockOcrService : IOcrService
{
    private double _confidenceResult = 0.95;
    private string _textResult = "4.2";

    public void SetupOcrResult(double confidence, string text)
    {
        _confidenceResult = confidence;
        _textResult = text;
    }

    public Task<OcrResult> ProcessImageAsync(string imagePath)
    {
        return Task.FromResult(new OcrResult
        {
            Success = true,
            Confidence = _confidenceResult,
            ExtractedText = _textResult,
            ProcessingTimeMs = 100
        });
    }
}
```

### Test Image Resources

Organize test images for various scenarios:

```
tests/test-images/
├── normal/
│   ├── led-display-4.2-amps.jpg    # Clear numeric reading
│   ├── led-display-6.8-amps.jpg    # Higher current reading
│   └── lcd-display-normal.jpg      # LCD text display
├── dry/
│   ├── display-showing-dry.jpg     # "Dry" status message
│   ├── led-display-blank.jpg       # Blank/off display
│   └── no-water-message.jpg        # "No Water" message
├── rcyc/
│   ├── rapid-cycle-error.jpg       # "rcyc" error message
│   └── cycling-display.jpg         # Cycling status
├── poor-quality/
│   ├── blurry-image.jpg           # Motion blur
│   ├── dark-image.jpg             # Under-exposed
│   ├── overexposed-image.jpg      # Over-exposed
│   └── noisy-image.jpg            # High ISO noise
└── edge-cases/
    ├── partial-display.jpg        # Partially visible display
    ├── multiple-numbers.jpg       # Multiple numeric values
    └── unusual-angle.jpg          # Camera at angle
```

## End-to-End Testing

### Complete Monitoring Workflow Test

```csharp
[TestFixture]
[Category("E2E")]
public class MonitoringWorkflowTests
{
    private TestHost _testHost;
    
    [SetUp]
    public void Setup()
    {
        _testHost = new TestHostBuilder()
            .UseConfiguration(GetTestConfiguration())
            .UseServices(ConfigureTestServices)
            .Build();
    }

    [Test]
    public async Task CompleteMonitoringCycle_NormalOperation_ProcessesSuccessfully()
    {
        // Arrange
        var monitoringService = _testHost.Services.GetService<MonitoringBackgroundService>();
        
        // Act
        await monitoringService.ExecuteAsync(CancellationToken.None);
        
        // Assert
        // Verify image captured
        // Verify OCR processed  
        // Verify database updated
        // Verify telemetry sent
    }
}
```

### Device Twin Configuration Update Test

```csharp
[Test]
[Category("E2E")]
public async Task DeviceTwinUpdate_CameraSettings_AppliesConfiguration()
{
    // Arrange
    var deviceTwinService = _testHost.Services.GetService<DeviceTwinService>();
    var newConfig = new DeviceTwinConfiguration
    {
        CameraGain = 8.0,
        MonitoringIntervalSeconds = 60
    };

    // Act
    await deviceTwinService.UpdateConfigurationAsync(newConfig);
    
    // Wait for configuration to apply
    await Task.Delay(TimeSpan.FromSeconds(2));

    // Assert
    var appliedConfig = await deviceTwinService.GetCurrentConfigurationAsync();
    Assert.AreEqual(8.0, appliedConfig.CameraGain);
    Assert.AreEqual(60, appliedConfig.MonitoringIntervalSeconds);
}
```

## Performance Testing

### OCR Performance Benchmarks

```csharp
[TestFixture]
[Category("Performance")]
public class OcrPerformanceTests
{
    [Test]
    public async Task OcrProcessing_StandardImage_CompletesWithinTimeout()
    {
        // Arrange
        var ocrService = new TesseractOcrService();
        var imagePath = "test-images/normal/led-display-4.2-amps.jpg";
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await ocrService.ProcessImageAsync(imagePath);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(result.Success);
        Assert.Less(stopwatch.Elapsed, timeout);
        Assert.Greater(result.Confidence, 0.7);
    }
}
```

### Memory Usage Testing

```csharp
[Test]
[Category("Memory")]
public async Task MonitoringService_LongRunning_DoesNotLeakMemory()
{
    // Arrange
    var initialMemory = GC.GetTotalMemory(true);
    var monitoringService = new MonitoringService();

    // Act - Run for extended period
    for (int i = 0; i < 100; i++)
    {
        await monitoringService.ProcessReadingAsync();
        
        if (i % 10 == 0)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    // Assert
    var finalMemory = GC.GetTotalMemory(true);
    var memoryGrowth = finalMemory - initialMemory;
    
    // Should not grow by more than 10MB
    Assert.Less(memoryGrowth, 10 * 1024 * 1024);
}
```

## Test Automation

### GitHub Actions Workflow

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
        
    - name: Install Tesseract
      run: |
        sudo apt-get update
        sudo apt-get install tesseract-ocr tesseract-ocr-eng
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Run Unit Tests
      run: dotnet test --no-build --filter Category=Unit --logger trx --results-directory TestResults/
      
    - name: Run Integration Tests
      run: dotnet test --no-build --filter Category=Integration --logger trx --results-directory TestResults/
      env:
        TEST_IOTHUB_CONNECTION_STRING: ${{ secrets.TEST_IOTHUB_CONNECTION_STRING }}
        
    - name: Publish Test Results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Test Results
        path: TestResults/*.trx
        reporter: dotnet-trx
```

## Testing Best Practices

### Test Organization
- ✅ Use descriptive test names: `Method_Scenario_ExpectedResult`
- ✅ Group related tests in test classes
- ✅ Use categories to organize test execution
- ✅ Follow AAA pattern: Arrange, Act, Assert

### Mock and Stub Usage
- ✅ Mock external dependencies (Azure, hardware)
- ✅ Use real implementations for business logic
- ✅ Verify interactions with mocks when needed
- ✅ Keep mocks simple and focused

### Test Data Management
- ✅ Use realistic test images
- ✅ Create reusable test data builders
- ✅ Clean up test data after each test
- ✅ Use in-memory databases for unit tests

### Continuous Testing
- ✅ Run unit tests on every commit
- ✅ Run integration tests on pull requests
- ✅ Run performance tests periodically
- ✅ Monitor test execution times

For production deployment testing, see [Installation Guide](../deployment/installation-guide.md).
For development environment setup, see [Development Setup](development-setup.md).
