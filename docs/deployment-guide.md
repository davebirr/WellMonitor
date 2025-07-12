# WellMonitor Raspberry Pi Deployment Guide

This guide covers deploying the WellMonitor application to a Raspberry Pi 4B with full security and proper systemd service configuration.

## Quick Start (Recommended)

For a complete build and secure installation in one command:

```bash
cd ~/WellMonitor
git pull
chmod +x scripts/install-wellmonitor-complete.sh
./scripts/install-wellmonitor-complete.sh --clean
```

This automatically:
- ✅ Pulls latest code changes
- ✅ Builds the application for linux-arm64
- ✅ Installs to secure system directories
- ✅ Migrates database and debug images
- ✅ Configures environment variables
- ✅ Enables full systemd security protections
- ✅ Starts the service

## Installation Locations

After secure installation:

| Component | Location | Purpose |
|-----------|----------|---------|
| **Application** | `/opt/wellmonitor/` | Executable and libraries |
| **Data** | `/var/lib/wellmonitor/` | Database and application data |
| **Configuration** | `/etc/wellmonitor/` | Environment variables and config |
| **Debug Images** | `/var/lib/wellmonitor/debug_images/` | Camera debug images |
| **Logs** | System journal | Use `journalctl -u wellmonitor` |

## Security Features

The secure installation includes:

- ✅ **ProtectHome=yes** - No access to user home directories
- ✅ **ProtectSystem=strict** - Read-only system protection
- ✅ **NoNewPrivileges=yes** - Prevents privilege escalation
- ✅ **PrivateTmp=yes** - Private temporary directory
- ✅ **Specific device access** - Only GPIO and camera devices
- ✅ **Secure environment file** - Protected configuration

## Service Management

```bash
# Check service status
sudo systemctl status wellmonitor

# View live logs
sudo journalctl -u wellmonitor -f

# Stop/start/restart service
sudo systemctl stop wellmonitor
sudo systemctl start wellmonitor
sudo systemctl restart wellmonitor

# Enable/disable auto-start
sudo systemctl enable wellmonitor
sudo systemctl disable wellmonitor
```

## Environment Configuration

Environment variables are stored securely in `/etc/wellmonitor/environment`:

```bash
# View current environment
sudo cat /etc/wellmonitor/environment

# Edit environment (requires restart)
sudo nano /etc/wellmonitor/environment
sudo systemctl restart wellmonitor
```

## LED Camera Optimization

The secure installation includes LED camera optimization settings for red 7-segment displays in dark environments. Update these via Azure IoT Hub device twin:

```bash
# Update LED camera settings
./scripts/Update-LedCameraSettings.ps1

# Test LED optimization
./scripts/Test-LedCameraOptimization.ps1
```

## Development Workflow

### For ongoing development:

```bash
# Quick rebuild and reinstall
./scripts/install-wellmonitor-complete.sh --clean

# Use existing build (if no code changes)
./scripts/install-wellmonitor-complete.sh --skip-build
```

### For testing without service installation:

```bash
# Traditional development approach
./scripts/sync-and-run.sh --clean
```

## Troubleshooting

### Service won't start:
```bash
# Check detailed status
sudo systemctl status wellmonitor -l

# Check recent logs
sudo journalctl -u wellmonitor -n 50 --no-pager

# Check environment variables
sudo systemctl show wellmonitor --property=Environment
```

### Build issues:
```bash
# Clean everything and rebuild
./scripts/install-wellmonitor-complete.sh --clean

# Check .NET installation
dotnet --info
```

### Permission issues:
```bash
# Check file permissions
ls -la /opt/wellmonitor/
ls -la /var/lib/wellmonitor/
ls -la /etc/wellmonitor/

# Fix data directory permissions
sudo chown -R davidb:davidb /var/lib/wellmonitor/
```

## Migration from Home Directory Installation

If you have an existing installation in `/home/davidb/WellMonitor/`, the secure installer automatically:

1. Stops the old service
2. Backs up the service configuration
3. Migrates the database from `~/WellMonitor/src/WellMonitor.Device/wellmonitor.db`
4. Migrates debug images from `~/WellMonitor/src/WellMonitor.Device/debug_images/`
5. Creates the new secure service configuration

## File Locations Reference

### Before (Insecure):
- Service: `/etc/systemd/system/wellmonitor.service` (with `ProtectHome=no`)
- App: `/home/davidb/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/`
- Data: `/home/davidb/WellMonitor/src/WellMonitor.Device/wellmonitor.db`
- Debug: `/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images/`

### After (Secure):
- Service: `/etc/systemd/system/wellmonitor.service` (with `ProtectHome=yes`)
- App: `/opt/wellmonitor/`
- Data: `/var/lib/wellmonitor/wellmonitor.db`
- Debug: `/var/lib/wellmonitor/debug_images/`
- Config: `/etc/wellmonitor/environment`

## Best Practices

1. **Always use the secure installer** for production deployments
2. **Test LED optimization** after installation to ensure camera settings work
3. **Monitor logs** regularly: `sudo journalctl -u wellmonitor -f`
4. **Update device twin settings** through Azure IoT Hub for configuration changes
5. **Keep backups** of `/var/lib/wellmonitor/` for database and debug images

## Azure Integration

The service automatically connects to Azure IoT Hub using the configured connection string. Monitor the device status through:

- Azure IoT Hub Device Explorer
- Device twin properties for configuration
- Telemetry messages for pump readings
- Direct methods for relay control

The LED camera optimization settings work seamlessly with the secure installation and provide optimal OCR performance for red 7-segment displays in dark basement environments.
