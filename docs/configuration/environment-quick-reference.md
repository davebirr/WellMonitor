# Environment Configuration Quick Reference

## WSL Prerequisites (One-time Setup)

```bash
# Install development tools
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl wget git unzip software-properties-common

# Install .NET 8 SDK
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb && rm packages-microsoft-prod.deb
sudo apt update && sudo apt install -y dotnet-sdk-8.0

# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Install GitHub CLI
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg && sudo chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null && sudo apt update && sudo apt install -y gh

# Authenticate
az login && gh auth login
```

## Setup for New Developers

```bash
# 1. Copy the template
cp .env.template .env

# 2. Edit with your actual values
nano .env  # or code .env

# 3. Load configuration
source .env

# 4. Test connectivity
ssh-pi  # Should connect to your Pi
```

## Configuration Sections

### 🔧 **Required for Production**
- `WELLMONITOR_IOTHUB_CONNECTION_STRING` - Azure IoT Hub connection
- `WELLMONITOR_DEVICE_ID` - Device identifier
- `WELLMONITOR_PI_HOSTNAME` - Your Raspberry Pi address
- `WELLMONITOR_PI_USERNAME` - SSH username for Pi

### 🔒 **Security (Optional but Recommended)**
- `WELLMONITOR_LOCAL_ENCRYPTION_KEY` - Local data encryption
- `WELLMONITOR_STORAGE_CONNECTION_STRING` - Azure Storage
- `WELLMONITOR_OCR_API_KEY` - Cognitive Services OCR
- `WELLMONITOR_POWERAPP_API_KEY` - PowerApp integration

### 🛠️ **Development (Optional)**
- `WELLMONITOR_DB_PATH` - Local database location
- `WELLMONITOR_DEBUG_IMAGES_PATH` - Debug image storage
- `WELLMONITOR_LOG_LEVEL` - Logging verbosity

## File Organization

| File | Purpose | Status | Contains |
|------|---------|--------|----------|
| `.env.template` | Template for developers | ✅ Committed | Generic examples and documentation |
| `.env` | Your personal configuration | 🚫 Git ignored | Your actual secrets and settings |
| `.env.local` | Alternative naming | 🚫 Git ignored | Same as `.env` |
| `.env.*.local` | Environment-specific | 🚫 Git ignored | Dev/staging/prod configs |

## Security Best Practices

### ✅ Safe Practices
- Use `.env.template` for documentation and examples
- Keep actual secrets in `.env` (git ignored)
- Use environment variables in scripts: `${WELLMONITOR_PI_HOSTNAME}`
- Generate strong encryption keys: `openssl rand -hex 16`

### ❌ Avoid These
- Committing `.env` files with real secrets
- Hardcoding credentials in scripts
- Using weak or default encryption keys
- Sharing connection strings in chat/email

## Quick Commands

```bash
# Load your configuration
source .env

# SSH to Pi
ssh-pi

# Monitor service logs
pi-logs

# Check service status
pi-status

# Restart service
pi-restart
```

## Troubleshooting

**Environment variables not working?**
```bash
# Check if loaded
echo $WELLMONITOR_PI_HOSTNAME

# Reload configuration
source .env

# Test SSH connectivity
ssh ${WELLMONITOR_PI_USERNAME}@${WELLMONITOR_PI_HOSTNAME} "echo 'Connected successfully'"
```

**Permission denied errors?**
```bash
# Check SSH key permissions
chmod 600 ~/.ssh/id_rsa

# Test SSH key
ssh-add -l
```
