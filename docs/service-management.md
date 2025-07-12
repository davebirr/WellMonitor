# WellMonitor Service Management Reference

This document provides quick reference commands for managing the WellMonitor systemd service after secure installation.

## Service Status and Control

### Check Service Status
```bash
# Basic status
sudo systemctl status wellmonitor

# Detailed status with recent logs
sudo systemctl status wellmonitor -l

# Check if service is enabled for auto-start
sudo systemctl is-enabled wellmonitor

# Check if service is currently active
sudo systemctl is-active wellmonitor
```

### Start/Stop/Restart Service
```bash
# Start the service
sudo systemctl start wellmonitor

# Stop the service
sudo systemctl stop wellmonitor

# Restart the service
sudo systemctl restart wellmonitor

# Reload service configuration (after editing service file)
sudo systemctl daemon-reload
sudo systemctl restart wellmonitor
```

### Enable/Disable Auto-Start
```bash
# Enable auto-start on boot
sudo systemctl enable wellmonitor

# Disable auto-start on boot
sudo systemctl disable wellmonitor
```

## Log Management

### View Logs
```bash
# View recent logs
sudo journalctl -u wellmonitor -n 50 --no-pager

# Follow logs in real-time
sudo journalctl -u wellmonitor -f

# View logs from last 24 hours
sudo journalctl -u wellmonitor --since "24 hours ago"

# View logs from specific time
sudo journalctl -u wellmonitor --since "2025-07-12 10:00:00"

# View logs with verbose output
sudo journalctl -u wellmonitor -n 50 --no-pager -o verbose
```

### Export Logs
```bash
# Export logs to file
sudo journalctl -u wellmonitor --since "24 hours ago" > wellmonitor-logs.txt

# Export logs in JSON format
sudo journalctl -u wellmonitor --since "24 hours ago" -o json > wellmonitor-logs.json
```

## Configuration Management

### Environment Variables
```bash
# View current environment configuration
sudo cat /etc/wellmonitor/environment

# Edit environment variables (requires service restart)
sudo nano /etc/wellmonitor/environment

# After editing, restart service
sudo systemctl restart wellmonitor

# Check environment variables in service
sudo systemctl show wellmonitor --property=Environment
```

### Service File
```bash
# View current service configuration
cat /etc/systemd/system/wellmonitor.service

# Edit service file (advanced users only)
sudo systemctl edit wellmonitor --full

# After editing service file
sudo systemctl daemon-reload
sudo systemctl restart wellmonitor
```

## File Locations

### Application Files
```bash
# Application binaries
ls -la /opt/wellmonitor/

# Check main executable
ls -la /opt/wellmonitor/WellMonitor.Device

# Application configuration
cat /opt/wellmonitor/appsettings.json
```

### Data Files
```bash
# Database and application data
ls -la /var/lib/wellmonitor/

# Database files
ls -la /var/lib/wellmonitor/wellmonitor.db*

# Debug images
ls -la /var/lib/wellmonitor/debug_images/

# Check database size
du -sh /var/lib/wellmonitor/wellmonitor.db*
```

### Configuration Files
```bash
# Environment configuration
ls -la /etc/wellmonitor/

# Environment file permissions (should be 640)
ls -la /etc/wellmonitor/environment
```

## Troubleshooting Commands

### Service Won't Start
```bash
# Check detailed service status
sudo systemctl status wellmonitor -l

# Check recent error logs
sudo journalctl -u wellmonitor -n 20 --no-pager

# Check if executable exists and has correct permissions
ls -la /opt/wellmonitor/WellMonitor.Device

# Test manual execution
sudo -u davidb /opt/wellmonitor/WellMonitor.Device --version
```

### Permission Issues
```bash
# Fix data directory permissions
sudo chown -R davidb:davidb /var/lib/wellmonitor/

# Fix environment file permissions
sudo chown root:davidb /etc/wellmonitor/environment
sudo chmod 640 /etc/wellmonitor/environment

# Check application file permissions
sudo chmod +x /opt/wellmonitor/WellMonitor.Device
```

### Database Issues
```bash
# Check database file
ls -la /var/lib/wellmonitor/wellmonitor.db*

# Check database accessibility
sudo -u davidb sqlite3 /var/lib/wellmonitor/wellmonitor.db ".tables"

# Backup database
sudo cp /var/lib/wellmonitor/wellmonitor.db* ~/backup-$(date +%Y%m%d)/
```

## Performance Monitoring

### Resource Usage
```bash
# Check service memory usage
sudo systemctl show wellmonitor --property=MemoryCurrent

# Monitor resource usage
sudo systemd-run --scope --slice=system --uid=davidb top -p $(pgrep -f WellMonitor.Device)

# Check service restart count
sudo systemctl show wellmonitor --property=NRestarts
```

### Camera and GPIO
```bash
# Check camera device access
ls -la /dev/video*

# Check GPIO access (requires gpio group membership)
ls -la /dev/gpiochip*

# Test camera capture (as service user)
sudo -u davidb v4l2-ctl --device=/dev/video0 --info
```

## Reinstallation

### Clean Reinstall
```bash
# Stop and remove current service
sudo systemctl stop wellmonitor
sudo systemctl disable wellmonitor
sudo rm /etc/systemd/system/wellmonitor.service
sudo systemctl daemon-reload

# Run complete secure installation
cd ~/WellMonitor
git pull
./scripts/install-wellmonitor-complete.sh --clean
```

### Update Only (Keep Data)
```bash
# Update application without losing data
cd ~/WellMonitor
git pull
./scripts/install-wellmonitor-complete.sh --skip-build
```

## Security Verification

### Check Security Settings
```bash
# Verify service security settings
sudo systemctl show wellmonitor | grep -E "(ProtectHome|ProtectSystem|NoNewPrivileges)"

# Should show:
# ProtectHome=yes
# ProtectSystem=strict
# NoNewPrivileges=yes
```

### Verify File Permissions
```bash
# Application files (should be owned by root)
ls -la /opt/wellmonitor/ | head -5

# Data files (should be owned by davidb)
ls -la /var/lib/wellmonitor/ | head -5

# Config files (should be root:davidb with 640)
ls -la /etc/wellmonitor/
```

This reference covers the most common service management tasks for the WellMonitor application running as a secure systemd service.
