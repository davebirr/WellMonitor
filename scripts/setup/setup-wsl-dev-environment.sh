#!/bin/bash
#
# setup-wsl-dev-environment.sh
# Setup WSL development environment for WellMonitor Raspberry Pi development
#

echo "🐧 Setting up WSL Development Environment for WellMonitor"
echo "========================================================"

# Check if running in WSL
if ! grep -qi "microsoft\|wsl" /proc/version 2>/dev/null && [ ! -f /proc/sys/fs/binfmt_misc/WSLInterop ]; then
    echo "❌ This script must be run inside WSL"
    echo "💡 Install WSL first: wsl --install Ubuntu-22.04"
    echo "🔍 Debug info:"
    echo "   /proc/version: $(cat /proc/version 2>/dev/null || echo 'not found')"
    echo "   WSLInterop: $(ls -la /proc/sys/fs/binfmt_misc/WSLInterop 2>/dev/null || echo 'not found')"
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

# Setup GitHub authentication
echo "🔐 Setting up GitHub authentication..."
echo "Choose GitHub authentication method:"
echo "1. SSH Key (Recommended)"
echo "2. GitHub CLI with Device Flow"
echo "3. Skip (configure manually later)"
read -p "Enter choice [1-3]: " github_auth_choice

case $github_auth_choice in
    1)
        if [ ! -f ~/.ssh/id_ed25519 ]; then
            echo "🔑 Generating SSH key for GitHub..."
            read -p "Enter your email for SSH key: " ssh_email
            ssh-keygen -t ed25519 -C "$ssh_email" -f ~/.ssh/id_ed25519 -N ""
            
            # Start SSH agent and add key
            eval "$(ssh-agent -s)" > /dev/null 2>&1
            ssh-add ~/.ssh/id_ed25519 > /dev/null 2>&1
            
            echo "✅ SSH key generated"
            echo ""
            echo "📋 Your public key (add this to GitHub):"
            cat ~/.ssh/id_ed25519.pub
            echo ""
            echo "🌐 Add this key to GitHub:"
            echo "   1. Go to GitHub → Settings → SSH and GPG keys"
            echo "   2. Click 'New SSH key'"
            echo "   3. Paste the above public key"
            echo ""
            
            # Configure Git to use SSH
            git config --global url."git@github.com:".insteadOf "https://github.com/"
            echo "✅ Git configured to use SSH for GitHub"
        else
            echo "✅ SSH key already exists"
        fi
        ;;
    2)
        echo "📱 Installing GitHub CLI..."
        sudo apt update > /dev/null 2>&1
        sudo apt install -y gh > /dev/null 2>&1
        echo "✅ GitHub CLI installed"
        echo ""
        echo "🔐 Run 'gh auth login' after setup completes to authenticate"
        ;;
    3)
        echo "⏭️ Skipping GitHub authentication setup"
        ;;
    *)
        echo "⚠️ Invalid choice, skipping GitHub authentication setup"
        ;;
esac

# Check if SSH key exists for Pi access
if [ ! -f ~/.ssh/id_ed25519_pi ]; then
    echo "🔐 Generating additional SSH key for Pi access..."
    read -p "Enter your email for Pi SSH key: " ssh_email
    ssh-keygen -t ed25519 -C "$ssh_email" -f ~/.ssh/id_ed25519_pi -N ""
    echo "✅ Pi SSH key generated"
    echo ""
    echo "📋 Your Pi public key (copy this to your Pi):"
    cat ~/.ssh/id_ed25519_pi.pub
    echo ""
    echo "💡 Run this on your Pi to add the key:"
    echo "   echo '$(cat ~/.ssh/id_ed25519_pi.pub)' >> ~/.ssh/authorized_keys"
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
if [ "$github_auth_choice" = "1" ]; then
    echo "1. Add SSH key to GitHub (see public key above)"
    echo "2. Test GitHub access: ssh -T git@github.com"
    echo "3. Clone WellMonitor repository: git clone git@github.com:davebirr/WellMonitor.git"
elif [ "$github_auth_choice" = "2" ]; then
    echo "1. Authenticate with GitHub: gh auth login"
    echo "2. Clone WellMonitor repository: git clone https://github.com/davebirr/WellMonitor.git"
else
    echo "1. Set up GitHub authentication (see docs/development/development-setup.md)"
    echo "2. Clone WellMonitor repository: git clone https://github.com/davebirr/WellMonitor.git"
fi
echo "4. Configure Pi SSH access: ssh-copy-id -i ~/.ssh/id_ed25519_pi pi@raspberrypi.local"
echo "5. Test Pi connection: ssh -i ~/.ssh/id_ed25519_pi pi@raspberrypi.local"
echo "6. Run installation: cd WellMonitor && ./scripts/installation/install-wellmonitor.sh"
echo ""
echo "🔧 Useful WSL Commands:"
echo "   explorer.exe .                    # Open current directory in Windows Explorer"
echo "   code .                           # Open VS Code in current directory"
echo "   wsl --shutdown                   # Restart WSL if needed"
