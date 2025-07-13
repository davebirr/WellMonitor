# Development Setup Guide

Complete development environment setup for the WellMonitor .NET application.

## Prerequisites

### Development Machine Requirements

**Operating System:**
- **Windows 10/11** (primary development)
  - **WSL2 with Ubuntu 22.04+ HIGHLY RECOMMENDED** for Raspberry Pi development
  - Native Windows (compatible but with limitations)
- Linux (Ubuntu 20.04+) or macOS (alternative)

**Software Requirements:**
- .NET 8 SDK or later
- Visual Studio 2022 or VS Code
- Git
- Azure CLI (for cloud integration)
- PowerShell 7+ (for deployment scripts)

### Development Tools Installation

**Install .NET 8 SDK:**
```powershell
# Windows (using winget)
winget install Microsoft.DotNet.SDK.8

# Or download from: https://dotnet.microsoft.com/download/dotnet/8.0
```

**Install Azure CLI:**
```powershell
# Windows
winget install Microsoft.AzureCLI

# Verify installation
az --version
```

**Install PowerShell 7:**
```powershell
# Windows
winget install Microsoft.PowerShell

# Or use existing Windows PowerShell 5.1
```

## 🐧 WSL2 Setup (Recommended for Raspberry Pi Development)

### Why Use WSL2?

Developing for Raspberry Pi from Windows is **significantly easier** with WSL2 because:

- **Native Linux Environment**: Same OS family as your Raspberry Pi
- **Better .NET ARM64 Builds**: Linux .NET SDK handles cross-compilation more reliably  
- **Native SSH/SCP**: No Windows path translation issues
- **Bash Script Execution**: Run deployment scripts natively without Git Bash quirks
- **Consistent Package Management**: Use apt, systemctl, and other Linux tools naturally

### WSL2 Installation

**1. Install WSL2 (Run in Administrator PowerShell):**
```powershell
# Install WSL2 with Ubuntu 22.04
wsl --install Ubuntu-22.04

# Reboot when prompted
```

**2. Initial WSL Setup:**
```bash
# After reboot, WSL will start automatically
# Set up your username and password when prompted

# Update system packages
sudo apt update && sudo apt upgrade -y
```

**3. Automated Development Environment Setup:**
```bash
# Clone repository and run setup script locally (recommended)
git clone https://github.com/davebirr/WellMonitor.git
cd WellMonitor
chmod +x scripts/setup/setup-wsl-dev-environment.sh
./scripts/setup/setup-wsl-dev-environment.sh

# Alternative: Run directly from GitHub (if repository is public)
# curl -fsSL https://raw.githubusercontent.com/davebirr/WellMonitor/main/scripts/setup/setup-wsl-dev-environment.sh | bash
```

### WSL Development Workflow

**1. Open WSL Terminal:**
- Use Windows Terminal (recommended)
- Or launch from Start Menu: "Ubuntu 22.04"

**2. Navigate to Project:**
```bash
cd ~/WellMonitor  # Your cloned repository
```

**3. Development Commands:**
```bash
# Build for Raspberry Pi
dotnet build src/WellMonitor.Device/WellMonitor.Device.csproj -c Release -r linux-arm64

# Deploy to Pi (much more reliable than Windows)
./scripts/installation/install-wellmonitor.sh

# Monitor Pi logs
ssh pi@raspberrypi.local "sudo journalctl -u wellmonitor -f"

# Open VS Code with WSL context
code .  # Opens project in VS Code with Linux environment
```

### WSL + VS Code Integration

**Install VS Code Extensions:**
- **WSL** extension (essential for WSL development)
- **C#** for Visual Studio Code
- **Remote - WSL** (often included with WSL extension)

**Open Project in WSL:**
```bash
# From WSL terminal
cd ~/WellMonitor
code .  # VS Code opens with WSL context automatically
```

**Benefits:**
- IntelliSense works with Linux .NET SDK
- Integrated terminal runs in WSL
- File paths are native Linux paths
- Debugging works with Linux runtime

### SSH Configuration for Pi Access

**Generate SSH Key (if needed):**
```bash
# Generate SSH key for Pi access
ssh-keygen -t ed25519 -C "your-email@example.com"

# Copy public key to Pi
ssh-copy-id pi@raspberrypi.local

# Test connection
ssh pi@raspberrypi.local "echo 'Connection successful!'"
```

### GitHub Authentication Setup

**Option 1: SSH Key Authentication (Recommended):**
```bash
# Generate SSH key for GitHub (if you don't already have one)
ssh-keygen -t ed25519 -C "your-email@example.com" -f ~/.ssh/id_ed25519_github

# Add SSH key to SSH agent
eval "$(ssh-agent -s)"
ssh-add ~/.ssh/id_ed25519_github

# Display public key to copy to GitHub
cat ~/.ssh/id_ed25519_github.pub

# Configure Git to use SSH
git config --global url."git@github.com:".insteadOf "https://github.com/"
```

**Add the public key to GitHub:**
1. Go to GitHub → Settings → SSH and GPG keys
2. Click "New SSH key"
3. Paste the public key content
4. Test: `ssh -T git@github.com`

**Option 2: GitHub CLI with Device Flow:**
```bash
# Install GitHub CLI
sudo apt update
sudo apt install gh

# Authenticate using device flow (works great in WSL)
gh auth login

# Select:
# - GitHub.com
# - HTTPS
# - Yes (authenticate Git with GitHub credentials)
# - Login with a web browser

# Follow the device flow instructions
```

**Option 3: Personal Access Token:**
```bash
# Configure Git with your username
git config --global user.name "Your Name"
git config --global user.email "your-email@example.com"

# When prompted for password, use Personal Access Token
# Create token at: GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
# Required scopes: repo, workflow
```

### Troubleshooting WSL

**Common Issues:**
1. **WSL2 not starting:**
   ```powershell
   # Restart WSL
   wsl --shutdown
   wsl
   ```

2. **Network issues:**
   ```bash
   # Reset DNS in WSL
   sudo rm /etc/resolv.conf
   sudo bash -c 'echo "nameserver 8.8.8.8" > /etc/resolv.conf'
   ```

3. **VS Code not detecting WSL:**
   ```bash
   # Install code command in WSL
   code --install-extension ms-vscode-remote.remote-wsl
   ```

### WSL File System Access

**Access WSL files from Windows:**
- Open File Explorer
- Navigate to: `\\wsl$\Ubuntu-22.04\home\{username}\WellMonitor`
- Or type `explorer.exe .` from WSL terminal

**Access Windows files from WSL:**
```bash
# Windows C: drive is mounted at /mnt/c
cd /mnt/c/Users/{username}/Documents
```

## Project Structure

```
WellMonitor/
├── src/
│   ├── WellMonitor.Device/          # Main device application
│   ├── WellMonitor.AzureFunctions/  # Azure Functions for PowerApp
│   └── WellMonitor.Shared/          # Shared models and utilities
├── tests/
│   ├── WellMonitor.Device.Tests/    # Unit tests for device app
│   └── WellMonitor.AzureFunctions.Tests/
├── scripts/                        # PowerShell deployment scripts
├── docs/                           # Documentation
└── WellMonitor.sln                 # Solution file
```

### Key Development Files

**Device Application:**
- `Program.cs` - Entry point and service configuration
- `Services/` - Core business logic services
- `Models/` - POCOs for configuration and data
- `Controllers/` - HTTP API endpoints (if enabled)

**Service Architecture:**
- `MonitoringBackgroundService` - Main capture and OCR loop
- `TelemetryBackgroundService` - Azure IoT Hub communication
- `DeviceTwinService` - Configuration management
- `CameraService` - Image capture with debug support
- `OcrService` - OCR processing with multiple providers

## Local Development Environment

### Clone and Setup

**Option 1: WSL2 (Recommended for Pi Development):**
```bash
# In WSL2 terminal
cd ~
git clone https://github.com/davebirr/WellMonitor.git
cd WellMonitor

# Verify .NET installation
dotnet --version

# Test ARM64 build capability  
dotnet build src/WellMonitor.Device/WellMonitor.Device.csproj -c Release -r linux-arm64
```

**Option 2: Windows (Alternative):**
```powershell
# In PowerShell
# Clone repository
git clone https://github.com/davebirr/WellMonitor.git
cd WellMonitor

# Restore dependencies
dotnet restore

# Build solution
dotnet build
```

### Configuration for Development

**Create secrets.json for local development:**
```powershell
# Navigate to device project
cd src/WellMonitor.Device

# Initialize user secrets
dotnet user-secrets init

# Add connection strings (replace with your values)
dotnet user-secrets set "IoTHub:ConnectionString" "HostName=your-hub.azure-devices.net;DeviceId=your-device;SharedAccessKey=your-key"
dotnet user-secrets set "Storage:ConnectionString" "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
dotnet user-secrets set "OCR:AzureKey" "your-cognitive-services-key"
```

**Alternative: Environment Variables**
```powershell
# Set for current session
$env:WELLMONITOR_SECRETS_MODE = "environment"
$env:WELLMONITOR_IOTHUB_CONNECTION_STRING = "your-connection-string"

# Or create .env file (not tracked in git)
echo "WELLMONITOR_SECRETS_MODE=environment" > .env
echo "WELLMONITOR_IOTHUB_CONNECTION_STRING=your-connection-string" >> .env
```

### Development Dependencies

**OCR Development:**
```powershell
# Install Tesseract for Windows development
# Download from: https://github.com/UB-Mannheim/tesseract/wiki

# Add to PATH or set TesseractPath in appsettings.json
```

**SQLite Database:**
```powershell
# SQLite is included via NuGet packages
# No additional installation needed for development
```

## Development Workflow

### 1. Local Development

**Run Device Application:**
```powershell
cd src/WellMonitor.Device
dotnet run
```

**Run with Specific Environment:**
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

**Debug in Visual Studio:**
- Set `WellMonitor.Device` as startup project
- Configure launch settings for development environment
- Use breakpoints for debugging

### 2. Testing

**Run Unit Tests:**
```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/WellMonitor.Device.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Manual Integration Testing:**
```powershell
# Test camera capture (if available)
cd src/WellMonitor.Device
dotnet run --scenario=camera-test

# Test OCR processing
dotnet run --scenario=ocr-test --image-path="test-images/sample.jpg"
```

### 3. Building for Raspberry Pi

**Cross-Platform Build:**
```powershell
# Build for linux-arm64 (Raspberry Pi)
dotnet publish src/WellMonitor.Device/WellMonitor.Device.csproj `
  -c Release `
  -r linux-arm64 `
  --self-contained true `
  -p:PublishSingleFile=true

# Output location
ls src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/
```

**Automated Build Script:**
```powershell
# Use provided build script
.\scripts\Build-ForRaspberryPi.ps1
```

## Device Twin Development

### Local Device Twin Testing

**Mock Device Twin Service:**
Create mock implementation for local testing without Azure:

```csharp
public class MockDeviceTwinService : IDeviceTwinService
{
    public Task<DeviceTwinConfiguration> GetConfigurationAsync()
    {
        // Return test configuration
        return Task.FromResult(new DeviceTwinConfiguration
        {
            MonitoringIntervalSeconds = 30,
            CameraGain = 12.0,
            OcrProvider = "Tesseract"
        });
    }
}
```

**Device Twin Management Scripts:**
```powershell
# Update camera settings for development
.\scripts\Update-LedCameraSettings.ps1

# Test configuration changes
.\scripts\Test-LedCameraOptimization.ps1
```

### Configuration Testing

**Test Configuration Validation:**
```csharp
[Test]
public void ValidateConfiguration_ValidSettings_ReturnsTrue()
{
    var validator = new ConfigurationValidationService();
    var config = new DeviceTwinConfiguration
    {
        CameraGain = 12.0,
        MonitoringIntervalSeconds = 30
    };
    
    var result = validator.Validate(config);
    Assert.IsTrue(result.IsValid);
}
```

## Hardware Abstraction for Development

### Mock Hardware Services

**Mock Camera Service:**
```csharp
public class MockCameraService : ICameraService
{
    public Task<CaptureResult> CaptureImageAsync()
    {
        // Return test image or load from resources
        return Task.FromResult(new CaptureResult
        {
            Success = true,
            ImagePath = "test-images/mock-display.jpg",
            Timestamp = DateTime.UtcNow
        });
    }
}
```

**Mock GPIO Service:**
```csharp
public class MockGpioService : IGpioService
{
    public Task<bool> ControlRelayAsync(bool activate)
    {
        // Log relay action without actual hardware
        Console.WriteLine($"Relay {(activate ? "activated" : "deactivated")}");
        return Task.FromResult(true);
    }
}
```

### Development Service Registration

**Configure services for development:**
```csharp
// In Program.cs for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<ICameraService, MockCameraService>();
    builder.Services.AddTransient<IGpioService, MockGpioService>();
}
else
{
    builder.Services.AddTransient<ICameraService, CameraService>();
    builder.Services.AddTransient<IGpioService, GpioService>();
}
```

## Debugging and Diagnostics

### Logging Configuration

**Development Logging:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "WellMonitor": "Debug",
      "Microsoft": "Warning"
    },
    "Console": {
      "LogLevel": {
        "Default": "Debug"
      }
    }
  }
}
```

**Structured Logging:**
```csharp
_logger.LogInformation("OCR processing completed. Confidence: {Confidence}, Text: {Text}", 
    result.Confidence, result.Text);
```

### Debug Image Handling

**Development Debug Images:**
```csharp
// Configure debug image path for development
"CameraDebugImagePath": "debug_images",
"DebugImageSaveEnabled": true,
"DebugImageRetentionDays": 3
```

**Test Image Resources:**
```
src/WellMonitor.Device/test-images/
├── normal/
│   ├── reading-4-2-amps.jpg
│   └── reading-6-8-amps.jpg
├── dry/
│   ├── display-showing-dry.jpg
│   └── dry-condition.jpg
└── rcyc/
    ├── rapid-cycle-error.jpg
    └── rcyc-display.jpg
```

## Database Development

### Local SQLite Setup

**Development Database:**
```csharp
// Configure for development
"ConnectionStrings": {
  "DefaultConnection": "Data Source=wellmonitor-dev.db"
},
"DatabaseRetentionDays": 7  // Shorter retention for development
```

**Database Migrations:**
```powershell
# Create migration (if using EF Core)
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

**Manual Database Setup:**
```sql
-- Create tables manually if not using EF Core
-- See docs/DataModel.sqlite.sql for schema
```

## CI/CD Integration

### GitHub Actions Workflow

```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Test
      run: dotnet test --no-build --verbosity normal
      
    - name: Build for Raspberry Pi
      run: |
        dotnet publish src/WellMonitor.Device/WellMonitor.Device.csproj \
          -c Release -r linux-arm64 --self-contained true \
          -p:PublishSingleFile=true
```

## Development Best Practices

### Code Organization

**Service Pattern:**
- Implement interfaces for all services
- Use dependency injection for loose coupling
- Create mock implementations for testing

**POCO Models:**
- Place all configuration classes in `Models/` folder
- Use data annotations for validation
- Keep models immutable where possible

**Error Handling:**
- Use structured exception handling
- Log errors with context
- Provide meaningful error messages

### Configuration Management

**Device Twin Integration:**
- Test configuration changes locally first
- Validate all settings before applying
- Provide safe fallback values

**Environment-Specific Settings:**
- Use `appsettings.Development.json` for dev settings
- Never commit secrets to source control
- Use user secrets for local development

### Testing Strategy

**Unit Tests:**
- Test business logic with mocked dependencies
- Test configuration validation
- Test data processing logic

**Integration Tests:**
- Test with real database
- Test Azure IoT Hub integration
- Test OCR processing with sample images

**End-to-End Tests:**
- Test complete workflows
- Test error scenarios
- Test configuration updates

## Deployment to Raspberry Pi

### 🎯 Recommended: WSL2 Deployment

**Complete Installation (Recommended):**
```bash
# From WSL2 terminal in project directory
cd ~/WellMonitor

# Run the comprehensive installation script
./scripts/installation/install-wellmonitor.sh

# Monitor installation progress and results
ssh pi@raspberrypi.local "sudo journalctl -u wellmonitor -f"
```

**Quick Update Deployment:**
```bash
# Build for ARM64
dotnet publish src/WellMonitor.Device/WellMonitor.Device.csproj \
  -c Release \
  -r linux-arm64 \
  --self-contained \
  -o /tmp/wellmonitor-build

# Stop service, copy, restart
ssh pi@raspberrypi.local "sudo systemctl stop wellmonitor"
scp -r /tmp/wellmonitor-build/* pi@raspberrypi.local:/opt/wellmonitor/
ssh pi@raspberrypi.local "sudo systemctl start wellmonitor"
```

### Alternative: Windows Deployment

**Using Git Bash or PowerShell:**
```powershell
# Copy installation script to Pi and run remotely
scp scripts/installation/install-wellmonitor.sh pi@raspberrypi.local:~/
ssh pi@raspberrypi.local "./install-wellmonitor.sh"
```

### Deployment Comparison

| **Method** | **Reliability** | **Build Quality** | **Complexity** | **Recommended** |
|------------|-----------------|-------------------|----------------|-----------------|
| **WSL2** | ✅ Excellent | ✅ Native Linux build | 🟢 Simple | **Yes** |
| **Windows** | ⚠️ Good | ⚠️ Cross-platform build | 🟡 Moderate | No |

### Development Workflow Examples

**WSL2 Development Cycle:**
```bash
# 1. Make code changes in VS Code (opened with 'code .' from WSL)
# 2. Test build
dotnet build src/WellMonitor.Device/WellMonitor.Device.csproj -c Release -r linux-arm64

# 3. Deploy to Pi
./scripts/installation/install-wellmonitor.sh

# 4. Monitor logs
ssh pi@raspberrypi.local "sudo journalctl -u wellmonitor -f"

# 5. Update camera settings via device twin
./scripts/diagnostics/fix-camera-property-names.ps1
```

### Production Deployment

**Secure Installation Process:**
```bash
# 1. SSH to Raspberry Pi
ssh pi@raspberrypi.local

# 2. Update repository
cd ~/WellMonitor  # Or clone if first time
git pull

# 3. Run installation
./scripts/installation/install-wellmonitor.sh

# 4. Verify service
sudo systemctl status wellmonitor
sudo journalctl -u wellmonitor --since "5 minutes ago"
```

For complete deployment procedures, see [Installation Guide](../deployment/installation-guide.md).

## Next Steps

1. **Set up development environment** following this guide
2. **Configure camera and OCR** using [Camera & OCR Setup](../configuration/camera-ocr-setup.md)
3. **Set up Azure services** using [Azure Integration](../configuration/azure-integration.md)
4. **Deploy to Raspberry Pi** using [Installation Guide](../deployment/installation-guide.md)
