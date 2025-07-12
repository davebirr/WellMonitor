# Environment Variables Setup for WellMonitor Service

⚠️ **DEPRECATED**: This document describes manual environment variable setup. 

**For new installations, use the automated secure installer instead:**
```bash
./scripts/install-wellmonitor-complete.sh --clean
```

This automatically handles all environment variables, builds the application, and installs it securely to system directories with full systemd protection.

---

## Manual Environment Variable Setup (Legacy)

The WellMonitor application requires specific environment variables when running as a service. The SIGABRT error is likely due to missing required configuration.

### Required Environment Variables

The application expects these environment variables:

### Required (IoT Hub Connection)
```bash
WELLMONITOR_IOTHUB_CONNECTION_STRING="HostName=your-hub.azure-devices.net;DeviceId=your-device;SharedAccessKey=your-key"
```

### Optional (but recommended)
```bash
WELLMONITOR_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
WELLMONITOR_LOCAL_ENCRYPTION_KEY="your-32-character-encryption-key-here"
WELLMONITOR_POWERAPP_API_KEY="your-powerapp-api-key"
WELLMONITOR_OCR_API_KEY="your-azure-cognitive-services-key"
```

### System Configuration
```bash
WELLMONITOR_SECRETS_MODE="environment"  # Use environment variables instead of secrets.json
```

## Setting Environment Variables for systemd Service

You have several options to set environment variables for the systemd service:

### Option 1: Update Service File (Recommended)
Edit the service file to include your environment variables:

```bash
sudo systemctl edit wellmonitor --full
```

Add your environment variables in the `[Service]` section:
```ini
[Service]
Environment=WELLMONITOR_SECRETS_MODE=environment
Environment=WELLMONITOR_IOTHUB_CONNECTION_STRING=your-connection-string-here
Environment=WELLMONITOR_STORAGE_CONNECTION_STRING=your-storage-connection-string
# Add other variables as needed
```

### Option 2: Use Environment File
Create an environment file and reference it in the service:

```bash
# Create environment file
sudo nano /etc/wellmonitor/environment

# Add variables (one per line):
WELLMONITOR_SECRETS_MODE=environment
WELLMONITOR_IOTHUB_CONNECTION_STRING=your-connection-string-here
WELLMONITOR_STORAGE_CONNECTION_STRING=your-storage-connection-string
```

Then update the service file:
```ini
[Service]
EnvironmentFile=/etc/wellmonitor/environment
```

### Option 3: Use systemctl set-environment
```bash
sudo systemctl set-environment WELLMONITOR_SECRETS_MODE=environment
sudo systemctl set-environment WELLMONITOR_IOTHUB_CONNECTION_STRING="your-connection-string"
```

## Testing Environment Variables

Test if your environment variables are accessible:

```bash
# Test as your user
echo $WELLMONITOR_IOTHUB_CONNECTION_STRING

# Test in systemd context
sudo systemd-run --uid=davidb --gid=davidb env | grep WELLMONITOR
```

## Manual Testing with Environment Variables

Test the application manually with environment variables:

```bash
cd ~/WellMonitor/src/WellMonitor.Device

# Set environment variables for this session
export WELLMONITOR_SECRETS_MODE=environment
export WELLMONITOR_IOTHUB_CONNECTION_STRING="your-connection-string-here"

# Run the application
./bin/Release/net8.0/linux-arm64/WellMonitor.Device
```

## Debugging Steps

1. **Check if IoT Hub connection string is set:**
   ```bash
   echo $WELLMONITOR_IOTHUB_CONNECTION_STRING
   ```

2. **Check service environment:**
   ```bash
   sudo systemctl show wellmonitor --property=Environment
   ```

3. **View detailed service logs:**
   ```bash
   sudo journalctl -u wellmonitor -n 50 --no-pager -o verbose
   ```

4. **Test manual execution with verbose logging:**
   ```bash
   cd ~/WellMonitor/src/WellMonitor.Device
   export ASPNETCORE_ENVIRONMENT=Development
   export WELLMONITOR_SECRETS_MODE=environment
   export WELLMONITOR_IOTHUB_CONNECTION_STRING="your-connection-string"
   ./bin/Release/net8.0/linux-arm64/WellMonitor.Device
   ```

The most critical environment variable is `WELLMONITOR_IOTHUB_CONNECTION_STRING`. Without it, the application will fail to start.
