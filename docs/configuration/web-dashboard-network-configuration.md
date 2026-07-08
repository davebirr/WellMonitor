# Web Dashboard Network Configuration

This guide explains how to configure the WellMonitor web dashboard for network accessibility through Azure IoT device twin properties.

## Overview

The WellMonitor web dashboard can be configured to:
- Run on custom ports
- Allow network access from other devices
- Bind to specific network interfaces
- Enable HTTPS
- Configure CORS origins
- Enable basic authentication

All these settings can be configured remotely via Azure IoT device twin properties.

## Device Twin Configuration

### Using PowerShell Script

Use the `scripts/configuration/Update-ExtendedDeviceTwin.ps1` script to configure web dashboard settings:

```powershell
# Run the script to update device twin with web configuration
./scripts/configuration/Update-ExtendedDeviceTwin.ps1
```

The script includes these web dashboard settings:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `webPort` | int | 5000 | HTTP port for the web dashboard |
| `webAllowNetworkAccess` | bool | false | Allow access from network (not just localhost) |
| `webBindAddress` | string | "127.0.0.1" | IP address to bind to ("0.0.0.0" for all interfaces) |
| `webEnableHttps` | bool | false | Enable HTTPS (requires certificate) |
| `webHttpsPort` | int | 5001 | HTTPS port |
| `webCorsOrigins` | string | "" | Comma-separated CORS origins |
| `webEnableAuthentication` | bool | false | Enable basic authentication |
| `webAuthUsername` | string | "admin" | Authentication username |

### Manual Device Twin Update

You can also manually update the device twin properties in Azure IoT Hub:

```json
{
  "properties": {
    "desired": {
      "webPort": 8080,
      "webAllowNetworkAccess": true,
      "webBindAddress": "0.0.0.0",
      "webEnableHttps": false,
      "webHttpsPort": 8443,
      "webCorsOrigins": "http://192.168.1.100:3000,http://monitoring.local",
      "webEnableAuthentication": true,
      "webAuthUsername": "wellmonitor"
    }
  }
}
```

## Configuration Examples

### 1. Local Access Only (Default)
```json
{
  "webPort": 5000,
  "webAllowNetworkAccess": false,
  "webBindAddress": "127.0.0.1"
}
```
- Dashboard accessible only from the device itself
- Access via: `http://localhost:5000`

### 2. Network Access on Custom Port
```json
{
  "webPort": 8080,
  "webAllowNetworkAccess": true,
  "webBindAddress": "0.0.0.0"
}
```
- Dashboard accessible from any device on the network
- Access via: `http://[device-ip]:8080`

### 3. Secure Network Access
```json
{
  "webPort": 8080,
  "webAllowNetworkAccess": true,
  "webBindAddress": "0.0.0.0",
  "webEnableAuthentication": true,
  "webAuthUsername": "wellmonitor"
}
```
- Network access with basic authentication
- Requires username/password to access dashboard

### 4. HTTPS Configuration
```json
{
  "webPort": 8080,
  "webHttpsPort": 8443,
  "webEnableHttps": true,
  "webAllowNetworkAccess": true,
  "webBindAddress": "0.0.0.0"
}
```
- Enables both HTTP and HTTPS
- Requires SSL certificate configuration

## Security Considerations

### Network Access
- **Default**: `webAllowNetworkAccess` is `false` for security
- **Network Access**: Set to `true` only when needed
- **Bind Address**: Use specific IP instead of "0.0.0.0" when possible

### Authentication
- **Password**: Set via environment variable `WEB_AUTH_PASSWORD` (not in device twin)
- **Username**: Configurable via device twin
- **HTTPS**: Recommended when enabling network access

### Firewall Configuration
When enabling network access, ensure proper firewall rules:
```bash
# Allow specific port (replace 8080 with your port)
sudo ufw allow 8080

# Or allow from specific network only
sudo ufw allow from 192.168.1.0/24 to any port 8080
```

## Environment Variables

Some sensitive settings should be configured via environment variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `WEB_AUTH_PASSWORD` | Basic auth password | `mySecurePassword123` |
| `WEB_SSL_CERT_PATH` | SSL certificate path | `/etc/ssl/certs/wellmonitor.crt` |
| `WEB_SSL_KEY_PATH` | SSL private key path | `/etc/ssl/private/wellmonitor.key` |

Set environment variables in `/etc/environment` or systemd service file:
```bash
# Add to /etc/environment
WEB_AUTH_PASSWORD=mySecurePassword123
```

## Live Configuration Updates

The web dashboard supports live configuration updates without restart:
1. Update device twin properties
2. Configuration automatically applies within ~30 seconds
3. Web server restarts on new port/binding if needed

## Troubleshooting

### Port Already in Use
```bash
# Check what's using the port
sudo netstat -tlnp | grep :8080

# Kill process if needed
sudo kill <pid>
```

### Network Access Issues
```bash
# Check if service is listening on correct interface
sudo netstat -tlnp | grep wellmonitor

# Test network connectivity
curl http://[device-ip]:[port]/health
```

### Device Twin Sync Issues
```bash
# Check device twin sync status
./scripts/diagnostics/check-device-twin-sync.sh

# Force device twin update
./scripts/diagnostics/force-device-twin-update.ps1
```

### Log Analysis
Check logs for web configuration issues:
```bash
# View service logs
journalctl -u wellmonitor -f

# Look for web configuration messages
journalctl -u wellmonitor | grep "Web configuration"
```

## Integration with Existing Configuration

The web dashboard configuration integrates seamlessly with existing WellMonitor configuration:
- OCR settings remain unchanged
- Debug settings work alongside web settings
- Monitoring intervals continue to apply
- All existing device twin properties are preserved

## Related Documentation

- [Configuration Guide](configuration-guide.md) - General configuration
- [Azure Integration](azure-integration.md) - Azure IoT setup
- [Service Management](../deployment/service-management.md) - Managing the service
- [Troubleshooting Guide](../deployment/troubleshooting-guide.md) - Common issues
