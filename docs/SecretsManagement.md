# Secrets Management Deployment Guide

## Overview

The Well Monitor application supports multiple secure methods for managing secrets on your Raspberry Pi device:

1. **Azure Key Vault with Managed Identity** (Recommended for production)
2. **Environment Variables** (Good for production, simpler setup)
3. **Hybrid Approach** (Flexible, falls back gracefully)
4. **Local secrets.json** (Development only)

## 1. Azure Key Vault with Managed Identity (Recommended)

### Setup Steps:

1. **Create Azure Key Vault:**
   ```bash
   az keyvault create \
     --name "wellmonitor-kv-[unique-suffix]" \
     --resource-group "wellmonitor-rg" \
     --location "eastus" \
     --enabled-for-template-deployment true
   ```

2. **Create Managed Identity for your IoT device:**
   ```bash
   az identity create \
     --name "wellmonitor-device-identity" \
     --resource-group "wellmonitor-rg"
   ```

3. **Grant Key Vault access to the Managed Identity:**
   ```bash
   az keyvault set-policy \
     --name "wellmonitor-kv-[unique-suffix]" \
     --object-id [managed-identity-principal-id] \
     --secret-permissions get list
   ```

4. **Store secrets in Key Vault:**
   ```bash
   az keyvault secret set --vault-name "wellmonitor-kv-[unique-suffix]" --name "WellMonitor-IoTHub-ConnectionString" --value "HostName=YourIoTHub.azure-devices.net;DeviceId=YourDevice;SharedAccessKey=YourKey"
   az keyvault secret set --vault-name "wellmonitor-kv-[unique-suffix]" --name "WellMonitor-Storage-ConnectionString" --value "DefaultEndpointsProtocol=https;AccountName=..."
   az keyvault secret set --vault-name "wellmonitor-kv-[unique-suffix]" --name "WellMonitor-LocalEncryption-Key" --value "your-encryption-key"
   az keyvault secret set --vault-name "wellmonitor-kv-[unique-suffix]" --name "WellMonitor-PowerApp-ApiKey" --value "your-powerapp-key"
   az keyvault secret set --vault-name "wellmonitor-kv-[unique-suffix]" --name "WellMonitor-OCR-ApiKey" --value "your-ocr-key"
   ```

5. **Configure device environment variables:**
   ```bash
   # Set these on your Raspberry Pi
   export WELLMONITOR_SECRETS_MODE=keyvault
   export KeyVault__Uri=https://wellmonitor-kv-[unique-suffix].vault.azure.net/
   export AZURE_CLIENT_ID=[managed-identity-client-id]
   export AZURE_TENANT_ID=[your-tenant-id]
   ```

## 2. Environment Variables (Production Alternative)

### Setup Steps:

1. **Create environment variables file:**
   ```bash
   # Create /etc/wellmonitor/env
   sudo mkdir -p /etc/wellmonitor
   sudo tee /etc/wellmonitor/env << 'EOF'
   WELLMONITOR_SECRETS_MODE=environment
   WELLMONITOR_IOTHUB_CONNECTION_STRING=HostName=YourIoTHub.azure-devices.net;DeviceId=YourDevice;SharedAccessKey=YourKey
   WELLMONITOR_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=...
   WELLMONITOR_LOCAL_ENCRYPTION_KEY=your-encryption-key
   WELLMONITOR_POWERAPP_API_KEY=your-powerapp-key
   WELLMONITOR_OCR_API_KEY=your-ocr-key
   EOF
   ```

2. **Secure the environment file:**
   ```bash
   sudo chmod 600 /etc/wellmonitor/env
   sudo chown root:root /etc/wellmonitor/env
   ```

3. **Create systemd service file:**
   ```bash
   sudo tee /etc/systemd/system/wellmonitor.service << 'EOF'
   [Unit]
   Description=Well Monitor Device
   After=network.target
   
   [Service]
   Type=simple
   User=pi
   Group=pi
   WorkingDirectory=/home/pi/wellmonitor
   ExecStart=/usr/bin/dotnet /home/pi/wellmonitor/WellMonitor.Device.dll
   EnvironmentFile=/etc/wellmonitor/env
   Restart=always
   RestartSec=30
   
   [Install]
   WantedBy=multi-user.target
   EOF
   ```

4. **Enable and start the service:**
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable wellmonitor
   sudo systemctl start wellmonitor
   ```

## 3. Hybrid Approach (Default)

This is the most flexible approach that tries Key Vault first, then environment variables, then configuration files.

### Setup Steps:

1. **Set environment variable:**
   ```bash
   export WELLMONITOR_SECRETS_MODE=hybrid
   ```

2. **Configure Key Vault (optional):**
   ```bash
   export KeyVault__Uri=https://wellmonitor-kv-[unique-suffix].vault.azure.net/
   export AZURE_CLIENT_ID=[managed-identity-client-id]
   export AZURE_TENANT_ID=[your-tenant-id]
   ```

3. **Set environment variables as fallback:**
   ```bash
   export WELLMONITOR_IOTHUB_CONNECTION_STRING=HostName=YourIoTHub.azure-devices.net;DeviceId=YourDevice;SharedAccessKey=YourKey
   # ... other environment variables
   ```

4. **Keep secrets.json for local development:**
   The application will use secrets.json if the above methods fail.

## 4. Security Best Practices

### File Permissions:
```bash
# For any local secret files
sudo chmod 600 /path/to/secrets/file
sudo chown root:root /path/to/secrets/file
```

### Network Security:
- Use TLS 1.2+ for all connections
- Verify certificates
- Use private networks when possible

### Logging:
- Never log secret values
- Use structured logging
- Monitor for authentication failures

### Secret Rotation:
- Rotate secrets regularly (every 90 days)
- Use Azure Key Vault's automatic rotation features
- Monitor for expired secrets

## 5. Troubleshooting

### Check Secret Source:
```bash
# Check which secrets mode is active
sudo journalctl -u wellmonitor | grep "Secrets service configured"
```

### Test Key Vault Access:
```bash
# Test managed identity authentication
az account get-access-token --resource https://vault.azure.net
```

### Verify Environment Variables:
```bash
# Check if variables are set
env | grep WELLMONITOR_
```

### Debug Logs:
```bash
# Monitor application logs
sudo journalctl -u wellmonitor -f
```

## 6. Migration Path

### From secrets.json to Production:

1. **Phase 1: Add Environment Variables**
   - Set `WELLMONITOR_SECRETS_MODE=hybrid`
   - Add environment variables
   - Test that environment variables are used

2. **Phase 2: Add Key Vault (Optional)**
   - Create Key Vault and managed identity
   - Set up Key Vault secrets
   - Test that Key Vault is used

3. **Phase 3: Remove secrets.json**
   - Set `WELLMONITOR_SECRETS_MODE=environment` or `keyvault`
   - Remove or secure secrets.json file

## 7. Deployment Checklist

- [ ] Choose secrets management approach
- [ ] Set up Azure Key Vault (if using)
- [ ] Configure environment variables
- [ ] Set up systemd service
- [ ] Test secret retrieval
- [ ] Verify application startup
- [ ] Monitor for errors
- [ ] Set up secret rotation schedule
- [ ] Document the chosen approach
- [ ] Train team on troubleshooting

## 8. Emergency Recovery

If secrets become unavailable:

1. **Check service status:**
   ```bash
   sudo systemctl status wellmonitor
   ```

2. **Check logs:**
   ```bash
   sudo journalctl -u wellmonitor -n 50
   ```

3. **Fallback to environment variables:**
   ```bash
   export WELLMONITOR_SECRETS_MODE=environment
   # Set required environment variables
   sudo systemctl restart wellmonitor
   ```

4. **Temporary local override:**
   ```bash
   # Create temporary secrets.json
   echo '{"IotHubConnectionString": "your-connection-string"}' > /tmp/secrets.json
   # Update application config to use this file
   ```
