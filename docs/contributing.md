# Contributing to WellMonitor

Thank you for your interest in contributing to WellMonitor! This document provides guidelines and information for contributors.

## 📋 Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Coding Standards](#coding-standards)
- [Configuration Management](#configuration-management)
- [Testing](#testing)
- [Documentation](#documentation)
- [Submitting Changes](#submitting-changes)
- [Azure Integration](#azure-integration)

## 🚀 Getting Started

### Prerequisites

- **.NET 8 SDK** or later
- **Visual Studio Code** or Visual Studio 2022
- **Git** for version control
- **Azure CLI** (for Azure integration)
- **Raspberry Pi 4B** (for hardware testing)

### Development Environment

1. **Clone the repository:**
   ```bash
   git clone https://github.com/davebirr/WellMonitor.git
   cd WellMonitor
   ```

2. **Set up environment variables:**
   ```bash
   cp .env.example .env
   # Edit .env with your Azure IoT Hub connection string and other secrets
   ```

3. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

4. **Build the solution:**
   ```bash
   dotnet build
   ```

## 🏗️ Development Setup

### Local Development

See [docs/development/development-setup.md](development/development-setup.md) for detailed setup instructions.

### Configuration Files

- **`.env`** - Local development secrets (never commit!)
- **`.env.example`** - Template for environment variables
- **`appsettings.json`** - Application configuration
- **`appsettings.Development.json`** - Development overrides

## 📁 Project Structure

```
WellMonitor/
├── src/
│   ├── WellMonitor.Device/        # Main Raspberry Pi application
│   ├── WellMonitor.AzureFunctions/  # Azure Functions for cloud processing
│   └── WellMonitor.Shared/        # Shared models and utilities
├── tests/                         # Unit and integration tests
├── docs/                         # Organized documentation
│   ├── configuration/            # Setup and configuration guides
│   ├── deployment/               # Installation and deployment
│   ├── development/              # Development workflows
│   └── reference/                # API and technical reference
└── scripts/                      # Organized automation scripts
    ├── configuration/            # Device twin and settings
    ├── deployment/               # Pi deployment tools
    ├── diagnostics/              # Testing and troubleshooting
    ├── installation/             # Service setup
    └── maintenance/              # Fixes and cleanup
```

## 🎯 Coding Standards

### C# Guidelines

- **Target Framework:** .NET 8 or later
- **Language Version:** C# 10 or later
- **Naming:** Follow standard .NET naming conventions
- **Async/Await:** Use for all I/O operations
- **Dependency Injection:** Use built-in DI container
- **Logging:** Use `ILogger<T>` for all logging

### Code Quality

- **Models:** Place POCOs in `Models` folders
- **Services:** Use interfaces for dependency injection
- **Error Handling:** Comprehensive exception handling
- **Documentation:** XML documentation comments for public APIs
- **Best Practices:** Follow idiomatic C# patterns

### Example Service Pattern

```csharp
public interface ICameraService
{
    Task<byte[]> CaptureImageAsync(CancellationToken cancellationToken = default);
}

public class CameraService : ICameraService
{
    private readonly ILogger<CameraService> _logger;
    private readonly IOptions<CameraOptions> _options;

    public CameraService(
        ILogger<CameraService> logger,
        IOptions<CameraOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async Task<byte[]> CaptureImageAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Capturing camera image");
        // Implementation
    }
}
```

## ⚙️ Configuration Management

### Environment Variables Approach

WellMonitor uses **environment variables** for configuration management:

- **Development:** `.env` files for local development
- **Production:** Environment variables on Raspberry Pi
- **Azure IoT Hub:** Device twin for runtime configuration updates

### Configuration Classes

Create options classes for configuration sections:

```csharp
public class CameraOptions
{
    public double Gain { get; set; } = 1.0;
    public int ShutterSpeedMicroseconds { get; set; } = 10000;
    public string DebugImagePath { get; set; } = "debug_images";
}
```

### Device Twin Integration

- **Property Names:** Must match exact names in Options classes
- **LED Optimization:** Camera settings optimized for red 7-segment displays
- **Hot Configuration:** Changes apply without service restart via `DeviceTwinService`

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test tests/WellMonitor.Device.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories

- **Unit Tests:** Individual service and component testing
- **Integration Tests:** Azure IoT Hub and database integration
- **Hardware Tests:** Camera and GPIO testing (requires Pi)

### Writing Tests

```csharp
[Test]
public async Task CaptureImageAsync_ShouldReturnImageData()
{
    // Arrange
    var logger = Mock.Of<ILogger<CameraService>>();
    var options = Microsoft.Extensions.Options.Options.Create(new CameraOptions());
    var service = new CameraService(logger, options);

    // Act
    var result = await service.CaptureImageAsync();

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Length, Is.GreaterThan(0));
}
```

## 📚 Documentation

### Documentation Standards

- **API Documentation:** XML comments for all public members
- **Architecture:** Document design decisions and patterns
- **Configuration:** Document all configuration options
- **Deployment:** Step-by-step deployment guides

### Documentation Structure

- **User Guides:** In `docs/configuration/` and `docs/deployment/`
- **Developer Guides:** In `docs/development/`
- **API Reference:** In `docs/reference/`
- **Inline Comments:** For complex business logic

## 🔄 Submitting Changes

### Git Workflow

1. **Create Feature Branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Changes:**
   - Follow coding standards
   - Add/update tests
   - Update documentation

3. **Commit Changes:**
   ```bash
   git add .
   git commit -m "feat: add camera exposure controls
   
   - Add exposure time configuration option
   - Update device twin property mapping
   - Add integration tests for camera settings"
   ```

4. **Push and Create PR:**
   ```bash
   git push origin feature/your-feature-name
   # Create pull request via GitHub
   ```

### Commit Message Format

Use conventional commits:

- **feat:** New features
- **fix:** Bug fixes
- **docs:** Documentation changes
- **refactor:** Code refactoring
- **test:** Test additions/changes
- **chore:** Build process or auxiliary tool changes

### Pull Request Guidelines

- **Clear Description:** Explain what changes and why
- **Testing:** Include test results and validation steps
- **Documentation:** Update relevant documentation
- **Breaking Changes:** Clearly document any breaking changes

## ☁️ Azure Integration

### Azure Services Used

- **Azure IoT Hub:** Device communication and management
- **Azure Functions:** Cloud processing and PowerApp integration
- **Azure Storage:** Telemetry data and image storage
- **Azure Cognitive Services:** OCR processing (optional)

### Development Considerations

- **Connection Strings:** Never commit real connection strings
- **Device Twin:** Test configuration changes thoroughly
- **Telemetry:** Validate message formats and frequency
- **Error Handling:** Robust offline/online scenarios

### Security Best Practices

- **Secrets Management:** Use environment variables only
- **Access Keys:** Rotate regularly and use least privilege
- **Network Security:** Secure communication channels
- **Device Security:** Follow IoT security best practices

## 🛠️ Hardware Considerations

### Raspberry Pi Development

- **GPIO Access:** Requires proper permissions and groups
- **Camera Module:** Test with actual LED displays
- **Performance:** Consider ARM64 optimizations
- **Security:** Use systemd security features

### Testing Environment

- **LED Display:** Test with actual red 7-segment displays
- **Lighting Conditions:** Test in actual deployment environment
- **Network:** Test offline/online scenarios

## 📞 Getting Help

- **Issues:** Use GitHub Issues for bug reports and feature requests
- **Discussions:** Use GitHub Discussions for questions and ideas
- **Documentation:** Check `docs/` for detailed guides
- **Examples:** See `tests/` for code examples

## 📄 License

By contributing to WellMonitor, you agree that your contributions will be licensed under the same license as the project.

---

Thank you for contributing to WellMonitor! Your contributions help make water well monitoring more reliable and accessible. 🚰