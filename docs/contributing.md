# Development & Testing Setup Guide

Welcome! This guide will help you set up your environment for contributing to the WellMonitor project. Please follow the steps below to ensure a smooth development experience.

---

## 1. Required Tools

Install the following tools using [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget/) (run commands in **PowerShell**):

- **Visual Studio Code**
  ```sh
  winget install --exact vscode
  ```

- **PowerShell 7**
  ```sh
  winget install --exact Microsoft.PowerShell
  ```

- **Git**
  ```sh
  winget install --exact Git.Git
  ```

- **Node.js v22.x LTS**
  ```sh
  winget install --exact OpenJS.NodeJS.LTS --version 22.13.0
  winget pin add OpenJS.NodeJS.LTS --version 22.13.* --force
  ```

- **.NET SDK 8**
  ```sh
  winget install --exact Microsoft.DotNet.SDK.8
  ```

- **.NET SDK 9**
  ```sh
  winget install --exact Microsoft.DotNet.SDK.9
  ```

- **Python 3**
  ```sh
  winget install --exact Python.Python.3.13
  ```

- **Azure CLI**
  ```sh
  winget install --exact Microsoft.AzureCLI
  ```

---

## 2. Azure CLI Extensions

After installing Azure CLI, you'll need to install the IoT extension for device management:

```sh
az extension add --name azure-iot
```

> **Note:** The azure-iot extension is required for managing Azure IoT Hub devices and device twins. It will be automatically installed the first time you run an IoT command if not already present.

---

## 3. Global npm Packages

Some npm packages need to be installed globally. You may need to run these commands as **Administrator** if you encounter permission issues.

```sh
npm install --global azure-functions-core-tools@4 --unsafe-perm true
npm install --global azurite
```

---

## 4. Repository Structure

The WellMonitor project consists of multiple components:

```
WellMonitor/
├── src/
│   ├── WellMonitor.Device/         # Raspberry Pi device application
│   ├── WellMonitor.AzureFunctions/ # Azure Functions for PowerApp integration
│   └── WellMonitor.Shared/         # Shared models and utilities
├── tests/                          # Unit tests
├── docs/                          # Documentation
└── scripts/                       # Management and deployment scripts
```

### Fork the Repository

- [Fork WellMonitor](https://github.com/davebirr/wellmonitor)

Clone your fork to your local development machine.

> **Tip:**  
> A Git repository is a `.git/` folder inside a project. It tracks all changes made to files in the project. Changes are committed to the repository, building up a history of the project.

---

## 5. Python Dependencies (for OCR Development)

The WellMonitor device application uses Python for OCR processing. If you're working on OCR improvements, install these dependencies:

```sh
pip install opencv-python pytesseract pillow numpy
```

> **Note:** For Raspberry Pi deployment, additional system packages are required:
> ```sh
> sudo apt-get install tesseract-ocr tesseract-ocr-eng python3-pip
> ```

---

## 6. Azure Authentication

To manage IoT devices and deploy to Azure, you'll need to authenticate with Azure CLI:

```sh
az login
```

Set your default subscription if you have multiple:
```sh
az account set --subscription "Your Subscription Name"
```

---

## 7. Development Workflow

### Device Twin Management
Use the provided PowerShell scripts to manage device configuration:

```sh
# Set up Azure CLI (if having PATH issues)
.\scripts\Setup-AzureCli.ps1

# Update camera settings for LED optimization
.\scripts\Update-LedCameraSettings.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice"

# Run complete camera optimization test
.\scripts\Test-LedCameraOptimization.ps1 -IoTHubName "YourHub" -DeviceId "YourDevice"
```

### Local Development
1. Set up your development environment with the required tools above
2. Clone the repository and open in VS Code
3. Use the provided scripts for Azure CLI setup: `.\scripts\Setup-AzureCli.ps1`
4. Configure your local secrets in `src/WellMonitor.Device/secrets.json`
5. Build and test the solution: `dotnet build` and `dotnet test`

### Raspberry Pi Development
For Pi-specific development and debugging:

```sh
# Use bash scripts for Pi compatibility (in scripts/ folder)
./diagnose-camera.sh
./optimize-led-camera.sh
./fix-script-permissions.sh
```

### Service Setup on Raspberry Pi
To set up WellMonitor as a systemd service:

```sh
# On your Raspberry Pi, run the service setup script
cd ~/WellMonitor
chmod +x scripts/setup-wellmonitor-service.sh
./scripts/setup-wellmonitor-service.sh
```

After service setup, you can:
```sh
# Monitor service logs in real-time
sudo journalctl -u wellmonitor -f

# Check service status
sudo systemctl status wellmonitor

# Restart service (after code changes)
sudo systemctl restart wellmonitor

# View debug images
ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/
```

---

## 8. Additional Notes

- Depending on your system, you may need to run some commands as administrator
- For Raspberry Pi deployment, see `docs/Raspberry Pi 4 Azure IoT Setup Guide.md`
- For camera optimization and LED display troubleshooting, see the scripts in the `scripts/` folder
- For more information on forking repositories, see [GitHub's guide](https://docs.github.com/en/get-started/quickstart/fork-a-repo)

---

## 9. Troubleshooting

### Azure CLI Issues
If you get "az: The term 'az' is not recognized" errors:
1. Run `.\scripts\Setup-AzureCli.ps1` to fix PATH issues
2. Restart your terminal or VS Code
3. Verify with `az --version`

### Camera Development
- Use debug images in `debug_images/` folder for OCR development
- LED optimization scripts are available for dark basement environments
- Debug images are **local only** - they stay on the Pi and are not committed to repository
- Check device twin settings with Azure CLI: `az iot hub device-twin show --hub-name YourHub --device-id YourDevice`
- Transfer debug images using WinSCP, rsync, or similar tools for analysis on development machine
---
Happy contributing!