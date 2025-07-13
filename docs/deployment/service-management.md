# WellMonitor Service Management

Complete guide for managing the WellMonitor systemd service, monitoring, and troubleshooting.

## Service Control

### Basic Operations
```bash
# Check service status
sudo systemctl status wellmonitor

# Start/stop/restart service
sudo systemctl start wellmonitor
sudo systemctl stop wellmonitor
sudo systemctl restart wellmonitor

# Enable/disable auto-start on boot
sudo systemctl enable wellmonitor
sudo systemctl disable wellmonitor

# Reload service configuration
sudo systemctl daemon-reload
```

### Service Status Information
```bash
# Detailed status with recent logs
sudo systemctl status wellmonitor -l

# Check if service is enabled for auto-start
sudo systemctl is-enabled wellmonitor

# Check if service is currently running
sudo systemctl is-active wellmonitor

# Show service configuration
sudo systemctl show wellmonitor
```

## Log Management

### Viewing Logs
```bash
# Recent logs (last 20 lines)
sudo journalctl -u wellmonitor -n 20

# Follow logs in real-time
sudo journalctl -u wellmonitor -f

# Logs since last boot
sudo journalctl -u wellmonitor -b

# Logs for specific time period
sudo journalctl -u wellmonitor --since "2024-01-01 00:00:00" --until "2024-01-01 23:59:59"

# Logs with specific priority (error, warning, info, debug)
sudo journalctl -u wellmonitor -p err
sudo journalctl -u wellmonitor -p warning
```

### Log Analysis
```bash
# Search logs for specific terms
sudo journalctl -u wellmonitor | grep "ERROR"
sudo journalctl -u wellmonitor | grep "camera"
sudo journalctl -u wellmonitor | grep "OCR"

# Export logs to file
sudo journalctl -u wellmonitor > /tmp/wellmonitor.log

# Verbose output with timestamps
sudo journalctl -u wellmonitor -o verbose
```

## Monitoring and Diagnostics

### System Resource Usage
```bash
# Check CPU and memory usage
top -p $(pgrep -f wellmonitor)

# Detailed process information
ps aux | grep wellmonitor

# Check disk usage for application directories
df -h /opt/wellmonitor /var/lib/wellmonitor /etc/wellmonitor
du -sh /var/lib/wellmonitor/*
```

### Application Health Checks
```bash
# Check if camera is accessible
ls -la /dev/video*

# Check GPIO access
ls -la /dev/gpiochip*

# Verify database accessibility
sudo -u wellmonitor sqlite3 /var/lib/wellmonitor/wellmonitor.db ".tables"

# Check recent debug images
ls -la /var/lib/wellmonitor/debug_images/ | tail -10

# Test database queries
sudo -u wellmonitor sqlite3 /var/lib/wellmonitor/wellmonitor.db "SELECT COUNT(*) FROM readings;"
```

### Network and Azure Connectivity
```bash
# Test internet connectivity
ping -c 4 8.8.8.8

# Test Azure IoT Hub connectivity (if hostname known)
nslookup your-hub.azure-devices.net

# Check environment variables
sudo systemctl show wellmonitor --property=Environment
```

## Configuration Management

### Environment Variables
```bash
# View current environment configuration
sudo cat /etc/wellmonitor/environment

# Edit environment variables
sudo nano /etc/wellmonitor/environment

# Apply configuration changes
sudo systemctl restart wellmonitor
```

### Service Configuration
```bash
# View service file
sudo systemctl cat wellmonitor

# Edit service file
sudo systemctl edit wellmonitor --full

# View effective service configuration
sudo systemctl show wellmonitor --no-pager
```

### Application Data
```bash
# Navigate to application data directory
cd /var/lib/wellmonitor

# Check database file
sudo -u wellmonitor file wellmonitor.db

# Backup application data
sudo tar -czf /tmp/wellmonitor-backup-$(date +%Y%m%d).tar.gz -C /var/lib wellmonitor

# Check debug images
ls -la debug_images/ | head -20
```

## Maintenance Operations

### Service Updates
```bash
# Update application (using installer)
cd ~/WellMonitor
git pull
./scripts/installation/install-wellmonitor.sh

# Manual service file update after changes
sudo systemctl daemon-reload
sudo systemctl restart wellmonitor
```

### Log Rotation and Cleanup
```bash
# Clear old journal logs (keep last 7 days)
sudo journalctl --vacuum-time=7d

# Clear old journal logs (keep last 100MB)
sudo journalctl --vacuum-size=100M

# Clean old debug images (older than 30 days)
sudo find /var/lib/wellmonitor/debug_images -name "*.jpg" -mtime +30 -delete
```

### Database Maintenance
```bash
# Vacuum database to reclaim space
sudo -u wellmonitor sqlite3 /var/lib/wellmonitor/wellmonitor.db "VACUUM;"

# Check database integrity
sudo -u wellmonitor sqlite3 /var/lib/wellmonitor/wellmonitor.db "PRAGMA integrity_check;"

# Backup database
sudo -u wellmonitor cp /var/lib/wellmonitor/wellmonitor.db /var/lib/wellmonitor/wellmonitor.db.backup.$(date +%Y%m%d)
```

## Emergency Procedures

### Service Recovery
```bash
# If service fails to start, check logs first
sudo journalctl -u wellmonitor -n 50

# Try restarting with verbose logging
sudo systemctl restart wellmonitor
sudo journalctl -u wellmonitor -f

# If persistent issues, try manual execution
sudo -u wellmonitor /opt/wellmonitor/WellMonitor.Device
```

### Data Recovery
```bash
# Restore from backup
sudo systemctl stop wellmonitor
sudo -u wellmonitor cp /var/lib/wellmonitor/wellmonitor.db.backup.YYYYMMDD /var/lib/wellmonitor/wellmonitor.db
sudo systemctl start wellmonitor
```

### Reset and Reinstall
```bash
# Complete service removal and clean reinstall
sudo systemctl stop wellmonitor
sudo systemctl disable wellmonitor
sudo rm /etc/systemd/system/wellmonitor.service
sudo systemctl daemon-reload

# Remove application data (if desired)
sudo rm -rf /opt/wellmonitor /var/lib/wellmonitor /etc/wellmonitor

# Reinstall using installer
cd ~/WellMonitor
./scripts/installation/install-wellmonitor.sh --clean
```

## Performance Monitoring

### Key Metrics to Monitor
- **Service Status**: Should always be "active (running)"
- **Memory Usage**: Typical usage 50-200MB
- **CPU Usage**: Should be low during idle periods
- **Disk Usage**: Monitor debug images and database growth
- **Log Errors**: Check for recurring error patterns

### Automated Monitoring Script
```bash
# Create monitoring script
sudo tee /usr/local/bin/wellmonitor-health.sh << 'EOF'
#!/bin/bash
echo "=== WellMonitor Health Check ==="
echo "Service Status: $(sudo systemctl is-active wellmonitor)"
echo "Memory Usage: $(ps -o pid,vsz,rss,comm -p $(pgrep -f wellmonitor) | tail -1)"
echo "Last Log Entry: $(sudo journalctl -u wellmonitor -n 1 --no-pager -o short-iso)"
echo "Debug Images: $(ls /var/lib/wellmonitor/debug_images/ | wc -l) files"
echo "Database Size: $(du -sh /var/lib/wellmonitor/wellmonitor.db | cut -f1)"
EOF

sudo chmod +x /usr/local/bin/wellmonitor-health.sh

# Run health check
sudo /usr/local/bin/wellmonitor-health.sh
```

## Common Commands Quick Reference

| Operation | Command |
|-----------|---------|
| Check status | `sudo systemctl status wellmonitor` |
| View logs | `sudo journalctl -u wellmonitor -f` |
| Restart service | `sudo systemctl restart wellmonitor` |
| Check environment | `sudo cat /etc/wellmonitor/environment` |
| View debug images | `ls -la /var/lib/wellmonitor/debug_images/` |
| Database query | `sudo -u wellmonitor sqlite3 /var/lib/wellmonitor/wellmonitor.db "SELECT COUNT(*) FROM readings;"` |
| Health check | `sudo /usr/local/bin/wellmonitor-health.sh` |

For more detailed troubleshooting, see the [Troubleshooting Guide](troubleshooting-guide.md).
