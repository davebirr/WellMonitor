# Well Monitoring .NET Application

This project is a .NET 8 application written in C# for the Raspberry Pi 4B. It monitors a water well using a camera and display device, integrates with Azure IoT Hub, and provides remote monitoring and control for tenants via PowerApp.

## Features

- **🎯 Complete OCR Integration**: Dual OCR engine support with intelligent pump monitoring
- **📱 Enterprise Configuration**: 39 configurable parameters via Azure IoT Hub device twin
- **📷 Intelligent Monitoring**: Automated image capture and pump status analysis every 30 seconds
- **🔍 Advanced OCR**: Extracts current readings from LED displays with confidence scoring
- **⚡ Safety Controls**: Automatic power cycling for rapid cycling with protection intervals
- **🚨 Condition Detection**: Detects 'Dry', 'Rapid Cycling', 'Normal', 'Idle', and 'Off' states
- **🔧 GPIO Control**: Relay management with debounce protection and audit logging
- **☁️ Azure IoT Integration**: Comprehensive telemetry and device twin configuration
- **📱 PowerApp Ready**: Framework prepared for tenant monitoring interface
- **📊 Enterprise Logging**: Local SQLite with comprehensive audit trails and sync strategy

## Documentation

**📚 [Complete Documentation](docs/README.md)**

### Quick Access
- **🚀 [Installation Guide](docs/deployment/installation-guide.md)** - Complete setup from development to production
- **⚙️ [Configuration Guide](docs/configuration/configuration-guide.md)** - Device twin and system configuration  
- **🔧 [Deployment Configuration](docs/configuration/deployment-configuration.md)** - Environment variables and deployment setup
- **🔧 [Service Management](docs/deployment/service-management.md)** - Service operations and monitoring
- **🎥 [Camera & OCR Setup](docs/configuration/camera-ocr-setup.md)** - Hardware and image processing optimization

### Documentation Structure
```
docs/
├── README.md                    # Main documentation index
├── deployment/                  # 📦 Installation and operations
│   ├── installation-guide.md   # Complete setup process
│   ├── service-management.md   # Service operations
│   └── troubleshooting-guide.md # Problem solving
├── configuration/               # ⚙️ Settings and integration
│   ├── configuration-guide.md  # Device twin configuration
│   ├── deployment-configuration.md # Environment variables and deployment setup
│   ├── camera-ocr-setup.md    # Hardware optimization
│   └── azure-integration.md   # Cloud services setup
├── development/                 # 🔧 Development environment
│   ├── development-setup.md    # Local development
│   ├── testing-guide.md       # Testing procedures
│   └── architecture-overview.md # System design
└── reference/                   # 📚 Technical reference
    ├── api-reference.md        # Commands and endpoints
    ├── data-models.md          # Database schema
    └── hardware-specs.md       # Pi and component specs
```

## Project Structure

```
wellmonitor/
├── docs/                          # 📚 Organized documentation (12 focused guides)
├── src/
│   ├── WellMonitor.Device/        # Main device app (Raspberry Pi)
│   │   ├── Services/              # OCR, Camera, GPIO, Database, Monitoring services
│   │   ├── Models/                # Configuration and data models (39 parameters)
│   │   └── Controllers/           # API controllers
│   ├── WellMonitor.Shared/        # Shared DTOs, models, utilities
│   └── WellMonitor.AzureFunctions/# Azure Functions for PowerApp integration
├── tests/                        # Unit/integration tests
├── scripts/                      # 🔧 Organized automation scripts (8 focused tools)
│   ├── installation/             # Installation and deployment
│   ├── configuration/            # Device twin and settings management  
│   ├── diagnostics/              # System and component testing
│   └── maintenance/              # Fixes and cleanup utilities
├── .github/                      # GitHub workflows and Copilot instructions
└── README.md
```

## 🚀 Quick Start

### **Production Deployment (Recommended)**

Deploy to Raspberry Pi with full security and LED camera optimization:

```bash
cd ~/WellMonitor
git pull
chmod +x scripts/installation/install-wellmonitor.sh
./scripts/installation/install-wellmonitor.sh
```

This provides:
- ✅ **Secure Installation**: System directories with full systemd protection
- ✅ **LED Optimization**: Pre-configured for red 7-segment displays in dark environments  
- ✅ **Auto-Migration**: Safely moves existing database and debug images
- ✅ **Complete Build**: Includes git pull, build, test, and service setup

**📖 See [Installation Guide](docs/deployment/installation-guide.md) for complete setup instructions.**

### **Development Setup**

For local development environment:

```bash
# Clone repository
git clone https://github.com/davebirr/WellMonitor.git
cd WellMonitor

# Build and test
dotnet restore
dotnet build
dotnet test

# Run locally
cd src/WellMonitor.Device
dotnet run
```

**📖 See [Development Setup](docs/development/development-setup.md) for complete development guide.**

### **1. OCR Integration Status: ✅ COMPLETE**

The complete OCR monitoring integration is ready for testing! See [docs/OCR-Monitoring-Integration.md](docs/OCR-Monitoring-Integration.md) for detailed implementation guide.

### **2. Raspberry Pi Camera Setup**

To test with real pump images:

```bash
# Enable camera interface
sudo raspi-config
# Navigate to Interface Options → Camera → Enable

# Install camera dependencies
sudo apt update
sudo apt install -y libcamera-apps

# Test camera capture
libcamera-still -o test_image.jpg --width 1920 --height 1080

# Position camera to view your pump's LED display
# Ensure good lighting and clear view of current readings
```

### **3. Deploy and Test**

```bash
# Build and deploy
cd src/WellMonitor.Device
dotnet publish -c Release -o /home/pi/wellmonitor
sudo systemctl restart wellmonitor

# Monitor live OCR processing
sudo journalctl -u wellmonitor -f | grep -E "(OCR|Reading|Status)"
```

## Data Logging & Sync Strategy


See [docs/DataLoggingAndSync.md](docs/DataLoggingAndSync.md) for the full logging/sync strategy.
See [docs/DataModel.md](docs/DataModel.md) for the full data model and schema.

**Summary:**
- Log high-frequency readings locally (SQLite) for 7–30 days for detailed graphs and troubleshooting.
- Aggregate and store daily/monthly kWh summaries for long-term billing.
- Sync periodic telemetry and summaries to Azure IoT Hub, with local queuing and retry if offline.
- Use local data for graphs; use monthly summaries for billing.
- See the full document for schema, retention, and implementation notes.

---

## Getting Started

### 1. Raspberry Pi Setup
- Flash Raspberry Pi OS, enable SSH/Wi-Fi
- Secure SSH access (use keys, disable password login)
- Update and install dependencies (see `docs/Raspberry Pi 4 Azure IoT Setup Guide.md`)

### 2. Azure IoT Hub Registration & Secrets Management
- Register your device in Azure IoT Hub
- Set up secure secrets management (see `docs/SecretsManagement.md`)
- Choose from: Azure Key Vault, Environment Variables, or Hybrid approach

### 3. Build and Deploy
- Build the .NET 8 application for ARM (Raspberry Pi)
- Deploy to the Pi and configure as a service if desired

### 4. PowerApp Integration
- Use Azure Functions or Logic Apps to bridge PowerApp and IoT Hub
- Expose endpoints for status and relay control

## Telemetry Message Example

```
{
  "timestamp": "2025-07-10T14:00:00Z",
  "currentDrawAmps": 5.2,
  "status": "Normal",
  "energyKWh": {
    "hour": 0.52,
    "day": 3.8,
    "month": 112.4
  }
}
```

## Coding Guidelines
- C# 10+, .NET 8+
- Async/await for all I/O
- Dependency injection for services
- Use `ILogger<T>` for logging
- POCOs and DTOs for data
- Follow .NET naming conventions and best practices

## Security & Secrets Management
- 🔐 **Secure secrets management** with Azure Key Vault, Environment Variables, or Hybrid approach
- 🔒 Keep your Pi and dependencies updated
- 🔑 Use SSH keys, not passwords
- 🚨 Never commit secrets to version control
- 📖 **Full security guide**: [`docs/SecretsManagement.md`](docs/SecretsManagement.md)

## Scripts and Automation

The project includes organized automation scripts for all deployment and management tasks:

### **🔧 Scripts Directory Structure**
```
scripts/
├── installation/        # Deployment and setup
│   ├── install-wellmonitor.sh    # Complete secure installation
│   ├── sync-and-run.sh          # Quick development sync
│   └── Deploy-ToPi.ps1          # Windows-based deployment
├── configuration/       # Device twin and settings
│   ├── update-device-twin.ps1   # Unified device twin management
│   └── Setup-AzureCli.ps1       # Azure CLI setup
├── diagnostics/         # Testing and troubleshooting
│   ├── diagnose-system.sh       # Comprehensive system diagnostics
│   ├── diagnose-camera.sh       # Camera-specific testing
│   └── diagnose-service.sh      # Service status and logs
└── maintenance/         # Fixes and cleanup
    ├── fix-camera-settings.sh   # Camera issue resolution
    └── cleanup-redundant-files.ps1 # Project cleanup
```

### **Quick Commands**
```bash
# Complete installation
./scripts/installation/install-wellmonitor.sh

# System diagnostics  
./scripts/diagnostics/diagnose-system.sh

# Configure LED optimization (from Windows)
.\scripts\configuration\update-device-twin.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice" -ConfigType "led"
```

**📖 See [scripts/README.md](scripts/README.md) for complete scripts documentation.**

## References

**📚 Updated Documentation Structure** 
- [docs/README.md](docs/README.md) - Complete documentation index with organized guides
- [docs/deployment/installation-guide.md](docs/deployment/installation-guide.md) - Complete setup process
- [docs/configuration/camera-ocr-setup.md](docs/configuration/camera-ocr-setup.md) - Hardware and OCR optimization
- [docs/deployment/troubleshooting-guide.md](docs/deployment/troubleshooting-guide.md) - Problem solving guide
- [scripts/README.md](scripts/README.md) - Complete scripts documentation

**📈 Recent Improvements**
- **Documentation Consolidation**: Reduced from 42+ files to 12 organized guides (-70% reduction)
- **Scripts Consolidation**: Reduced from 35+ scripts to 8 focused tools (-77% reduction)  
- **Improved Organization**: Logical directory structure with clear separation of concerns
- **Enhanced Usability**: Unified interfaces and comprehensive documentation

**🔗 External References**
- Azure IoT Hub Documentation
- Azure IoT Device SDK for .NET  
- Raspberry Pi Documentation
