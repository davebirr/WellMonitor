# WellMonitor Systemd Service Setup Script
# Run this script on your Raspberry Pi to set up WellMonitor as a system service

#!/bin/bash

set -e  # Exit on any error

echo "🔧 Setting up WellMonitor as a systemd service..."
echo "=================================================="

# Check if running as root
if [[ $EUID -eq 0 ]]; then
   echo "❌ This script should NOT be run as root/sudo"
   echo "   Run as your regular user. The script will prompt for sudo when needed."
   exit 1
fi

# Variables
SERVICE_NAME="wellmonitor"
USER_NAME=$(whoami)
WORK_DIR="/home/$USER_NAME/WellMonitor/src/WellMonitor.Device"
EXEC_PATH="/home/$USER_NAME/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/WellMonitor.Device"

echo "User: $USER_NAME"
echo "Working Directory: $WORK_DIR"
echo "Executable: $EXEC_PATH"
echo

# Check if the project directory exists
if [ ! -d "$WORK_DIR" ]; then
    echo "❌ WellMonitor project directory not found: $WORK_DIR"
    echo "   Please ensure you've cloned the repository to /home/$USER_NAME/WellMonitor"
    exit 1
fi

# Build the project first
echo "🔨 Building WellMonitor project..."
cd "$WORK_DIR"
dotnet publish -c Release -r linux-arm64 --self-contained false

# Check if the executable exists after build
if [ ! -f "$EXEC_PATH" ]; then
    echo "❌ Executable not found after build: $EXEC_PATH"
    echo "   Build may have failed. Check the output above."
    exit 1
fi

echo "✅ Build completed successfully"
echo

# Create the systemd service file
echo "📝 Creating systemd service file..."
SERVICE_FILE_CONTENT="[Unit]
Description=WellMonitor Device Service
After=network.target
Wants=network.target

[Service]
Type=exec
User=$USER_NAME
Group=$USER_NAME
WorkingDirectory=$WORK_DIR
ExecStart=$EXEC_PATH
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=WELLMONITOR_SECRETS_MODE=hybrid

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=wellmonitor

# Security
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target"

# Write the service file (requires sudo)
echo "$SERVICE_FILE_CONTENT" | sudo tee /etc/systemd/system/$SERVICE_NAME.service > /dev/null

echo "✅ Service file created: /etc/systemd/system/$SERVICE_NAME.service"
echo

# Set permissions
sudo chmod 644 /etc/systemd/system/$SERVICE_NAME.service

# Reload systemd and enable the service
echo "🔄 Reloading systemd and enabling service..."
sudo systemctl daemon-reload
sudo systemctl enable $SERVICE_NAME

echo "✅ Service enabled to start on boot"
echo

# Create debug images directory if it doesn't exist
DEBUG_DIR="$WORK_DIR/debug_images"
if [ ! -d "$DEBUG_DIR" ]; then
    echo "📁 Creating debug images directory..."
    mkdir -p "$DEBUG_DIR"
    echo "✅ Created: $DEBUG_DIR"
fi

# Start the service
echo "🚀 Starting WellMonitor service..."
sudo systemctl start $SERVICE_NAME

# Wait a moment for startup
sleep 3

# Check status
echo
echo "📊 Service Status:"
sudo systemctl status $SERVICE_NAME --no-pager

echo
echo "🎯 Setup Complete!"
echo "=================="
echo
echo "Service Commands:"
echo "• Check status:    sudo systemctl status $SERVICE_NAME"
echo "• View logs:       sudo journalctl -u $SERVICE_NAME -f"
echo "• Restart:         sudo systemctl restart $SERVICE_NAME"
echo "• Stop:            sudo systemctl stop $SERVICE_NAME"
echo "• Disable:         sudo systemctl disable $SERVICE_NAME"
echo
echo "Debug Commands:"
echo "• Debug images:    ls -la $DEBUG_DIR/"
echo "• Recent logs:     sudo journalctl -u $SERVICE_NAME --since '10 minutes ago'"
echo "• Camera test:     cd $WORK_DIR && dotnet run"
echo
echo "Next Steps:"
echo "1. Monitor logs: sudo journalctl -u $SERVICE_NAME -f"
echo "2. Check debug images are being created with LED optimizations"
echo "3. Verify device twin settings are being applied"
echo "4. Test camera captures with new LED settings"

echo
echo "✅ WellMonitor service is now running!"
