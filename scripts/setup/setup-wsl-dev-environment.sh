#!/bin/bash
#
# setup-wsl-dev-environment.sh
# Setup WSL development environment for WellMonitor Raspberry Pi development
#

echo "🐧 Setting up WSL Development Environment for WellMonitor"
echo "========================================================"

# Check if running in WSL
if ! grep -q Microsoft /proc/version; then
    echo "❌ This script must be run inside WSL"
    echo "💡 Install WSL first: wsl --install Ubuntu-22.04"
    exit 1
fi

echo "✅ Running in WSL environment"

# Update system packages
echo "📦 Updating system packages..."
sudo apt update && sudo apt upgrade -y

# Install .NET 8 SDK if not present
if ! command -v dotnet &> /dev/null; then
    echo "📥 Installing .NET 8 SDK..."
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    sudo apt update
    sudo apt install -y dotnet-sdk-8.0
else
    echo "✅ .NET SDK already installed: $(dotnet --version)"
fi

# Install development tools
echo "🔧 Installing development tools..."
sudo apt install -y \
    git \
    openssh-client \
    build-essential \
    curl \
    wget \
    unzip \
    tree \
    jq

# Install PowerShell for cross-platform scripts
if ! command -v pwsh &> /dev/null; then
    echo "💻 Installing PowerShell..."
    wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    sudo apt update
    sudo apt install -y powershell
    rm packages-microsoft-prod.deb
else
    echo "✅ PowerShell already installed"
fi

# Configure Git if not already configured
if ! git config --global user.name > /dev/null 2>&1; then
    echo "🔧 Configuring Git..."
    read -p "Enter your Git username: " git_username
    read -p "Enter your Git email: " git_email
    git config --global user.name "$git_username"
    git config --global user.email "$git_email"
    echo "✅ Git configured"
else
    echo "✅ Git already configured for: $(git config --global user.name)"
fi

# Check if SSH key exists
if [ ! -f ~/.ssh/id_ed25519 ]; then
    echo "🔐 Generating SSH key for Pi access..."
    read -p "Enter your email for SSH key: " ssh_email
    ssh-keygen -t ed25519 -C "$ssh_email" -f ~/.ssh/id_ed25519 -N ""
    echo "✅ SSH key generated"
    echo ""
    echo "📋 Your public key (copy this to your Pi):"
    cat ~/.ssh/id_ed25519.pub
    echo ""
    echo "💡 Run this on your Pi to add the key:"
    echo "   echo '$(cat ~/.ssh/id_ed25519.pub)' >> ~/.ssh/authorized_keys"
else
    echo "✅ SSH key already exists"
fi

# Test .NET ARM64 build capability
echo "🧪 Testing .NET ARM64 build capability..."
if [ -d "WellMonitor" ]; then
    cd WellMonitor
    if dotnet build src/WellMonitor.Device/WellMonitor.Device.csproj -c Release -r linux-arm64 --verbosity quiet; then
        echo "✅ .NET ARM64 build test successful"
    else
        echo "⚠️ .NET ARM64 build test failed - check dependencies"
    fi
    cd ..
else
    echo "💡 Clone WellMonitor repository to test builds"
fi

echo ""
echo "🎉 WSL Development Environment Setup Complete!"
echo ""
echo "📋 Next Steps:"
echo "1. Clone WellMonitor repository: git clone https://github.com/davebirr/WellMonitor.git"
echo "2. Configure Pi SSH access: ssh-copy-id pi@raspberrypi.local"
echo "3. Test connection: ssh pi@raspberrypi.local"
echo "4. Run installation: cd WellMonitor && ./scripts/installation/install-wellmonitor.sh"
echo ""
echo "🔧 Useful WSL Commands:"
echo "   explorer.exe .                    # Open current directory in Windows Explorer"
echo "   code .                           # Open VS Code in current directory"
echo "   wsl --shutdown                   # Restart WSL if needed"
