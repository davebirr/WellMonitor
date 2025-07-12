#!/bin/bash
# WellMonitor Service Diagnostic Script
# Helps troubleshoot service startup issues

SERVICE_NAME="wellmonitor"
USER_NAME=$(whoami)
WORK_DIR="/home/$USER_NAME/WellMonitor/src/WellMonitor.Device"
EXEC_PATH="/home/$USER_NAME/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/WellMonitor.Device"

echo "🔍 WellMonitor Service Diagnostics"
echo "=================================="
echo

echo "📊 System Information:"
echo "User: $USER_NAME"
echo "Working Directory: $WORK_DIR"
echo "Executable Path: $EXEC_PATH"
echo "Service Name: $SERVICE_NAME"
echo

echo "🔧 Checking Dependencies:"

# Check .NET Runtime
echo -n "• .NET Runtime: "
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ Found $DOTNET_VERSION"
else
    echo "❌ Not found"
fi

# Check if executable exists
echo -n "• Executable file: "
if [ -f "$EXEC_PATH" ]; then
    echo "✅ Found"
    ls -la "$EXEC_PATH"
else
    echo "❌ Not found at $EXEC_PATH"
    echo "   Checking for alternative locations..."
    find "$WORK_DIR/bin" -name "WellMonitor.Device*" -type f 2>/dev/null || echo "   No executables found"
fi

# Check working directory
echo -n "• Working directory: "
if [ -d "$WORK_DIR" ]; then
    echo "✅ Found"
else
    echo "❌ Not found"
fi

# Check project file
echo -n "• Project file: "
PROJECT_FILE="$WORK_DIR/WellMonitor.Device.csproj"
if [ -f "$PROJECT_FILE" ]; then
    echo "✅ Found"
else
    echo "❌ Not found"
fi

echo

echo "🚀 Testing Manual Execution:"
if [ -f "$EXEC_PATH" ]; then
    echo "Testing with dotnet command..."
    cd "$WORK_DIR"
    
    echo "Command: dotnet $EXEC_PATH"
    echo "Working directory: $(pwd)"
    echo
    echo "Press Ctrl+C to stop the test..."
    echo "----------------------------------------"
    
    # Try to run for 10 seconds
    timeout 10s dotnet "$EXEC_PATH" || echo "Test completed or failed"
    
    echo
    echo "If the app ran without errors, the service should work."
    echo "If there were errors, those need to be fixed first."
else
    echo "❌ Cannot test - executable not found"
fi

echo

echo "📜 Recent Service Logs:"
sudo journalctl -u $SERVICE_NAME --since "10 minutes ago" --no-pager | tail -20

echo

echo "🔄 Service Status:"
sudo systemctl status $SERVICE_NAME --no-pager

echo

echo "🛠️ Troubleshooting Commands:"
echo "• View full logs:     sudo journalctl -u $SERVICE_NAME -f"
echo "• Stop service:       sudo systemctl stop $SERVICE_NAME"
echo "• Start service:      sudo systemctl start $SERVICE_NAME"
echo "• Restart service:    sudo systemctl restart $SERVICE_NAME"
echo "• Disable service:    sudo systemctl disable $SERVICE_NAME"
echo "• Manual test:        cd $WORK_DIR && dotnet $EXEC_PATH"
echo "• Check build:        cd $WORK_DIR && dotnet build"
