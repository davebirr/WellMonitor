#!/bin/bash

echo "=== Complete Secure WellMonitor Installation ==="

# Parse arguments
CLEAN_BUILD=false
SKIP_BUILD=false

for arg in "$@"; do
    case $arg in
        --clean)
            CLEAN_BUILD=true
            shift
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [--clean] [--skip-build]"
            echo "  --clean      Clean build (removes bin/obj folders)"
            echo "  --skip-build Skip the build process (use existing binaries)"
            exit 0
            ;;
    esac
done

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DEVICE_PROJECT="$PROJECT_ROOT/src/WellMonitor.Device"

echo -e "${BLUE}📍 Current status:${NC}"
echo "  Branch: $(git branch --show-current)"
echo "  Location: $PROJECT_ROOT"
echo "  Last commit: $(git log -1 --oneline)"
echo ""

# Stop and disable existing service first
echo -e "${YELLOW}🛑 Stopping existing wellmonitor service...${NC}"
sudo systemctl stop wellmonitor 2>/dev/null && echo -e "${GREEN}✅ Service stopped${NC}" || echo -e "${BLUE}ℹ️  Service was not running${NC}"
sudo systemctl disable wellmonitor 2>/dev/null && echo -e "${GREEN}✅ Service disabled${NC}" || echo -e "${BLUE}ℹ️  Service was not enabled${NC}"

# Backup existing service file
if [ -f /etc/systemd/system/wellmonitor.service ]; then
    echo -e "${BLUE}💾 Backing up existing service file...${NC}"
    sudo cp /etc/systemd/system/wellmonitor.service /etc/systemd/system/wellmonitor.service.backup.$(date +%Y%m%d_%H%M%S)
    echo -e "${GREEN}✅ Backup saved${NC}"
fi

# Build process (unless skipped)
if [ "$SKIP_BUILD" = false ]; then
    cd "$PROJECT_ROOT"
    
    echo -e "${BLUE}📥 Fetching latest changes...${NC}"
    git fetch origin
    
    # Check if we're behind
    BEHIND=$(git rev-list --count HEAD..origin/$(git branch --show-current) 2>/dev/null || echo "0")
    if [ "$BEHIND" -gt 0 ]; then
        echo -e "${GREEN}🔄 Pulling $BEHIND new commit(s)...${NC}"
        git pull
    else
        echo -e "${GREEN}✅ Already up to date${NC}"
    fi
    
    # Clean build if requested
    if [ "$CLEAN_BUILD" = true ]; then
        echo -e "${BLUE}🧹 Cleaning previous build...${NC}"
        dotnet clean
        find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
        find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
    fi
    
    # Restore and build
    echo -e "${BLUE}📦 Restoring packages...${NC}"
    if ! dotnet restore; then
        echo -e "${RED}❌ Package restore failed${NC}"
        exit 1
    fi
    
    echo -e "${BLUE}🔨 Building project for linux-arm64...${NC}"
    if ! dotnet publish "$DEVICE_PROJECT" -c Release -r linux-arm64 --self-contained true; then
        echo -e "${RED}❌ Build failed${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Build successful${NC}"
    
    # Run tests if they exist
    if [ -d "tests" ] && [ "$(find tests -name "*.csproj" | wc -l)" -gt 0 ]; then
        echo -e "${BLUE}🧪 Running tests...${NC}"
        if dotnet test --no-build --configuration Release --verbosity minimal; then
            echo -e "${GREEN}✅ Tests passed${NC}"
        else
            echo -e "${YELLOW}⚠️  Some tests failed${NC}"
        fi
    fi
else
    echo -e "${YELLOW}⏭️  Skipping build process (using existing binaries)${NC}"
fi

echo ""
echo -e "${BLUE}🏗️  Setting up secure system installation...${NC}"

# Create system directories
sudo mkdir -p /opt/wellmonitor
sudo mkdir -p /var/lib/wellmonitor
sudo mkdir -p /var/log/wellmonitor
sudo mkdir -p /etc/wellmonitor

echo -e "${GREEN}✅ Created system directories${NC}"

# Copy application files from publish directory
echo -e "${BLUE}📁 Installing application files...${NC}"
if [ -d "$DEVICE_PROJECT/bin/Release/net8.0/linux-arm64/publish" ]; then
    sudo cp -r "$DEVICE_PROJECT/bin/Release/net8.0/linux-arm64/publish/"* /opt/wellmonitor/
    echo -e "${GREEN}✅ Used publish directory${NC}"
elif [ -d "$DEVICE_PROJECT/bin/Release/net8.0/linux-arm64" ]; then
    sudo cp -r "$DEVICE_PROJECT/bin/Release/net8.0/linux-arm64/"* /opt/wellmonitor/
    echo -e "${GREEN}✅ Used linux-arm64 directory${NC}"
else
    echo -e "${RED}❌ No built application found. Run with --clean to rebuild.${NC}"
    exit 1
fi

sudo chown -R root:root /opt/wellmonitor
sudo chmod +x /opt/wellmonitor/WellMonitor.Device

# Set up data directory with proper ownership
sudo chown -R davidb:davidb /var/lib/wellmonitor
sudo chmod 755 /var/lib/wellmonitor

# Copy database if it exists
if [ -f "$DEVICE_PROJECT/wellmonitor.db" ]; then
    echo -e "${BLUE}💾 Migrating existing database...${NC}"
    sudo cp "$DEVICE_PROJECT/wellmonitor.db"* /var/lib/wellmonitor/ 2>/dev/null || true
    sudo chown davidb:davidb /var/lib/wellmonitor/wellmonitor.db*
    echo -e "${GREEN}✅ Database migrated${NC}"
fi

# Create debug images directory
sudo mkdir -p /var/lib/wellmonitor/debug_images
sudo chown -R davidb:davidb /var/lib/wellmonitor/debug_images

# Copy existing debug images if they exist
if [ -d "$DEVICE_PROJECT/debug_images" ]; then
    echo -e "${BLUE}🖼️  Migrating debug images...${NC}"
    sudo cp -r "$DEVICE_PROJECT/debug_images/"* /var/lib/wellmonitor/debug_images/ 2>/dev/null || true
    sudo chown -R davidb:davidb /var/lib/wellmonitor/debug_images
    echo -e "${GREEN}✅ Debug images migrated${NC}"
fi

# Create environment file (more secure than inline environment variables)
sudo tee /etc/wellmonitor/environment > /dev/null << 'EOF'
ASPNETCORE_ENVIRONMENT=Production
WELLMONITOR_SECRETS_MODE=environment
DOTNET_EnableDiagnostics=0
WELLMONITOR_IOTHUB_CONNECTION_STRING=HostName=YourIoTHub.azure-devices.net;DeviceId=YourDeviceId;SharedAccessKey=YourDeviceKey
WELLMONITOR_LOCAL_ENCRYPTION_KEY=YourLocalEncryptionKey32Characters
EOF

# Secure the environment file
sudo chown root:davidb /etc/wellmonitor/environment
sudo chmod 640 /etc/wellmonitor/environment

echo -e "${GREEN}✅ Created secure environment file${NC}"

# Create the secure service file
sudo tee /etc/systemd/system/wellmonitor.service > /dev/null << 'EOF'
[Unit]
Description=WellMonitor Device Service
After=network.target
Wants=network.target

[Service]
Type=exec
User=davidb
Group=davidb
WorkingDirectory=/var/lib/wellmonitor
ExecStart=/opt/wellmonitor/WellMonitor.Device
Restart=always
RestartSec=10

# Load environment from secure file
EnvironmentFile=/etc/wellmonitor/environment

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=wellmonitor

# Security - FULL PROTECTION ENABLED
NoNewPrivileges=yes
PrivateTmp=yes
ProtectHome=yes         # ✅ ENABLED - No access to /home
ProtectSystem=strict    # ✅ ENABLED - Strong system protection
ProtectKernelTunables=yes
ProtectKernelModules=yes
ProtectControlGroups=yes
RestrictRealtime=yes
SystemCallArchitectures=native

# Allow access only to specific directories
ReadWritePaths=/var/lib/wellmonitor
ReadWritePaths=/var/log/wellmonitor
ReadOnlyPaths=/etc/wellmonitor

# Device access for GPIO and camera
DeviceAllow=/dev/gpiochip0 rw
DeviceAllow=/dev/video0 rw
DeviceAllow=/dev/video1 rw
SupplementaryGroups=gpio video

[Install]
WantedBy=multi-user.target
EOF

echo -e "${GREEN}✅ Created secure service file with full security protections${NC}"

# Update appsettings.json to use new paths
sudo tee /opt/wellmonitor/appsettings.json > /dev/null << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/var/lib/wellmonitor/wellmonitor.db"
  },
  "Camera": {
    "debugImagePath": "/var/lib/wellmonitor/debug_images"
  }
}
EOF

echo -e "${GREEN}✅ Updated configuration with system paths${NC}"

# Reload systemd and enable service
sudo systemctl daemon-reload
sudo systemctl enable wellmonitor

echo ""
echo -e "${BLUE}🚀 Starting wellmonitor service...${NC}"

# Start the service
sudo systemctl start wellmonitor

# Wait for startup
sleep 5

# Check status
echo ""
echo -e "${BLUE}📊 Service status:${NC}"
sudo systemctl status wellmonitor --no-pager -l

echo ""
echo -e "${BLUE}📝 Recent logs:${NC}"
sudo journalctl -u wellmonitor -n 15 --no-pager

echo ""
echo -e "${GREEN}🎉 Secure Installation Complete!${NC}"
echo "========================================"
echo -e "${GREEN}✅ Application installed to:     /opt/wellmonitor/${NC}"
echo -e "${GREEN}✅ Data directory:              /var/lib/wellmonitor/${NC}"
echo -e "${GREEN}✅ Configuration:               /etc/wellmonitor/${NC}"
echo -e "${GREEN}✅ Database location:           /var/lib/wellmonitor/wellmonitor.db${NC}"
echo -e "${GREEN}✅ Debug images:                /var/lib/wellmonitor/debug_images/${NC}"
echo -e "${GREEN}✅ Security: ProtectHome=yes    (Enabled)${NC}"
echo -e "${GREEN}✅ Security: ProtectSystem=strict (Enabled)${NC}"
echo ""
echo -e "${BLUE}📋 Service Management:${NC}"
echo "  Status:  sudo systemctl status wellmonitor"
echo "  Logs:    sudo journalctl -u wellmonitor -f"
echo "  Stop:    sudo systemctl stop wellmonitor"
echo "  Start:   sudo systemctl start wellmonitor"
echo "  Restart: sudo systemctl restart wellmonitor"
