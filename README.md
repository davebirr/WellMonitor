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


## Project Structure


```
wellmonitor/
├── docs/                          # Documentation and setup guides
│   ├── OCR-Monitoring-Integration.md # Complete OCR implementation guide
│   ├── DataLoggingAndSync.md      # Data logging & sync strategy
│   ├── DataModel.md               # Data model and schema
│   ├── DeviceTwinExtendedConfiguration.md # 39-parameter configuration guide
│   ├── SecretsManagement.md       # Secure secrets management guide
│   └── RaspberryPiDeploymentGuide.md # Pi deployment instructions
├── src/
│   ├── WellMonitor.Device/        # Main device app (Raspberry Pi)
│   │   ├── Services/              # OCR, Camera, GPIO, Database, Monitoring services
│   │   ├── Models/                # Configuration and data models (39 parameters)
│   │   └── Controllers/           # API controllers
│   ├── WellMonitor.Shared/        # Shared DTOs, models, utilities
│   └── WellMonitor.AzureFunctions/# Azure Functions for PowerApp integration
├── tests/                        # Unit/integration tests
├── .github/                      # GitHub workflows and Copilot instructions
├── README.md
└── ...
```

## 🚀 Quick Start

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

## References
- [docs/OCR-Implementation.md](docs/OCR-Implementation.md) - Comprehensive OCR documentation
- [docs/SecretsManagement.md](docs/SecretsManagement.md) - Secure secrets management guide
- [docs/RaspberryPiDeploymentGuide.md](docs/RaspberryPiDeploymentGuide.md) - Complete deployment guide
- [docs/Raspberry Pi 4 Azure IoT Setup Guide.md](docs/Raspberry%20Pi%204%20Azure%20IoT%20Setup%20Guide.md)
- Azure IoT Hub Documentation
- Azure IoT Device SDK for .NET
- Raspberry Pi Documentation
