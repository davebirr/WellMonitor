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
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
DEVICE_PROJECT="$PROJECT_ROOT/src/WellMonitor.Device"

echo -e "${BLUE}📍 Current status:${NC}"
echo "  Script Dir: $SCRIPT_DIR"
echo "  Project Root: $PROJECT_ROOT"
echo "  Device Project: $DEVICE_PROJECT"
echo "  Branch: $(git -C "$PROJECT_ROOT" branch --show-current 2>/dev/null || echo 'unknown')"
echo "  Location: $PROJECT_ROOT"
echo "  Last commit: $(git -C "$PROJECT_ROOT" log -1 --oneline 2>/dev/null || echo 'unknown')"
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
    git -C "$PROJECT_ROOT" fetch origin
    
    # Check if we're behind
    BEHIND=$(git -C "$PROJECT_ROOT" rev-list --count HEAD..origin/$(git -C "$PROJECT_ROOT" branch --show-current) 2>/dev/null || echo "0")
    if [ "$BEHIND" -gt 0 ]; then
        echo -e "${GREEN}🔄 Pulling $BEHIND new commit(s)...${NC}"
        git -C "$PROJECT_ROOT" pull
    else
        echo -e "${GREEN}✅ Already up to date${NC}"
    fi
    
    # Clean build if requested
    if [ "$CLEAN_BUILD" = true ]; then
        echo -e "${BLUE}🧹 Cleaning previous build...${NC}"
        dotnet clean "$PROJECT_ROOT/WellMonitor.sln"
        find "$PROJECT_ROOT" -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
        find "$PROJECT_ROOT" -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
    fi
    
    # Restore and build
    echo -e "${BLUE}📦 Restoring packages...${NC}"
    if ! dotnet restore "$PROJECT_ROOT/WellMonitor.sln"; then
        echo -e "${RED}❌ Package restore failed${NC}"
        exit 1
    fi
    
    echo -e "${BLUE}🔨 Building solution and publishing device project for linux-arm64...${NC}"
    # Build the entire solution first to ensure dependencies are built
    if ! dotnet build "$PROJECT_ROOT/WellMonitor.sln" -c Release; then
        echo -e "${RED}❌ Solution build failed${NC}"
        exit 1
    fi
    
    # Then publish the device project specifically for linux-arm64
    if ! dotnet publish "$DEVICE_PROJECT" -c Release -r linux-arm64 --self-contained true; then
        echo -e "${RED}❌ Device project publish failed${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Build and publish successful${NC}"
    
    # Run tests if they exist (only on compatible platforms)
    if [ -d "$PROJECT_ROOT/tests" ] && [ "$(find "$PROJECT_ROOT/tests" -name "*.csproj" | wc -l)" -gt 0 ]; then
        echo -e "${BLUE}🧪 Running tests...${NC}"
        # Check if we're on ARM64 and skip tests if they're not compatible
        if [[ "$(uname -m)" == "aarch64" ]]; then
            echo -e "${YELLOW}⏭️  Skipping tests on ARM64 platform (test runner compatibility)${NC}"
        else
            if dotnet test "$PROJECT_ROOT/WellMonitor.sln" --no-build --configuration Release --verbosity minimal; then
                echo -e "${GREEN}✅ Tests passed${NC}"
            else
                echo -e "${YELLOW}⚠️  Some tests failed${NC}"
            fi
        fi
    fi
else
    echo -e "${YELLOW}⏭️  Skipping build process (using existing binaries)${NC}"
fi

echo ""
echo -e "${BLUE}🏗️  Setting up secure system installation...${NC}"

# Check camera setup before proceeding
echo -e "${BLUE}📷 Checking camera setup...${NC}"
if ! ls /dev/video* >/dev/null 2>&1; then
    echo -e "${YELLOW}⚠️  No camera devices found. Please ensure:${NC}"
    echo "  1. Camera is properly connected"
    echo "  2. Camera is enabled in raspi-config"
    echo "  3. Reboot after enabling camera"
    echo ""
    echo -e "${BLUE}💡 To enable camera:${NC}"
    echo "  sudo raspi-config"
    echo "  → Interface Options → Camera → Enable"
    echo "  → Finish → Reboot"
    echo ""
fi

# Check if camera module is loaded
if ! lsmod | grep -q "bcm2835_v4l2\|bcm2835_isp"; then
    echo -e "${YELLOW}⚠️  Camera modules not loaded. This may cause camera errors.${NC}"
    echo -e "${BLUE}💡 You may need to add to /boot/config.txt:${NC}"
    echo "  camera_auto_detect=1"
    echo "  start_x=1"
    echo ""
fi

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
# Check if environment file already exists and has real values
if [ -f /etc/wellmonitor/environment ]; then
    if grep -q "REPLACE_WITH_YOUR_ACTUAL_CONNECTION_STRING\|YourIoTHub\.azure-devices\.net\|YourLocalEncryptionKey32Characters" /etc/wellmonitor/environment; then
        echo -e "${YELLOW}⚠️  Environment file exists but contains placeholder values${NC}"
        NEEDS_CONFIG=true
    else
        echo -e "${GREEN}✅ Environment file exists with configured values${NC}"
        NEEDS_CONFIG=false
    fi
else
    NEEDS_CONFIG=true
fi

if [ "$NEEDS_CONFIG" = true ]; then
    echo -e "${BLUE}📝 Creating environment file with placeholders...${NC}"
    sudo tee /etc/wellmonitor/environment > /dev/null << 'EOF'
ASPNETCORE_ENVIRONMENT=Production
WELLMONITOR_SECRETS_MODE=environment
DOTNET_EnableDiagnostics=0
# IMPORTANT: Update these values with your actual Azure IoT Hub credentials
# Format: HostName=your-hub.azure-devices.net;DeviceId=your-device-id;SharedAccessKey=your-device-key
WELLMONITOR_IOTHUB_CONNECTION_STRING=REPLACE_WITH_YOUR_ACTUAL_CONNECTION_STRING
WELLMONITOR_LOCAL_ENCRYPTION_KEY=REPLACE_WITH_32_CHARACTER_ENCRYPTION_KEY
EOF
fi

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
DeviceAllow=/dev/video* rw
DeviceAllow=/dev/dma_heap rw
DeviceAllow=/dev/dma_heap/* rw
DeviceAllow=/dev/dri rw
DeviceAllow=/dev/dri/* rw
SupplementaryGroups=gpio video render

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
echo -e "${YELLOW}🔧 IMPORTANT: Post-Installation Configuration Required${NC}"
echo "========================================"
if [ "$NEEDS_CONFIG" = true ]; then
    echo -e "${RED}🚨 CONFIGURATION REQUIRED: Your environment file has placeholder values!${NC}"
    echo ""
    echo -e "${RED}1. Update Azure IoT Hub Connection String:${NC}"
    echo "   sudo nano /etc/wellmonitor/environment"
    echo "   Replace: WELLMONITOR_IOTHUB_CONNECTION_STRING=..."
    echo "   With your actual Azure IoT Hub connection string"
    echo ""
    echo -e "${RED}2. Update Encryption Key:${NC}"
    echo "   Replace: WELLMONITOR_LOCAL_ENCRYPTION_KEY=..."
    echo "   With a 32-character encryption key"
    echo ""
    echo -e "${RED}3. Restart service after configuration:${NC}"
    echo "   sudo systemctl restart wellmonitor"
    echo ""
else
    echo -e "${GREEN}✅ Configuration appears to be set up${NC}"
    echo ""
fi
echo -e "${RED}3. Camera Setup (if camera errors persist):${NC}"
echo "   Run camera diagnostics: ./scripts/diagnostics/diagnose-camera.sh"
echo "   Or enable via: sudo raspi-config"
echo "   → Interface Options → Camera → Enable"
echo "   → Finish → Reboot"
echo ""
echo -e "${BLUE}📋 Service Management:${NC}"
echo "  Status:  sudo systemctl status wellmonitor"
echo "  Logs:    sudo journalctl -u wellmonitor -f"
echo "  Stop:    sudo systemctl stop wellmonitor"
echo "  Start:   sudo systemctl start wellmonitor"
echo "  Restart: sudo systemctl restart wellmonitor"
