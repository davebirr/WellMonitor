#!/bin/bash
# WellMonitor Comprehensive Diagnostic Script
# Consolidates service, camera, and system diagnostics in one tool

set -e

# Configuration
SERVICE_NAME="wellmonitor"
USER_NAME=$(whoami)
PROJECT_ROOT="/home/$USER_NAME/WellMonitor"
WORK_DIR="$PROJECT_ROOT/src/WellMonitor.Device"
EXEC_PATH="$WORK_DIR/bin/Release/net8.0/linux-arm64/WellMonitor.Device"
DEBUG_DIR="$WORK_DIR/debug_images"
TEST_DIR="/tmp/wellmonitor-diagnostics"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
print_header() {
    echo -e "\n${BLUE}🔍 $1${NC}"
    echo "=================================="
}

print_success() {
    echo -e "   ${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "   ${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "   ${RED}❌ $1${NC}"
}

print_info() {
    echo -e "   ${BLUE}ℹ️  $1${NC}"
}

# Create test directory
mkdir -p "$TEST_DIR"

print_header "WellMonitor System Diagnostics"
echo "User: $USER_NAME"
echo "Project Root: $PROJECT_ROOT"
echo "Working Directory: $WORK_DIR"
echo "Executable Path: $EXEC_PATH"
echo "Service Name: $SERVICE_NAME"
echo "Debug Directory: $DEBUG_DIR"

# 1. System Dependencies
print_header "System Dependencies"

# Check .NET Runtime
echo -n "• .NET Runtime: "
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    print_success "Found $DOTNET_VERSION"
else
    print_error "Not found - install with: curl -sSL https://dot.net/v1/dotnet-install.sh | bash"
fi

# Check libcamera
echo -n "• Camera Libraries: "
if command -v libcamera-still &> /dev/null; then
    print_success "libcamera-still available"
else
    print_error "Not found - install with: sudo apt install libcamera-apps"
fi

# Check systemd
echo -n "• Systemd: "
if command -v systemctl &> /dev/null; then
    print_success "Available"
else
    print_error "Not found"
fi

# 2. Project Structure
print_header "Project Structure"

# Check project directory
echo -n "• Project directory: "
if [ -d "$PROJECT_ROOT" ]; then
    print_success "Found"
else
    print_error "Not found at $PROJECT_ROOT"
fi

# Check working directory
echo -n "• Working directory: "
if [ -d "$WORK_DIR" ]; then
    print_success "Found"
else
    print_error "Not found at $WORK_DIR"
fi

# Check project file
echo -n "• Project file: "
PROJECT_FILE="$WORK_DIR/WellMonitor.Device.csproj"
if [ -f "$PROJECT_FILE" ]; then
    print_success "Found"
else
    print_error "Not found at $PROJECT_FILE"
fi

# Check executable
echo -n "• Executable file: "
if [ -f "$EXEC_PATH" ]; then
    print_success "Found"
    SIZE=$(stat -c%s "$EXEC_PATH" 2>/dev/null || echo "0")
    echo "     Size: $SIZE bytes"
    echo "     Modified: $(stat -c%y "$EXEC_PATH" 2>/dev/null || echo "unknown")"
else
    print_error "Not found at $EXEC_PATH"
    echo "   Checking for alternative locations..."
    find "$WORK_DIR/bin" -name "WellMonitor.Device*" -type f 2>/dev/null | head -5 || echo "   No executables found"
fi

# Check debug directory
echo -n "• Debug directory: "
if [ -d "$DEBUG_DIR" ]; then
    print_success "Found"
    IMAGE_COUNT=$(find "$DEBUG_DIR" -name "*.jpg" 2>/dev/null | wc -l)
    echo "     Images: $IMAGE_COUNT"
else
    print_warning "Not found - will be created automatically"
fi

# 3. Camera Hardware
print_header "Camera Hardware"

# Check camera detection
echo -n "• Camera detection: "
if command -v libcamera-hello &> /dev/null; then
    if timeout 10s libcamera-hello --list-cameras &>/dev/null; then
        print_success "Camera detected"
        timeout 10s libcamera-hello --list-cameras 2>&1 | head -5
    else
        print_error "No cameras detected"
    fi
else
    print_error "libcamera-hello not available"
fi

# Check camera interface
echo -n "• Camera interface: "
if grep -q "camera_auto_detect=1" /boot/config.txt 2>/dev/null; then
    print_success "Auto-detect enabled"
elif grep -q "start_x=1" /boot/config.txt 2>/dev/null; then
    print_success "Legacy support enabled"
else
    # In modern Pi OS, camera is enabled by default
    if [ -d "/sys/class/video4linux" ] && [ "$(ls -A /sys/class/video4linux 2>/dev/null)" ]; then
        print_success "Camera available (modern Pi OS - no config needed)"
    else
        print_warning "Camera may need configuration - check GPU memory and /boot/config.txt"
        echo "     Modern Pi OS: Camera should work by default"
        echo "     If issues persist: sudo nano /boot/config.txt"
        echo "     Add: gpu_mem=128 and camera_auto_detect=1"
    fi
fi

# Check camera permissions
echo -n "• Camera permissions: "
if groups | grep -q video; then
    print_success "User in video group"
else
    print_warning "User not in video group - run: sudo usermod -a -G video $USER_NAME"
fi

# 4. Camera Testing
print_header "Camera Testing"

# Test 1: Basic capture
echo "• Test 1: Basic capture..."
if timeout 15s libcamera-still --output "$TEST_DIR/test_basic.jpg" --timeout 5000 --width 640 --height 480 --nopreview 2>/dev/null; then
    if [ -f "$TEST_DIR/test_basic.jpg" ]; then
        SIZE=$(stat -c%s "$TEST_DIR/test_basic.jpg" 2>/dev/null || echo "0")
        print_success "Basic capture successful ($SIZE bytes)"
    else
        print_error "Command succeeded but no file created"
    fi
else
    print_error "Basic capture failed"
fi

# Test 2: WellMonitor-style capture
echo "• Test 2: WellMonitor-style capture..."
if timeout 15s libcamera-still --output "$TEST_DIR/test_wellmonitor.jpg" --width 1920 --height 1080 --quality 85 --timeout 2000 --encoding jpg --immediate --nopreview 2>/dev/null; then
    if [ -f "$TEST_DIR/test_wellmonitor.jpg" ]; then
        SIZE=$(stat -c%s "$TEST_DIR/test_wellmonitor.jpg" 2>/dev/null || echo "0")
        print_success "WellMonitor-style capture successful ($SIZE bytes)"
    else
        print_error "Command succeeded but no file created"
    fi
else
    print_error "WellMonitor-style capture failed"
fi

# Test 3: LED optimized capture
echo "• Test 3: LED optimized capture..."
if timeout 15s libcamera-still --output "$TEST_DIR/test_led.jpg" --width 1920 --height 1080 --quality 85 --timeout 2000 --encoding jpg --immediate --nopreview --gain 12.0 --shutter 50000 --awb off --analoggain 8.0 2>/dev/null; then
    if [ -f "$TEST_DIR/test_led.jpg" ]; then
        SIZE=$(stat -c%s "$TEST_DIR/test_led.jpg" 2>/dev/null || echo "0")
        print_success "LED optimized capture successful ($SIZE bytes)"
    else
        print_error "Command succeeded but no file created"
    fi
else
    print_error "LED optimized capture failed"
fi

# 5. Service Status
print_header "Service Status"

# Check service existence
echo -n "• Service file: "
SERVICE_FILE="/etc/systemd/system/$SERVICE_NAME.service"
if [ -f "$SERVICE_FILE" ]; then
    print_success "Found at $SERVICE_FILE"
else
    print_error "Not found at $SERVICE_FILE"
fi

# Check service status
echo -n "• Service status: "
if systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
    print_success "Running"
elif systemctl is-enabled --quiet "$SERVICE_NAME" 2>/dev/null; then
    print_warning "Installed but not running"
else
    print_error "Not installed or not enabled"
fi

# Show recent logs
echo "• Recent service logs:"
if sudo journalctl -u "$SERVICE_NAME" --since "10 minutes ago" --no-pager -q 2>/dev/null | tail -5 | grep -q .; then
    sudo journalctl -u "$SERVICE_NAME" --since "10 minutes ago" --no-pager -q | tail -5 | sed 's/^/     /'
else
    echo "     No recent logs found"
fi

# 6. Manual Execution Test
print_header "Manual Execution Test"

if [ -f "$EXEC_PATH" ]; then
    echo "• Testing manual execution (10 second timeout)..."
    cd "$WORK_DIR"
    
    print_info "Command: dotnet $EXEC_PATH"
    print_info "Working directory: $(pwd)"
    echo
    
    # Try to run for 10 seconds
    if timeout 10s dotnet "$EXEC_PATH" 2>&1 | head -10; then
        print_success "Application started successfully"
    else
        print_error "Application failed to start or timed out"
    fi
else
    print_error "Cannot test - executable not found"
fi

# 7. Debug Images Analysis
print_header "Debug Images Analysis"

if [ -d "$DEBUG_DIR" ]; then
    RECENT_FILES=$(find "$DEBUG_DIR" -name "*.jpg" -mtime -1 2>/dev/null | sort -r | head -5)
    if [ -n "$RECENT_FILES" ]; then
        print_success "Recent debug images found:"
        for file in $RECENT_FILES; do
            SIZE=$(stat -c%s "$file" 2>/dev/null || echo "0")
            TIMESTAMP=$(basename "$file" | sed 's/.*_\([0-9]\{8\}_[0-9]\{6\}\).jpg/\1/')
            echo "     $(basename "$file"): $SIZE bytes (timestamp: $TIMESTAMP)"
        done
    else
        print_warning "No recent debug images found"
    fi
    
    # Check total images
    TOTAL_IMAGES=$(find "$DEBUG_DIR" -name "*.jpg" 2>/dev/null | wc -l)
    echo "     Total images: $TOTAL_IMAGES"
else
    print_warning "Debug images directory not found"
fi

# 8. Configuration Analysis
print_header "Configuration Analysis"

CONFIG_FILE="$WORK_DIR/appsettings.json"
if [ -f "$CONFIG_FILE" ]; then
    print_success "Configuration file found"
    
    # Check for camera settings
    if grep -q "Camera" "$CONFIG_FILE" 2>/dev/null; then
        print_info "Camera settings found in configuration"
    else
        print_warning "No camera settings found in configuration"
    fi
    
    # Check for debug settings
    if grep -q "debugImagePath" "$CONFIG_FILE" 2>/dev/null; then
        DEBUG_PATH=$(grep "debugImagePath" "$CONFIG_FILE" | cut -d'"' -f4)
        print_info "Debug image path: $DEBUG_PATH"
    else
        print_info "No debug image path configured"
    fi
else
    print_warning "Configuration file not found at $CONFIG_FILE"
fi

# 9. Recommendations
print_header "Recommendations"

# Analyze results and provide recommendations
CAMERA_WORKS=false
SERVICE_WORKS=false

if [ -f "$TEST_DIR/test_basic.jpg" ] || [ -f "$TEST_DIR/test_wellmonitor.jpg" ]; then
    CAMERA_WORKS=true
fi

if systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
    SERVICE_WORKS=true
fi

if ! $CAMERA_WORKS; then
    print_error "CRITICAL: Camera not working"
    echo "   Solutions:"
    echo "   1. Check camera cable connection"
    echo "   2. Enable camera: sudo raspi-config → Interface Options → Camera"
    echo "   3. Reboot: sudo reboot"
    echo "   4. Install camera tools: sudo apt install libcamera-apps"
elif ! $SERVICE_WORKS; then
    print_warning "Camera works but service has issues"
    echo "   Solutions:"
    echo "   1. Check service logs: sudo journalctl -u $SERVICE_NAME -f"
    echo "   2. Test manual execution: cd $WORK_DIR && dotnet $EXEC_PATH"
    echo "   3. Rebuild application: cd $WORK_DIR && dotnet publish -c Release -r linux-arm64"
    echo "   4. Reinstall service: ./scripts/installation/install-wellmonitor.sh"
else
    print_success "System appears healthy"
    echo "   Maintenance tasks:"
    echo "   1. Monitor logs: sudo journalctl -u $SERVICE_NAME -f"
    echo "   2. Check debug images: ls -la $DEBUG_DIR/"
    echo "   3. Update device twin settings via Azure portal"
fi

# 10. Useful Commands
print_header "Useful Commands"

echo "• View live logs:         sudo journalctl -u $SERVICE_NAME -f"
echo "• Restart service:        sudo systemctl restart $SERVICE_NAME"
echo "• Manual test:            cd $WORK_DIR && dotnet $EXEC_PATH"
echo "• Rebuild application:    cd $WORK_DIR && dotnet publish -c Release -r linux-arm64"
echo "• Test camera:            libcamera-still --output test.jpg --timeout 5000"
echo "• View test images:       ls -la $TEST_DIR/"
echo "• Copy images to PC:      scp pi@your-pi:$TEST_DIR/*.jpg ."
echo "• Clean test files:       rm -rf $TEST_DIR"
echo "• Update LED settings:    ./scripts/configuration/update-device-twin.ps1"
echo "• Full reinstall:         ./scripts/installation/install-wellmonitor.sh"

print_header "Diagnostic Summary"

echo "Test images saved in: $TEST_DIR/"
echo "Review images to check camera quality and LED visibility"
echo "Use diagnostic information above to troubleshoot specific issues"
echo ""
print_info "Diagnostic complete - see recommendations above"
