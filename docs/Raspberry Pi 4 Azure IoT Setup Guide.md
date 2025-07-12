# Raspberry Pi 4 Azure IoT Setup Guide

## 1. Initial Raspberry Pi Setup

1. **Flash Raspberry Pi OS**  
   - Download Raspberry Pi Imager.
   - Flash the latest Raspberry Pi OS to your SD card.
   - Enable SSH and Wi-Fi in the Imager’s advanced settings (or add `ssh` file to `/boot`).

2. **Boot and Connect**  
   - Insert SD card, power on Pi, and connect to your network.
   - Find your Pi’s IP address (check your router or use `ping raspberrypi.local`).

---

## 2. Secure SSH Access

1. **Generate SSH Key (on your local computer):**
   ```bash
   ssh-keygen -t rsa -b 4096 -C "your_email@example.com"
   ```

2. **Copy Public Key to Pi:**
   ```bash
   ssh-copy-id pi@<raspberrypi-ip>
   ```
   Or manually append your public key to /home/pi/.ssh/authorized_keys.

3. **Disable Password Authentication (on the Pi):**
   ```bash
   sudo nano /etc/ssh/sshd_config
   ```
   Set:
   ```
   PubkeyAuthentication yes
   PasswordAuthentication no
   ```

   Restart SSH:
   ```
   sudo systemctl restart ssh
   ```

---

## 3. Update and Prepare the Pi

   ```
   sudo apt update && sudo apt upgrade -y
   sudo apt install python3-pip python3-venv git
   ```

---

## 3a. Install .NET 8 Runtime (and SDK, optional) for .NET/C# Projects

For .NET/C# device apps, install the .NET 8 runtime (and SDK if you want to build on the Pi):

```bash
# Install latest .NET 8 runtime (ARM64)
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0 --runtime dotnet

# Create the directory with sudo 
sudo mkdir -p /usr/share/dotnet

# (Optional) Install latest .NET 8 SDK (ARM64)
sudo ./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
sudo ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

# Add to PATH for current session
export PATH=$PATH:/home/davidb/.dotnet:/usr/share/dotnet

# To make it permanent, add the export line to ~/.bashrc or ~/.profile

# Test installation
dotnet --info
```

---

## 3b. Install Native Dependencies for .NET IoT

```bash
sudo apt install libgpiod-dev libcamera-dev sqlite3 libsqlite3-dev tesseract-ocr
```
- `libgpiod-dev`: GPIO access for .NET IoT libraries
- `libcamera-dev`: Camera support
- `sqlite3` and `libsqlite3-dev`: For local data logging
- `tesseract-ocr`: For OCR (Tesseract)

---

## 3c. (Optional) Set Up Tesseract Data Files

If you need additional language packs for Tesseract:
```bash
sudo apt install tesseract-ocr-eng tesseract-ocr-osd
```

---

## 3d. (Optional) Enable Camera and I2C

Use `raspi-config` to enable camera and I2C if needed:
```bash
sudo raspi-config
# Interfacing Options > Camera: Enable
# Interfacing Options > I2C: Enable
```

---

## 3e. Set up Github CLI

   To Install Gibhub CLI
   ```bash
   (type -p wget >/dev/null || (sudo apt update && sudo apt install wget -y)) \
      && sudo mkdir -p -m 755 /etc/apt/keyrings \
      && out=$(mktemp) && wget -nv -O$out https://cli.github.com/packages/githubcli-archive-keyring.gpg \
      && cat $out | sudo tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null \
      && sudo chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
      && sudo mkdir -p -m 755 /etc/apt/sources.list.d \
      && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
      && sudo apt update \
      && sudo apt install gh -y
   ```

   To Update Github CLI
   ```bash
   sudo apt update
   sudo apt install gh
   ```

## 3f. Clone Repository and Configure Git

   ```bash
   # Authenticate with GitHub CLI
   gh auth login
   
   # Clone the repository
   git clone https://github.com/davebirr/WellMonitor.git
   cd WellMonitor
   
   # Configure git for cross-platform compatibility
   git config core.autocrlf input
   git config core.filemode false
   
   # The scripts should now have proper permissions
   ls -la scripts/
   ```

   **Note:** The repository includes `.gitattributes` to handle line endings and executable permissions automatically. The sync scripts (`scripts/sync-and-run.sh` and `scripts/quick-sync.sh`) should be executable without manual `chmod`.

## 3g. Configure Python Environment for WellMonitor OCR

   The WellMonitor application uses Python OCR as a fallback when the .NET Tesseract library has issues. Ensure the Python virtual environment is activated before running the application:

   ```bash
   # Activate the Python virtual environment (required for OCR)
   source ~/iotenv/bin/activate
   
   # Verify OCR dependencies are installed
   python3 -c "import pytesseract, PIL, cv2, numpy; print('OCR dependencies OK')"
   
   # Run WellMonitor with Python OCR support
   cd WellMonitor
   ./scripts/sync-and-run.sh
   ```

   **Important:** Always activate the Python virtual environment before starting WellMonitor to ensure OCR functionality works properly.

   **OCR Provider Recommendation:** For Raspberry Pi ARM64, use `"ocrProvider": "Python"` in your Azure IoT Hub device twin configuration. The .NET Tesseract library has ARM64 compatibility issues, but Python OCR provides excellent performance and reliability on Pi hardware.

---

## 4. Set Up Python Virtual Environment

```bash
   # Create and activate virtual environment
   python3 -m venv ~/iotenv
   source ~/iotenv/bin/activate
   
   # Upgrade pip and install core packages
   pip install --upgrade pip
   pip install azure-iot-device
   
   # Install OCR dependencies for WellMonitor
   pip install pytesseract pillow opencv-python numpy
   
   # Install other IoT packages as needed
   pip install sense-hat picamera
   ```

   **Note:** The WellMonitor application requires `pytesseract`, `pillow`, `opencv-python`, and `numpy` for OCR functionality. These must be installed in the virtual environment to avoid "externally managed environment" errors on modern Python installations.

   To activate the environment later:
   ```bash
   source ~/iotenv/bin/activate
   ```

   ---

   ## 5. Install Azure CLI

   The Azure CLI is essential for managing Azure IoT Hub device twins, monitoring telemetry, and running automated deployment scripts:

   ```bash
   # Install Azure CLI for Debian/Ubuntu on ARM64
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   
   # Verify installation
   az --version
   
   # Login to Azure (requires device code authentication)
   az login --use-device-code
   
   # Set default subscription (if you have multiple)
   az account set --subscription "Your Subscription Name"
   
   # Verify IoT Hub access
   az iot hub list --query "[].name" -o table
   ```

   **Benefits of Azure CLI on Pi:**
   - **Device Twin Management**: Update configuration remotely using scripts
   - **Telemetry Monitoring**: View real-time device messages 
   - **Automated Deployment**: Use deployment scripts for updates
   - **Troubleshooting**: Query device status and logs
   - **Development**: Test device twin changes before applying

   **WellMonitor Specific Usage:**
   ```bash
   # Update debug image path configuration
   ./scripts/update-debug-image-path.sh
   
   # Update OCR provider settings
   ./scripts/Update-DeviceTwinOCR.ps1  # (PowerShell script)
   
   # Monitor device telemetry
   az iot hub monitor-events --hub-name YourIoTHub --device-id YourDeviceId
   ```

   ---

   ## 6. Register Device in Azure IoT Hub
   In Azure Portal:
   Go to your IoT Hub.
   Click IoT Devices > + New.
   Enter a unique Device ID (e.g., rpi4b-well01).
   Save and copy the Primary Connection String.

   ---

   ## 7. Create and Run Your Python Script
   1. **Create a script (e.g., send_message.py):**

   ```
   from azure.iot.device import IoTHubDeviceClient, Message

   CONNECTION_STRING = "YOUR_DEVICE_CONNECTION_STRING"

   def main():
      client = IoTHubDeviceClient.create_from_connection_string(CONNECTION_STRING)
      msg = Message('{"hello": "world"}')
      client.send_message(msg)
      print("Message sent!")

   if __name__ == "__main__":
      main()
   ```

   2. **Run the script:**
   ```
   python send_message.py
   ```

   ---

   ## 8. Monitor Device in Azure
   Use Azure IoT Explorer or the Azure Portal to view messages and device status.

   ---
   
   ## 9. (Optional) Set Up Remote Desktop
   ```
   sudo apt install xrdp
   ```

   ---

   ## 9. (Optional) Enable Sense HAT and Camera
   Sense HAT:
   Install with sudo apt install sense-hat
   Camera:
   Use the picamera Python library.

   ---

   ## 10. Security Best Practices
   Keep your Pi updated (sudo apt update && sudo apt upgrade).
   Use SSH keys, not passwords.
   Never share your device connection string or private SSH key.

   References
   Azure IoT Hub Documentation
   Azure IoT Device SDK for Python
   Raspberry Pi Documentation

---

## 10a. Troubleshooting OCR and Debug Images

### Check Debug Images
```bash
# Check if debug images are being saved
ls -la ~/WellMonitor/debug_images/
ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/

# If directory doesn't exist, create it manually for testing
mkdir -p ~/WellMonitor/debug_images

# Check current working directory when app runs
pwd
ls -la
```

### OCR Provider Debugging
```bash
# Check Python OCR dependencies in virtual environment
source ~/iotenv/bin/activate
python3 -c "import pytesseract, PIL, cv2, numpy; print('OCR dependencies OK')"

# Test Python OCR manually
python3 -c "
import pytesseract
from PIL import Image
import cv2
import numpy as np
print('Python OCR test:', pytesseract.get_tesseract_version())
"
```

### Device Twin Configuration Check
Log into Azure IoT Hub and verify your device twin desired properties include:
```json
{
  "ocrProvider": "Python",
  "debugImageSaveEnabled": true,
  "cameraDebugImagePath": "debug_images"
}
```

**Known Issues:**
- OCR service initializes before Python provider is ready (timing issue)
- Debug images should now save if both flags are set correctly
- Device twin validation warnings are resolved ✅

---

## 11. WellMonitor Specific Setup

### Script Permissions Fix
After cloning the WellMonitor repository, you may encounter "Permission denied" errors when running shell scripts. This is due to git on Windows not preserving Unix executable permissions:

```bash
# Quick fix - run after any git pull that gives permission denied errors
./scripts/fix-script-permissions.sh

# If that script itself isn't executable, use:
chmod +x scripts/*.sh
./scripts/fix-script-permissions.sh

# Set up automatic permission fixing (recommended)
./scripts/setup-git-hooks.sh
```

### WellMonitor Azure CLI Usage
With Azure CLI installed, you can now use the automated configuration scripts:

```bash
# Diagnose debug image path issues
./scripts/diagnose-debug-image-path.sh

# Fix debug image path configuration (requires Azure CLI)
./scripts/update-debug-image-path.sh

# Deploy and run WellMonitor
./scripts/deploy-to-pi.sh

# Test OCR functionality
./scripts/test-ocr.sh
```

### Device Twin Configuration
Update device twin settings for optimal Pi performance:

```bash
# Set OCR provider to Python (recommended for Pi)
az iot hub device-twin update \
    --hub-name YourIoTHub \
    --device-id YourDeviceId \
    --set properties.desired.ocrProvider="Python"

# Enable debug image saving
az iot hub device-twin update \
    --hub-name YourIoTHub \
    --device-id YourDeviceId \
    --set properties.desired.debugImageSaveEnabled=true \
    --set properties.desired.cameraDebugImagePath="/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"
```

### Monitor WellMonitor Telemetry
```bash
# Monitor real-time telemetry from your device
az iot hub monitor-events --hub-name YourIoTHub --device-id YourDeviceId

# View device twin current configuration
az iot hub device-twin show --hub-name YourIoTHub --device-id YourDeviceId
```

### Common WellMonitor Issues

**Script Permission Denied:**
- Run: `./scripts/fix-script-permissions.sh`
- Or manually: `chmod +x scripts/*.sh`

**Debug Images Not Saving:**
- Check device twin: `debugImageSaveEnabled: true`
- Verify path: `cameraDebugImagePath` is set correctly
- Run diagnostic: `./scripts/diagnose-debug-image-path.sh`

**OCR Not Working:**
- Ensure Python virtual environment is active: `source ~/iotenv/bin/activate`
- Test OCR dependencies: `python3 -c "import pytesseract; print('OK')"`
- Set device twin: `ocrProvider: "Python"` (recommended for Pi)

**Service Not Starting:**
- Check logs: `sudo journalctl -u wellmonitor -f`
- Verify .NET installation: `dotnet --info`
- Check GPIO permissions: `sudo usermod -a -G gpio $USER`