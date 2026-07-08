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

**Prerequisites Installation:**

First, install required tools in your WSL environment:

```bash
# Update package lists
sudo apt update && sudo apt upgrade -y

# Install basic development tools
sudo apt install -y curl wget git unzip software-properties-common apt-transport-https lsb-release gnupg

# Install .NET 8 SDK
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Verify .NET installation
dotnet --version

# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Verify Azure CLI installation
az --version

# Install GitHub CLI
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
&& sudo chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg \
&& echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
&& sudo apt update \
&& sudo apt install -y gh

# Verify GitHub CLI installation
gh --version

# Install PowerShell (optional - for running PowerShell scripts)
# Method 1: Using snap (most reliable)
sudo snap install powershell --classic

# Method 2: If snap is not available, download and install manually
# Get the latest release info and download the correct file
ARCH=$(dpkg --print-architecture)
echo "Detected architecture: $ARCH"

if [ "$ARCH" = "amd64" ]; then
    echo "Downloading PowerShell for amd64..."
    
    # Debug: Let's see what we get from each step
    echo "Step 1: Getting release info..."
    RELEASE_DATA=$(curl -s https://api.github.com/repos/PowerShell/PowerShell/releases/latest)
    echo "Release data retrieved (first 200 chars): ${RELEASE_DATA:0:200}"
    
    echo "Step 2: Looking for browser_download_url lines..."
    DOWNLOAD_LINES=$(echo "$RELEASE_DATA" | grep "browser_download_url")
    echo "Found $(echo "$DOWNLOAD_LINES" | wc -l) download URLs"
    
    echo "Step 3: Looking for amd64.deb files..."
    DEB_LINES=$(echo "$DOWNLOAD_LINES" | grep "amd64\.deb")
    echo "Found amd64.deb lines:"
    echo "$DEB_LINES"
    
    echo "Step 4: Getting first match and extracting URL..."
    URL=$(echo "$DEB_LINES" | head -1 | cut -d '"' -f 4)
    echo "Extracted URL: '$URL'"
    
    if [ -n "$URL" ]; then
        echo "Step 5: Downloading file..."
        wget -O powershell.deb "$URL"
        sudo dpkg -i powershell.deb
        sudo apt-get install -f  # Fix any dependency issues
        rm powershell.deb
    else
        echo "ERROR: Could not extract download URL"
        echo "Falling back to snap installation..."
        sudo snap install powershell --classic
    fi
    
elif [ "$ARCH" = "arm64" ]; then
    echo "Downloading PowerShell for arm64..."
    echo "Note: PowerShell .deb packages are not available for ARM64 architecture"
    echo "Using snap installation with edge channel..."
    
    # For ARM64, PowerShell is only available on edge channel
    sudo snap install --edge powershell --classic
    
else
    echo "Unsupported architecture: $ARCH"
    echo "Supported architectures: amd64, arm64"
    echo "Attempting snap installation..."
    
    # Try edge channel for unsupported architectures
    if sudo snap install --edge powershell --classic; then
        echo "Successfully installed PowerShell via snap edge channel"
    else
        echo "ERROR: Could not install PowerShell for architecture $ARCH"
        echo "You may need to install PowerShell manually from:"
        echo "https://github.com/PowerShell/PowerShell/releases"
        exit 1
    fi
fi

# Verify PowerShell installation
pwsh --version
```

**Authenticate with Azure and GitHub:**
```bash
# Authenticate with Azure CLI
az login
# Follow the browser authentication flow

# Set your default subscription using the project's subscription ID
az account set --subscription "${AZURE_SUBSCRIPTION_ID}"
# Alternative if environment variable not set:
# az account set --subscription "7e77b014-138f-4858-952b-76e75f0bbeb7"

# Verify Azure authentication and subscription
az account show

# Authenticate with GitHub CLI
gh auth login
# Select:
# - GitHub.com
# - HTTPS or SSH (depending on your preference)
# - Yes (authenticate Git with GitHub credentials)
# - Login with a web browser or paste token

# Verify GitHub authentication
gh auth status
```

**Configure Git (if not already done):**
```bash
# Set your Git identity
git config --global user.name "Your Name"
git config --global user.email "your-email@example.com"

# Configure Git to use VS Code as editor (optional)
git config --global core.editor "code --wait"
```

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

**Verify Installation:**
```bash
# Test all tools are working
echo "=== Checking Prerequisites ==="
echo "✅ .NET SDK: $(dotnet --version)"
echo "✅ Azure CLI: $(az --version | head -n1)"
echo "✅ GitHub CLI: $(gh --version | head -n1)"
echo "✅ PowerShell: $(pwsh --version)"
echo "✅ Git: $(git --version)"

# Test Azure authentication
echo "=== Azure Status ==="
az account show --output table

# Test GitHub authentication  
echo "=== GitHub Status ==="
gh auth status

# Test environment variables
echo "=== Environment Variables ==="
echo "Pi Hostname: ${WELLMONITOR_PI_HOSTNAME}"
echo "Pi Username: ${WELLMONITOR_PI_USERNAME}"
echo "SSH Key Path: ${WELLMONITOR_SSH_KEY_PATH}"

# Test SSH connectivity (if Pi is accessible)
echo "=== Testing SSH Connection ==="
ssh ${WELLMONITOR_PI_USERNAME}@${WELLMONITOR_PI_HOSTNAME} "echo 'SSH connection successful!'"
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

### WSL Prerequisites Issues

**If .NET installation fails:**
```bash
# Remove and retry
sudo apt remove dotnet-sdk-8.0
sudo apt autoremove
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Alternative: Use Microsoft's install script
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
export PATH="$HOME/.dotnet:$PATH"
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
```

**If Azure CLI installation fails:**
```bash
# Alternative installation method
pip3 install azure-cli

# Or using snap
sudo snap install azure-cli --classic
```

**If GitHub CLI installation fails:**
```bash
# Alternative installation using snap
sudo snap install gh

# Or download and install manually
wget https://github.com/cli/cli/releases/latest/download/gh_*_linux_amd64.deb
sudo dpkg -i gh_*_linux_amd64.deb
```

**Network/DNS issues in WSL:**
```bash
# Reset DNS configuration
sudo rm /etc/resolv.conf
sudo bash -c 'echo "nameserver 8.8.8.8" > /etc/resolv.conf'
sudo bash -c 'echo "nameserver 8.8.4.4" >> /etc/resolv.conf'

# Or use systemd-resolved
sudo systemctl restart systemd-resolved
```

**PowerShell not found:**
```bash
# Check if PowerShell is installed
which pwsh

# Method 1: Use snap (most reliable - bypasses repository issues)
sudo snap install powershell --classic

# For ARM64 systems (Windows on ARM), use edge channel:
sudo snap install --edge powershell --classic

# Method 2: Download latest release automatically (if snap not available)
ARCH=$(dpkg --print-architecture)
echo "Detected architecture: $ARCH"

if [ "$ARCH" = "amd64" ]; then
    echo "Downloading PowerShell for amd64..."
    RELEASE_DATA=$(curl -s https://api.github.com/repos/PowerShell/PowerShell/releases/latest)
    DEB_LINES=$(echo "$RELEASE_DATA" | grep "browser_download_url" | grep "amd64\.deb")
    URL=$(echo "$DEB_LINES" | head -1 | cut -d '"' -f 4)
    
    if [ -n "$URL" ]; then
        wget -O powershell.deb "$URL"
        sudo dpkg -i powershell.deb
        sudo apt-get install -f
        rm powershell.deb
    else
        echo "Could not extract URL, using snap instead"
        sudo snap install powershell --classic
    fi
elif [ "$ARCH" = "arm64" ]; then
    echo "ARM64 architecture detected - .deb packages not available"
    echo "Using snap edge channel for ARM64..."
    sudo snap install --edge powershell --classic
else
    echo "Unsupported architecture, trying snap edge channel..."
    sudo snap install --edge powershell --classic
fi

# Method 3: Use Microsoft's install script
curl -L https://aka.ms/install-powershell.sh | sudo bash

# Architecture-specific notes:
# - AMD64/x86_64: Full support with .deb packages and stable snap channel
# - ARM64: Limited support, requires snap edge channel (no .deb packages available)
# - Other architectures: May require manual installation from GitHub releases

# Windows on ARM detection:
# If you're running WSL on a Windows on ARM device (Surface Pro X, etc.):
# - dpkg --print-architecture will return "arm64"  
# - PowerShell .deb packages are not available for ARM64
# - Use: sudo snap install --edge powershell --classic
# - Edge channel may have pre-release features but is the only option for ARM64

# Common issues:
# 1. "snap not available on stable": Use --edge flag for ARM64 systems
# 2. "wget missing URL": Likely ARM64 system where .deb packages don't exist
# 3. Architecture mismatch: Verify with uname -m and dpkg --print-architecture
```

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
