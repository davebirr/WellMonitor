#!/bin/bash

echo "=== Creating Secure WellMonitor Service with Home Directory Access ==="

# Create override directory
sudo mkdir -p /etc/systemd/system/wellmonitor.service.d

# Create override file that allows specific home directory access
sudo tee /etc/systemd/system/wellmonitor.service.d/override.conf > /dev/null << 'EOF'
[Service]
# Override security settings to allow specific home directory access
ProtectHome=read-only
ReadWritePaths=/home/davidb/WellMonitor/src/WellMonitor.Device
ReadWritePaths=/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images
ReadWritePaths=/home/davidb/WellMonitor/src/WellMonitor.Device/Data

# Additional security hardening
ProtectKernelTunables=yes
ProtectKernelModules=yes
ProtectControlGroups=yes
RestrictRealtime=yes
SystemCallArchitectures=native

# Device access for GPIO and camera
DeviceAllow=/dev/gpiochip0 rw
DeviceAllow=/dev/video0 rw
DeviceAllow=/dev/video1 rw
SupplementaryGroups=gpio video
EOF

echo "Created systemd override with selective home directory access"

# Reload systemd
sudo systemctl daemon-reload

echo ""
echo "Service security settings:"
echo "✅ ProtectHome=read-only (Safer than no protection)"
echo "✅ Specific ReadWritePaths defined"
echo "✅ Additional security hardening enabled"
echo "✅ Device access properly configured"

# Test the service
sudo systemctl restart wellmonitor
sleep 3
sudo systemctl status wellmonitor --no-pager -l
