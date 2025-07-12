#!/bin/bash

echo "=== Creating Fixed WellMonitor Service ==="

# Create the corrected service file
sudo tee /etc/systemd/system/wellmonitor.service > /dev/null << 'EOF'
[Unit]
Description=WellMonitor Device Service
After=network.target
Wants=network.target

[Service]
Type=exec
User=davidb
Group=davidb
WorkingDirectory=/home/davidb/WellMonitor/src/WellMonitor.Device
ExecStart=/home/davidb/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/WellMonitor.Device
Restart=always
RestartSec=10

# Environment Variables
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=WELLMONITOR_SECRETS_MODE=environment
Environment=DOTNET_EnableDiagnostics=0
Environment=WELLMONITOR_IOTHUB_CONNECTION_STRING=HostName=RTHIoTHub.azure-devices.net;DeviceId=rpi4b-1407well01;SharedAccessKey=up8WmG130lE4BoGCYc7e7NE54uzOod2yBYdOP2xlpiM=
Environment=WELLMONITOR_LOCAL_ENCRYPTION_KEY=12345678901234567890123456789012

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=wellmonitor

# Security - RELAXED SETTINGS for /home access
NoNewPrivileges=yes
PrivateTmp=yes
# ProtectHome=yes  # <-- REMOVED - This was blocking access to /home/davidb/
ProtectSystem=no    # <-- CHANGED from strict to no
ReadWritePaths=/home/davidb/WellMonitor

[Install]
WantedBy=multi-user.target
EOF

echo "Service file created with relaxed security settings"

# Reload systemd to pick up the new service file
echo "Reloading systemd daemon..."
sudo systemctl daemon-reload

# Enable the service
echo "Enabling wellmonitor service..."
sudo systemctl enable wellmonitor

echo ""
echo "=== Testing the fixed service ==="

# Stop any existing service
sudo systemctl stop wellmonitor 2>/dev/null

# Start the service
echo "Starting wellmonitor service..."
sudo systemctl start wellmonitor

# Wait a moment for startup
sleep 3

# Check status
echo ""
echo "Service status:"
sudo systemctl status wellmonitor --no-pager -l

echo ""
echo "Recent logs:"
sudo journalctl -u wellmonitor -n 10 --no-pager

echo ""
echo "=== Service Management Commands ==="
echo "Check status:    sudo systemctl status wellmonitor"
echo "View logs:       sudo journalctl -u wellmonitor -f"
echo "Stop service:    sudo systemctl stop wellmonitor"
echo "Start service:   sudo systemctl start wellmonitor"
echo "Restart service: sudo systemctl restart wellmonitor"
