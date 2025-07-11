# Well Monitoring .NET Application

This project is a .NET 8 application written in C# for the Raspberry Pi 4B. It monitors a water well using a camera and display device, integrates with Azure IoT Hub, and provides remote monitoring and control for tenants via PowerApp.

## Features

- Captures images of the well pump's current display using a camera
- Uses OCR (Tesseract or Azure Cognitive Services) to extract current readings and status messages
- Calculates energy consumption (kWh) per hour, day, and month
- Detects abnormal states: 'Dry' (low current) and 'rcyc' (rapid cycling)
- Controls a relay via GPIO to cycle power when 'rcyc' is detected (with debounce)
- Sends telemetry (timestamp, current, status, energy) to Azure IoT Hub
- Receives direct method calls from PowerApp to cycle power
- Exposes device status and control endpoints for PowerApp integration
- Logs all relay actions and manual overrides


## Project Structure


```
wellmonitor/
├── docs/                          # Documentation and setup guides
│   ├── DataLoggingAndSync.md      # Data logging & sync strategy
│   ├── DataModel.md               # Data model and schema
│   ├── SecretsManagement.md       # Secure secrets management guide
│   └── RaspberryPiDeploymentGuide.md # Pi deployment instructions
├── src/
│   ├── WellMonitor.Device/        # Main device app (Raspberry Pi)
│   ├── WellMonitor.Shared/        # Shared DTOs, models, utilities
│   └── WellMonitor.AzureFunctions/# Azure Functions for PowerApp integration
├── tests/                        # Unit/integration tests
├── .github/                      # GitHub workflows and Copilot instructions
├── README.md
└── ...
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
- [docs/SecretsManagement.md](docs/SecretsManagement.md) - Secure secrets management guide
- [docs/RaspberryPiDeploymentGuide.md](docs/RaspberryPiDeploymentGuide.md) - Complete deployment guide
- [docs/Raspberry Pi 4 Azure IoT Setup Guide.md](docs/Raspberry%20Pi%204%20Azure%20IoT%20Setup%20Guide.md)
- Azure IoT Hub Documentation
- Azure IoT Device SDK for .NET
- Raspberry Pi Documentation
