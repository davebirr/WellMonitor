# WellMonitor Installation Guide

Complete installation process from development environment to production deployment on Raspberry Pi 4B.

## Prerequisites

### Raspberry Pi Setup
1. **Flash Raspberry Pi OS** using Raspberry Pi Imager
2. **Enable SSH and Wi-Fi** in advanced settings
3. **Boot and connect** to your network

### Initial System Setup
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install essential packages
sudo apt install git curl wget nano

# Install .NET 8 Runtime
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0 --runtime dotnet

# Install .NET IoT dependencies
sudo apt install libgpiod-dev libcamera-dev sqlite3 libsqlite3-dev tesseract-ocr tesseract-ocr-eng

# Set up .NET PATH
echo 'export PATH=$PATH:/home/$USER/.dotnet' >> ~/.bashrc
source ~/.bashrc
```

### SSH Security (Recommended)
```bash
# Generate SSH key on your development machine
ssh-keygen -t rsa -b 4096 -C "your_email@example.com"

# Copy to Pi
ssh-copy-id pi@<raspberry-pi-ip>

# Disable password authentication on Pi
sudo nano /etc/ssh/sshd_config
# Set: PasswordAuthentication no
sudo systemctl restart ssh
```

## Installation Methods

### Method 1: Complete Automated Installation (Recommended)

For new installations or updates:

```bash
# Clone repository
cd ~
git clone https://github.com/davebirr/WellMonitor.git
cd WellMonitor

# Run complete installation (script moved to organized location)
chmod +x scripts/installation/install-wellmonitor.sh
./scripts/installation/install-wellmonitor.sh --clean
```

> **Note**: Scripts have been reorganized into logical categories:
> - `scripts/installation/` - Installation and deployment scripts  
> - `scripts/configuration/` - Device twin and settings management
> - `scripts/diagnostics/` - System diagnostics and troubleshooting
> - `scripts/maintenance/` - Maintenance and repair utilities
> 
> See [`scripts/README.md`](../../scripts/README.md) for complete script documentation.

This single command:
- ✅ Pulls latest code changes
- ✅ Builds application for linux-arm64
- ✅ Installs to secure system directories
- ✅ Migrates database and debug images
- ✅ Configures environment variables
- ✅ Sets up systemd service with full security
- ✅ Starts the service

### Method 2: Manual Development Build

For development and testing:

```bash
cd ~/WellMonitor

# Build for Raspberry Pi
dotnet publish src/WellMonitor.Device/WellMonitor.Device.csproj \
  -c Release \
  -r linux-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true

# Test run manually
cd src/WellMonitor.Device/bin/Release/net8.0/linux-arm64
export WELLMONITOR_SECRETS_MODE=environment
export WELLMONITOR_IOTHUB_CONNECTION_STRING="your-connection-string"
./WellMonitor.Device
```

## Installation Locations

After secure installation, components are organized as follows:

| Component | Location | Purpose | Permissions |
|-----------|----------|---------|-------------|
| **Application** | `/opt/wellmonitor/` | Executable and libraries | `root:root` (755) |
| **Data Directory** | `/var/lib/wellmonitor/` | Database and application data | `wellmonitor:wellmonitor` (755) |
| **Configuration** | `/etc/wellmonitor/` | Environment variables | `root:wellmonitor` (640) |
| **Debug Images** | `/var/lib/wellmonitor/debug_images/` | Camera debug images | `wellmonitor:wellmonitor` (755) |
| **Service Logs** | System journal | Application logs | Use `journalctl -u wellmonitor` |

## Configuration

### Required Environment Variables

The application requires these environment variables:

```bash
# Essential - Azure IoT Hub connection
WELLMONITOR_IOTHUB_CONNECTION_STRING="HostName=your-hub.azure-devices.net;DeviceId=your-device;SharedAccessKey=your-key"

# Recommended - Additional services
WELLMONITOR_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
WELLMONITOR_OCR_API_KEY="your-azure-cognitive-services-key"

# System configuration
WELLMONITOR_SECRETS_MODE="environment"
```

### Environment Variable Setup

Environment variables are automatically configured during installation in `/etc/wellmonitor/environment`:

```bash
# View current configuration
sudo cat /etc/wellmonitor/environment

# Edit configuration
sudo nano /etc/wellmonitor/environment

# Apply changes
sudo systemctl restart wellmonitor
```

## Security Features

The secure installation implements enterprise-grade security:

### Systemd Security Protections
- ✅ **ProtectHome=yes** - No access to user home directories
- ✅ **ProtectSystem=strict** - Read-only system protection
- ✅ **NoNewPrivileges=yes** - Prevents privilege escalation
- ✅ **PrivateTmp=yes** - Private temporary directory
- ✅ **RestrictNamespaces=yes** - Limits namespace access

### Device Access Controls
- ✅ **DeviceAllow** - Only GPIO and camera devices allowed
- ✅ **Specific GPIO access** - Only `/dev/gpiochip*` devices
- ✅ **Camera access** - Only `/dev/video*` devices

### File System Permissions
- ✅ **Dedicated user account** - `wellmonitor` service user
- ✅ **Minimal file permissions** - Read-only application, write access only to data directory
- ✅ **Protected configuration** - Environment file accessible only to service user

## Verification

After installation, verify the service is working:

```bash
# Check service status
sudo systemctl status wellmonitor

# View recent logs
sudo journalctl -u wellmonitor -n 20 -f

# Test camera capture
ls -la /var/lib/wellmonitor/debug_images/

# Verify database
sudo -u wellmonitor sqlite3 /var/lib/wellmonitor/wellmonitor.db ".tables"
```

## Updates

To update the application:

```bash
cd ~/WellMonitor
git pull
./scripts/installation/install-wellmonitor.sh
```

The installer automatically:
- Stops the service
- Backs up existing data
- Installs new version
- Migrates data if needed
- Restarts service

## Next Steps

1. **Configure device twin settings** - See [Configuration Guide](../configuration/configuration-guide.md)
2. **Set up camera and OCR** - See [Camera & OCR Setup](../configuration/camera-ocr-setup.md)
3. **Monitor service operation** - See [Service Management](service-management.md)
4. **Connect to PowerApp** - See [Azure Integration](../configuration/azure-integration.md)

## Troubleshooting

If you encounter issues, see the [Troubleshooting Guide](troubleshooting-guide.md) for common problems and solutions.

### Quick Diagnostic Commands
```bash
# Check service status and recent logs
sudo systemctl status wellmonitor
sudo journalctl -u wellmonitor -n 20 -f

# Run system diagnostics (organized scripts)
./scripts/diagnostics/diagnose-system.sh
./scripts/diagnostics/diagnose-service.sh
./scripts/diagnostics/diagnose-camera.sh

# Check device configuration
./scripts/diagnostics/check-device-configuration.sh
```

See [`scripts/diagnostics/`](../../scripts/diagnostics/) for complete diagnostic tools.
