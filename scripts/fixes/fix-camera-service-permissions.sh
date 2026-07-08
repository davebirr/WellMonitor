#!/bin/bash

echo "=== Camera Service Fix for WellMonitor ==="
echo "This script fixes the recurring camera access issue in systemd service"
echo ""

# Check if running as root
if [ "$EUID" -eq 0 ]; then
    echo "❌ Do not run this script as root. Run as the wellmonitor user (davidb)."
    exit 1
fi

# Backup the current service file
echo "📋 Backing up current service file..."
sudo cp /etc/systemd/system/wellmonitor.service /etc/systemd/system/wellmonitor.service.backup.camera-fix.$(date +%Y%m%d_%H%M%S)

# Create the camera-optimized service file
echo "🔧 Creating camera-optimized service file..."
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

# CAMERA FIX: Relaxed security for camera access
# The camera requires access to various system resources that strict security blocks
NoNewPrivileges=yes
PrivateTmp=yes

# CAMERA FIX: Allow home access for camera initialization
ProtectHome=no

# CAMERA FIX: Reduce system protection to allow camera access
ProtectSystem=yes

# Keep other security settings that don't interfere with camera
ProtectKernelTunables=yes
ProtectKernelModules=yes
ProtectControlGroups=yes
RestrictRealtime=yes
SystemCallArchitectures=native

# CAMERA FIX: More permissive device access
DevicePolicy=auto

# Allow access to specific directories
ReadWritePaths=/var/lib/wellmonitor
ReadWritePaths=/var/log/wellmonitor
ReadWritePaths=/tmp
ReadOnlyPaths=/etc/wellmonitor

# CAMERA FIX: Comprehensive device access for camera and GPIO
DeviceAllow=/dev/gpiochip* rw
DeviceAllow=/dev/video* rw
DeviceAllow=/dev/media* rw
DeviceAllow=/dev/dma_heap* rw
DeviceAllow=/dev/dri* rw
DeviceAllow=/dev/vchiq rw
DeviceAllow=/dev/vcio rw
DeviceAllow=/dev/vcsm-cma rw
DeviceAllow=/dev/fb* rw
DeviceAllow=/dev/char/226:* rw

# CAMERA FIX: Essential groups for camera access
SupplementaryGroups=gpio video render dialout

[Install]
WantedBy=multi-user.target
EOF

echo "✅ Camera-optimized service file created"

# Reload systemd and restart service
echo "🔄 Reloading systemd configuration..."
sudo systemctl daemon-reload

echo "🛑 Stopping wellmonitor service..."
sudo systemctl stop wellmonitor

echo "🚀 Starting wellmonitor service with camera fix..."
sudo systemctl start wellmonitor

# Wait a moment for service to start
sleep 3

# Check service status
echo ""
echo "📊 Service Status:"
sudo systemctl status wellmonitor --no-pager -l

echo ""
echo "🔍 Testing camera access in service (checking recent logs)..."
sleep 5
journalctl -u wellmonitor --since "1 minute ago" | grep -i -E "(camera|no cameras|available)" | tail -5

echo ""
echo "=== Camera Fix Applied ==="
echo "Monitor the logs with: journalctl -u wellmonitor -f"
echo "If this fixes the camera issue, document it in copilot-instructions.md"
