#!/bin/bash

# WellMonitor Systemd Service Setup Script (Improved)
# This script sets up WellMonitor as a systemd service with better deployment detection

set -e  # Exit on any error

# Configuration
SERVICE_NAME="wellmonitor"
USER_NAME="${USER:-$(whoami)}"
PROJECT_DIR="$(pwd)"
WORK_DIR="$PROJECT_DIR/src/WellMonitor.Device"

echo "🔧 Setting up WellMonitor as a systemd service..."
echo "=================================================="
echo "User: $USER_NAME"
echo "Working Directory: $WORK_DIR"

# Change to project directory
cd "$WORK_DIR"

# Build the project
echo
echo "🔨 Building WellMonitor project..."
dotnet publish -c Release -r linux-arm64 --self-contained true

# Check build artifacts to determine deployment type
PUBLISH_DIR="$WORK_DIR/bin/Release/net8.0/linux-arm64/publish"
RUNTIME_DIR="$WORK_DIR/bin/Release/net8.0/linux-arm64"
DLL_PATH="$RUNTIME_DIR/WellMonitor.Device.dll"
EXE_PATH="$RUNTIME_DIR/WellMonitor.Device"
PUBLISH_EXE_PATH="$PUBLISH_DIR/WellMonitor.Device"

# Determine the best execution method
if [ -f "$PUBLISH_EXE_PATH" ]; then
    echo "✅ Found self-contained publish executable: $PUBLISH_EXE_PATH"
    EXEC_START="$PUBLISH_EXE_PATH"
    EXEC_TYPE="self-contained"
elif [ -f "$EXE_PATH" ]; then
    echo "✅ Found runtime executable: $EXE_PATH"
    EXEC_START="$EXE_PATH"
    EXEC_TYPE="runtime"
elif [ -f "$DLL_PATH" ]; then
    echo "✅ Found DLL, using dotnet runtime: $DLL_PATH"
    EXEC_START="/usr/bin/dotnet $DLL_PATH"
    EXEC_TYPE="framework-dependent"
else
    echo "❌ No executable found. Build may have failed."
    echo "   Checked:"
    echo "   - $PUBLISH_EXE_PATH"
    echo "   - $EXE_PATH"
    echo "   - $DLL_PATH"
    exit 1
fi

echo "Execution Type: $EXEC_TYPE"
echo "Execution Command: $EXEC_START"
echo

# Create the systemd service file
echo "📝 Creating systemd service file..."

# Check if user has environment variables set
ENV_VARS=""
if [ -n "$WELLMONITOR_IOTHUB_CONNECTION_STRING" ]; then
    ENV_VARS="Environment=WELLMONITOR_IOTHUB_CONNECTION_STRING=$WELLMONITOR_IOTHUB_CONNECTION_STRING"
    echo "✅ Found WELLMONITOR_IOTHUB_CONNECTION_STRING in environment"
else
    echo "⚠️  WELLMONITOR_IOTHUB_CONNECTION_STRING not found in environment"
    echo "   You'll need to set environment variables manually after service creation"
fi

SERVICE_FILE_CONTENT="[Unit]
Description=WellMonitor Device Service
After=network.target
Wants=network.target

[Service]
Type=exec
User=$USER_NAME
Group=$USER_NAME
WorkingDirectory=$WORK_DIR
ExecStart=$EXEC_START
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=WELLMONITOR_SECRETS_MODE=environment
Environment=DOTNET_EnableDiagnostics=0
$ENV_VARS

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=$SERVICE_NAME

# Security
NoNewPrivileges=yes
PrivateTmp=yes
ProtectHome=yes
ProtectSystem=strict
ReadWritePaths=$WORK_DIR

[Install]
WantedBy=multi-user.target"

# Write the service file
echo "$SERVICE_FILE_CONTENT" | sudo tee /etc/systemd/system/$SERVICE_NAME.service > /dev/null
echo "✅ Service file created: /etc/systemd/system/$SERVICE_NAME.service"

# Reload systemd and enable the service
echo
echo "🔄 Reloading systemd and enabling service..."
sudo systemctl daemon-reload
sudo systemctl enable $SERVICE_NAME
echo "✅ Service enabled to start on boot"

# Start the service
echo
echo "🚀 Starting WellMonitor service..."
sudo systemctl start $SERVICE_NAME

# Wait a moment for the service to start
sleep 3

# Show service status
echo
echo "📊 Service Status:"
sudo systemctl status $SERVICE_NAME --no-pager -l

echo
echo "🎉 Service setup complete!"
echo
echo "📝 Useful commands:"
echo "• View logs:          sudo journalctl -u $SERVICE_NAME -f"
echo "• Restart service:    sudo systemctl restart $SERVICE_NAME"
echo "• Stop service:       sudo systemctl stop $SERVICE_NAME"
echo "• Check status:       sudo systemctl status $SERVICE_NAME"
echo "• Manual test:        cd $WORK_DIR && $EXEC_START"
echo
echo "⚠️  IMPORTANT: Environment Variables"
echo "If the service fails to start, you may need to set environment variables:"
echo "• Edit service:       sudo systemctl edit $SERVICE_NAME --full"
echo "• Add this line:      Environment=WELLMONITOR_IOTHUB_CONNECTION_STRING=your-connection-string"
echo "• Then reload:        sudo systemctl daemon-reload && sudo systemctl restart $SERVICE_NAME"
echo
echo "See environment-setup.md for detailed configuration instructions."
