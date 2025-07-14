# Web Dashboard Network Access - Quick Start Example

This example shows how to make the WellMonitor dashboard accessible from other devices on your network.

## Step 1: Update Device Twin Configuration

Run the PowerShell script to enable network access:

```powershell
# Navigate to the WellMonitor directory
cd /path/to/WellMonitor

# Run the device twin update script
./scripts/configuration/Update-ExtendedDeviceTwin.ps1
```

Or manually update specific settings in the script before running it:

```powershell
# In scripts/configuration/Update-ExtendedDeviceTwin.ps1, modify these values:
"webPort" = 8080                    # Custom port
"webAllowNetworkAccess" = $true     # Enable network access
"webBindAddress" = "0.0.0.0"        # Listen on all interfaces
"webEnableAuthentication" = $true   # Require login
"webAuthUsername" = "wellmonitor"   # Custom username
```

## Step 2: Set Authentication Password (Optional)

If you enabled authentication, set the password via environment variable:

```bash
# Add to /etc/environment
echo "WEB_AUTH_PASSWORD=mySecurePassword123" | sudo tee -a /etc/environment

# Or set for the systemd service
sudo systemctl edit wellmonitor
```

Add this content to the service override:
```ini
[Service]
Environment="WEB_AUTH_PASSWORD=mySecurePassword123"
```

## Step 3: Configure Firewall

Allow access to your chosen port:

```bash
# Allow the web dashboard port (replace 8080 with your port)
sudo ufw allow 8080

# Or allow from specific network only (more secure)
sudo ufw allow from 192.168.1.0/24 to any port 8080
```

## Step 4: Restart Service

Restart the WellMonitor service to apply changes:

```bash
sudo systemctl restart wellmonitor
```

## Step 5: Access Dashboard

The dashboard will be accessible from any device on your network:

```
http://[raspberry-pi-ip]:8080
```

For example:
- `http://192.168.1.100:8080`
- `http://wellmonitor.local:8080`

If authentication is enabled, use:
- **Username**: `wellmonitor` (or your custom username)
- **Password**: `mySecurePassword123` (your environment variable)

## Verification

Check that the service is running and listening:

```bash
# Check service status
sudo systemctl status wellmonitor

# Verify port is listening
sudo netstat -tlnp | grep :8080

# Test from another device
curl http://[pi-ip]:8080/health
```

## Live Configuration Updates

You can update the configuration without restarting:

1. **Option A**: Update device twin in Azure IoT Hub portal
2. **Option B**: Run the PowerShell script again with new values
3. **Option C**: Use Azure CLI:

```bash
# Update specific property
az iot hub device-twin update \
  --device-id your-device-id \
  --hub-name your-iot-hub \
  --set properties.desired.webPort=9090
```

The service will automatically pick up changes within ~30 seconds.

## Troubleshooting

### Can't access from network
```bash
# Check if service is binding to correct interface
sudo netstat -tlnp | grep wellmonitor

# Should show: 0.0.0.0:8080 (not 127.0.0.1:8080)
```

### Port already in use
```bash
# Find what's using the port
sudo lsof -i :8080

# Kill the process if needed
sudo kill [pid]
```

### Authentication not working
```bash
# Check environment variable is set
echo $WEB_AUTH_PASSWORD

# Check service environment
sudo systemctl show wellmonitor | grep Environment
```

### Device twin not syncing
```bash
# Check device twin sync
./scripts/diagnostics/check-device-twin-sync.sh

# Force sync
./scripts/diagnostics/force-device-twin-update.ps1
```

## Security Best Practices

1. **Use authentication** when enabling network access
2. **Use specific IP binding** instead of 0.0.0.0 when possible
3. **Configure firewall** to limit access to trusted networks
4. **Use HTTPS** in production environments
5. **Set strong passwords** via environment variables (not device twin)
6. **Monitor access logs** for unauthorized attempts

## Next Steps

- [Full Configuration Guide](web-dashboard-network-configuration.md)
- [Security Best Practices](../deployment/troubleshooting-guide.md)
- [Azure IoT Integration](azure-integration.md)
