#!/bin/bash
# Camera DMA Error Fix Script
# Resolves "Could not open any dmaHeap device" camera errors

echo "🔧 WellMonitor Camera DMA Error Fix"
echo "===================================="
echo

# Check current camera status
echo "📊 Current Camera Status:"
echo "========================="

# Check camera detection
echo -n "• Camera detection: "
if timeout 10s libcamera-hello --list-cameras &>/dev/null; then
    echo "✅ Camera detected"
    libcamera-hello --list-cameras 2>/dev/null | head -5
else
    echo "❌ Camera not detected or timeout"
fi

# Check GPU memory
echo
echo -n "• GPU memory allocation: "
GPU_MEM=$(vcgencmd get_mem gpu 2>/dev/null | cut -d= -f2)
if [ -n "$GPU_MEM" ]; then
    echo "$GPU_MEM"
    GPU_VALUE=$(echo "$GPU_MEM" | sed 's/M//')
    if [ "$GPU_VALUE" -lt 128 ]; then
        echo "  ⚠️  Recommended: 128M or higher for camera operation"
    else
        echo "  ✅ Sufficient for camera operation"
    fi
else
    echo "❌ Could not determine GPU memory"
fi

# Check boot configuration
echo
echo "• Boot configuration:"
CAMERA_AUTO=$(grep -E "^camera_auto_detect" /boot/config.txt 2>/dev/null)
START_X=$(grep -E "^start_x" /boot/config.txt 2>/dev/null)
GPU_MEM_CONFIG=$(grep -E "^gpu_mem" /boot/config.txt 2>/dev/null)

if [ -n "$CAMERA_AUTO" ]; then
    echo "  ✅ $CAMERA_AUTO"
elif [ -n "$START_X" ]; then
    echo "  ✅ $START_X (legacy)"
else
    echo "  ⚠️  No explicit camera configuration found"
fi

if [ -n "$GPU_MEM_CONFIG" ]; then
    echo "  ✅ $GPU_MEM_CONFIG"
else
    echo "  ⚠️  No GPU memory configuration found"
fi

# Check for conflicting processes
echo
echo -n "• Camera process conflicts: "
CAMERA_PROCS=$(pgrep -f "libcamera|rpicam" | wc -l)
if [ "$CAMERA_PROCS" -gt 0 ]; then
    echo "⚠️  $CAMERA_PROCS camera processes running"
    pgrep -f "libcamera|rpicam" | xargs ps -p 2>/dev/null || true
else
    echo "✅ No conflicting processes"
fi

# Test basic camera capture
echo
echo "🔬 Camera Functionality Test:"
echo "============================="

TEST_DIR="/tmp/camera-fix-test"
mkdir -p "$TEST_DIR"

echo "• Testing basic capture..."
if timeout 15s libcamera-still --output "$TEST_DIR/test_basic.jpg" --timeout 2000 --nopreview 2>/dev/null; then
    if [ -f "$TEST_DIR/test_basic.jpg" ]; then
        SIZE=$(stat -c%s "$TEST_DIR/test_basic.jpg" 2>/dev/null || echo "0")
        echo "  ✅ Basic capture successful ($SIZE bytes)"
    else
        echo "  ❌ Command succeeded but no file created"
    fi
else
    echo "  ❌ Basic capture failed"
    echo "  Testing with verbose output..."
    timeout 10s libcamera-still --output "$TEST_DIR/test_verbose.jpg" --timeout 2000 --nopreview --verbose 2>&1 | head -10
fi

# Propose fixes
echo
echo "🔧 Recommended Fixes:"
echo "===================="

NEEDS_REBOOT=false

# Fix 1: GPU Memory
if [ -z "$GPU_MEM_CONFIG" ] || [ "${GPU_VALUE:-0}" -lt 128 ]; then
    echo "1. Increase GPU memory allocation:"
    echo "   sudo sed -i '/^gpu_mem=/d' /boot/config.txt"
    echo "   echo 'gpu_mem=128' | sudo tee -a /boot/config.txt"
    NEEDS_REBOOT=true
fi

# Fix 2: Camera Interface
if [ -z "$CAMERA_AUTO" ] && [ -z "$START_X" ]; then
    echo "2. Enable camera interface:"
    echo "   echo 'camera_auto_detect=1' | sudo tee -a /boot/config.txt"
    NEEDS_REBOOT=true
fi

# Fix 3: Kill conflicting processes
if [ "$CAMERA_PROCS" -gt 0 ]; then
    echo "3. Kill conflicting camera processes:"
    echo "   sudo pkill -f libcamera"
    echo "   sudo pkill -f rpicam"
fi

# Fix 4: Service restart
echo "4. Restart WellMonitor service:"
echo "   sudo systemctl restart wellmonitor"

if $NEEDS_REBOOT; then
    echo
    echo "⚠️  REBOOT REQUIRED after boot configuration changes"
    echo "    sudo reboot"
fi

echo
echo "🚀 Quick Fix Script:"
echo "==================="
echo "#!/bin/bash"
echo "# Apply all fixes automatically"
echo

if [ -z "$GPU_MEM_CONFIG" ] || [ "${GPU_VALUE:-0}" -lt 128 ]; then
    echo "# Fix GPU memory"
    echo "sudo sed -i '/^gpu_mem=/d' /boot/config.txt"
    echo "echo 'gpu_mem=128' | sudo tee -a /boot/config.txt"
fi

if [ -z "$CAMERA_AUTO" ] && [ -z "$START_X" ]; then
    echo "# Enable camera"
    echo "echo 'camera_auto_detect=1' | sudo tee -a /boot/config.txt"
fi

if [ "$CAMERA_PROCS" -gt 0 ]; then
    echo "# Kill camera processes"
    echo "sudo pkill -f libcamera 2>/dev/null || true"
    echo "sudo pkill -f rpicam 2>/dev/null || true"
fi

echo "# Restart service"
echo "sudo systemctl restart wellmonitor"

if $NEEDS_REBOOT; then
    echo "# Reboot system"
    echo "echo 'Rebooting in 10 seconds...'"
    echo "sleep 10"
    echo "sudo reboot"
fi

echo
echo "💡 To apply fixes automatically:"
echo "   $0 --fix"
echo
echo "🔍 To monitor camera after fixes:"
echo "   sudo journalctl -u wellmonitor -f | grep -i camera"

# Auto-fix option
if [ "$1" = "--fix" ]; then
    echo
    echo "🔧 Applying fixes automatically..."
    echo "================================="
    
    # Apply GPU memory fix
    if [ -z "$GPU_MEM_CONFIG" ] || [ "${GPU_VALUE:-0}" -lt 128 ]; then
        echo "• Setting GPU memory to 128MB..."
        sudo sed -i '/^gpu_mem=/d' /boot/config.txt
        echo 'gpu_mem=128' | sudo tee -a /boot/config.txt
    fi
    
    # Apply camera interface fix
    if [ -z "$CAMERA_AUTO" ] && [ -z "$START_X" ]; then
        echo "• Enabling camera interface..."
        echo 'camera_auto_detect=1' | sudo tee -a /boot/config.txt
    fi
    
    # Kill conflicting processes
    if [ "$CAMERA_PROCS" -gt 0 ]; then
        echo "• Killing conflicting camera processes..."
        sudo pkill -f libcamera 2>/dev/null || true
        sudo pkill -f rpicam 2>/dev/null || true
        sleep 2
    fi
    
    # Restart service
    echo "• Restarting WellMonitor service..."
    sudo systemctl restart wellmonitor
    
    if $NEEDS_REBOOT; then
        echo
        echo "⚠️  Boot configuration changed - reboot required"
        echo "   Rebooting in 10 seconds (Ctrl+C to cancel)..."
        sleep 10
        sudo reboot
    else
        echo
        echo "✅ Fixes applied! Monitor service logs:"
        echo "   sudo journalctl -u wellmonitor -f | grep -i camera"
    fi
fi

# Cleanup
rm -rf "$TEST_DIR" 2>/dev/null || true

echo
echo "📚 Documentation: docs/configuration/camera-ocr-setup.md"
echo "🔧 Complete diagnostics: ./scripts/diagnostics/diagnose-system.sh"
