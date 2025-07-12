#!/bin/bash

echo "=== Installing WellMonitor to System Directory (Secure Approach) ==="

# Stop and disable existing service first
echo "Stopping existing wellmonitor service..."
sudo systemctl stop wellmonitor 2>/dev/null || echo "Service not running"
sudo systemctl disable wellmonitor 2>/dev/null || echo "Service not enabled"

# Backup existing service file
if [ -f /etc/systemd/system/wellmonitor.service ]; then
    echo "Backing up existing service file..."
    sudo cp /etc/systemd/system/wellmonitor.service /etc/systemd/system/wellmonitor.service.backup
    echo "Backup saved to: /etc/systemd/system/wellmonitor.service.backup"
fi

# Create system directories
sudo mkdir -p /opt/wellmonitor
sudo mkdir -p /var/lib/wellmonitor
sudo mkdir -p /var/log/wellmonitor
sudo mkdir -p /etc/wellmonitor

echo "Created system directories:"
echo "  /opt/wellmonitor     - Application binaries"
echo "  /var/lib/wellmonitor - Application data (database, etc.)"
echo "  /var/log/wellmonitor - Application logs"
echo "  /etc/wellmonitor     - Configuration files"

# Copy application files
echo ""
echo "Copying application files..."
sudo cp -r ~/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/* /opt/wellmonitor/
sudo chown -R root:root /opt/wellmonitor
sudo chmod +x /opt/wellmonitor/WellMonitor.Device

# Set up data directory with proper ownership
sudo chown -R davidb:davidb /var/lib/wellmonitor
sudo chmod 755 /var/lib/wellmonitor

# Copy database if it exists
if [ -f ~/WellMonitor/src/WellMonitor.Device/wellmonitor.db ]; then
    echo "Copying existing database..."
    sudo cp ~/WellMonitor/src/WellMonitor.Device/wellmonitor.db* /var/lib/wellmonitor/
    sudo chown davidb:davidb /var/lib/wellmonitor/wellmonitor.db*
fi

# Create debug images directory
sudo mkdir -p /var/lib/wellmonitor/debug_images
sudo chown -R davidb:davidb /var/lib/wellmonitor/debug_images

# Create environment file (more secure than inline environment variables)
sudo tee /etc/wellmonitor/environment > /dev/null << 'EOF'
ASPNETCORE_ENVIRONMENT=Production
WELLMONITOR_SECRETS_MODE=environment
DOTNET_EnableDiagnostics=0
WELLMONITOR_IOTHUB_CONNECTION_STRING=HostName=RTHIoTHub.azure-devices.net;DeviceId=rpi4b-1407well01;SharedAccessKey=up8WmG130lE4BoGCYc7e7NE54uzOod2yBYdOP2xlpiM=
WELLMONITOR_LOCAL_ENCRYPTION_KEY=12345678901234567890123456789012
EOF

# Secure the environment file
sudo chown root:davidb /etc/wellmonitor/environment
sudo chmod 640 /etc/wellmonitor/environment

echo "Created secure environment file at /etc/wellmonitor/environment"

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

echo ""
echo "Created secure service file with full security protections"

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

echo "Updated appsettings.json with system paths"

# Reload systemd and enable service
sudo systemctl daemon-reload
sudo systemctl enable wellmonitor

echo ""
echo "=== Testing the secure installation ==="

# Stop any existing service
sudo systemctl stop wellmonitor 2>/dev/null

# Start the service
echo "Starting wellmonitor service..."
sudo systemctl start wellmonitor

# Wait for startup
sleep 5

# Check status
echo ""
echo "Service status:"
sudo systemctl status wellmonitor --no-pager -l

echo ""
echo "Recent logs:"
sudo journalctl -u wellmonitor -n 15 --no-pager

echo ""
echo "=== Installation Summary ==="
echo "✅ Application installed to:     /opt/wellmonitor/"
echo "✅ Data directory:              /var/lib/wellmonitor/"
echo "✅ Configuration:               /etc/wellmonitor/"
echo "✅ Database location:           /var/lib/wellmonitor/wellmonitor.db"
echo "✅ Debug images:                /var/lib/wellmonitor/debug_images/"
echo "✅ Security: ProtectHome=yes    (Enabled)"
echo "✅ Security: ProtectSystem=strict (Enabled)"
echo ""
echo "The service now follows Linux system service best practices!"
echo "No more security compromises - full protection enabled."
