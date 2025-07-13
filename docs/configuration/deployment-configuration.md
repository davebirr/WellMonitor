# Deployment Configuration Guide

This guide explains how to configure your specific deployment details for the WellMonitor application.

## Overview

The WellMonitor documentation uses environment variables to keep deployment-specific details (like hostnames, usernames, etc.) separate from the generic documentation. This approach:

- **Keeps documentation generic** and reusable for multiple deployments
- **Protects sensitive information** from being committed to source control
- **Makes documentation maintenance easier** by avoiding hardcoded values
- **Follows security best practices** for configuration management

## Environment Variable Configuration

### Required Variables

Set these environment variables for your specific deployment:

```bash
# Raspberry Pi connection details
export WELLMONITOR_PI_HOSTNAME="your-pi-hostname.local"
export WELLMONITOR_PI_USERNAME="your-pi-username"
export WELLMONITOR_SSH_KEY_PATH="~/.ssh/id_rsa"

# Optional: Azure IoT Hub details for local development
export WELLMONITOR_IOTHUB_CONNECTION_STRING="your-connection-string"
export WELLMONITOR_DEVICE_ID="your-device-id"
```

### Example Values

**Common Raspberry Pi Configurations:**
```bash
# Default Raspberry Pi OS setup
export WELLMONITOR_PI_HOSTNAME="raspberrypi.local"
export WELLMONITOR_PI_USERNAME="pi"

# Custom hostname setup
export WELLMONITOR_PI_HOSTNAME="wellmonitor-001.local"
export WELLMONITOR_PI_USERNAME="wellmonitor"

# Static IP setup
export WELLMONITOR_PI_HOSTNAME="192.168.1.100"
export WELLMONITOR_PI_USERNAME="pi"
```

## Configuration Methods

### Method 1: WSL/Linux Environment (Recommended)

**Add to your shell profile:**
```bash
# Edit your profile file
nano ~/.bashrc  # or ~/.profile, ~/.zshrc

# Add at the end:
# WellMonitor deployment configuration
export WELLMONITOR_PI_HOSTNAME="your-pi-hostname.local"
export WELLMONITOR_PI_USERNAME="your-pi-username"
export WELLMONITOR_SSH_KEY_PATH="~/.ssh/id_rsa"

# Convenient aliases
alias ssh-pi='ssh ${WELLMONITOR_PI_USERNAME}@${WELLMONITOR_PI_HOSTNAME}'
alias pi-logs='ssh ${WELLMONITOR_PI_USERNAME}@${WELLMONITOR_PI_HOSTNAME} "sudo journalctl -u wellmonitor -f"'
alias pi-status='ssh ${WELLMONITOR_PI_USERNAME}@${WELLMONITOR_PI_HOSTNAME} "sudo systemctl status wellmonitor"'

# Reload profile
source ~/.bashrc
```

### Method 2: Windows PowerShell

**For current session:**
```powershell
$env:WELLMONITOR_PI_HOSTNAME = "your-pi-hostname.local"
$env:WELLMONITOR_PI_USERNAME = "your-pi-username"
$env:WELLMONITOR_SSH_KEY_PATH = "~/.ssh/id_rsa"
```

**For persistent configuration:**
```powershell
# Add to PowerShell profile
code $PROFILE

# Add these lines:
$env:WELLMONITOR_PI_HOSTNAME = "your-pi-hostname.local"
$env:WELLMONITOR_PI_USERNAME = "your-pi-username"
$env:WELLMONITOR_SSH_KEY_PATH = "~/.ssh/id_rsa"

# Create convenient functions
function ssh-pi { ssh "${env:WELLMONITOR_PI_USERNAME}@${env:WELLMONITOR_PI_HOSTNAME}" }
function pi-logs { ssh "${env:WELLMONITOR_PI_USERNAME}@${env:WELLMONITOR_PI_HOSTNAME}" "sudo journalctl -u wellmonitor -f" }
```

### Method 3: VS Code Settings

**For development in VS Code:**
```json
// In .vscode/settings.json (project-specific, not committed)
{
  "terminal.integrated.env.linux": {
    "WELLMONITOR_PI_HOSTNAME": "your-pi-hostname.local",
    "WELLMONITOR_PI_USERNAME": "your-pi-username",
    "WELLMONITOR_SSH_KEY_PATH": "~/.ssh/id_rsa"
  },
  "terminal.integrated.env.windows": {
    "WELLMONITOR_PI_HOSTNAME": "your-pi-hostname.local",
    "WELLMONITOR_PI_USERNAME": "your-pi-username",
    "WELLMONITOR_SSH_KEY_PATH": "~/.ssh/id_rsa"
  }
}
```

## Deployment Scripts Configuration

### Update Installation Scripts

Some deployment scripts may need to be updated to use environment variables:

**Example script update:**
```bash
# Old hardcoded approach
PI_HOST="pi@raspberrypi.local"

# New environment variable approach
PI_HOST="${WELLMONITOR_PI_USERNAME}@${WELLMONITOR_PI_HOSTNAME}"
```

### SSH Configuration

**Create SSH config for easier access:**
```bash
# Edit SSH config
nano ~/.ssh/config

# Add configuration (replace with your values):
Host pi
    HostName your-pi-hostname.local
    User your-pi-username
    IdentityFile ~/.ssh/id_rsa
    IdentitiesOnly yes
    ServerAliveInterval 60
    ServerAliveCountMax 3

# Alternative short alias
Host well
    HostName your-pi-hostname.local
    User your-pi-username
    IdentityFile ~/.ssh/id_rsa
    IdentitiesOnly yes
```

## Security Considerations

### What NOT to Commit

**Never commit these to source control:**
- Specific hostnames or IP addresses
- Usernames 
- SSH private keys
- Connection strings
- API keys
- Device IDs

### Safe Configuration Practices

**✅ Good practices:**
- Use environment variables for deployment-specific details
- Use generic examples in documentation
- Keep sensitive values in `.env` files (not committed)
- Use SSH config files for connection details
- Document the configuration pattern, not the actual values

**❌ Avoid:**
- Hardcoding hostnames in documentation
- Committing usernames to source control
- Including connection strings in example files
- Using real device IDs in samples

## Troubleshooting

### Environment Variables Not Working

**Check if variables are set:**
```bash
# Linux/WSL
echo $WELLMONITOR_PI_HOSTNAME
env | grep WELLMONITOR

# Windows PowerShell
echo $env:WELLMONITOR_PI_HOSTNAME
Get-ChildItem Env: | Where-Object Name -like "*WELLMONITOR*"
```

**Common issues:**
1. **Variables not exported**: Use `export` in bash/zsh
2. **Wrong shell**: Make sure you're in the correct shell (bash vs zsh vs PowerShell)
3. **Profile not loaded**: Restart terminal or `source ~/.bashrc`
4. **Typos in variable names**: Double-check exact spelling

### SSH Connection Issues

**Test connection:**
```bash
# Test basic SSH
ssh ${WELLMONITOR_PI_USERNAME}@${WELLMONITOR_PI_HOSTNAME} "echo 'Success'"

# Debug SSH connection
ssh -v ${WELLMONITOR_PI_USERNAME}@${WELLMONITOR_PI_HOSTNAME}
```

**Common solutions:**
1. **Check Pi is accessible**: `ping ${WELLMONITOR_PI_HOSTNAME}`
2. **Verify SSH service**: Pi must have SSH enabled
3. **Check SSH keys**: Use `ssh-copy-id` to setup key authentication
4. **Firewall issues**: Ensure port 22 is open

## Team Configuration

### For Development Teams

**Share configuration template (not actual values):**
```bash
# Create a template file: deployment-config.template
# Team members copy this and fill in their values

# Template file (committed to repo):
export WELLMONITOR_PI_HOSTNAME="your-pi-hostname.local"
export WELLMONITOR_PI_USERNAME="your-pi-username"
export WELLMONITOR_SSH_KEY_PATH="~/.ssh/id_rsa"

# Each developer creates their own:
cp deployment-config.template deployment-config.sh
# Edit deployment-config.sh with actual values
# Add deployment-config.sh to .gitignore
```

### Documentation Guidelines

**When writing documentation:**
- Use `${WELLMONITOR_PI_HOSTNAME}` instead of actual hostnames
- Use `${WELLMONITOR_PI_USERNAME}` instead of actual usernames  
- Provide generic examples like `your-pi-hostname.local`
- Reference this configuration guide for setup

**When providing examples:**
```bash
# ✅ Good - uses environment variables
ssh ${WELLMONITOR_PI_USERNAME}@${WELLMONITOR_PI_HOSTNAME}

# ❌ Avoid - hardcoded values
ssh pi@raspberrypi.local
```

This approach makes documentation reusable while keeping deployment details secure and configurable.
