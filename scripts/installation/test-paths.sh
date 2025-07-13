#!/bin/bash

# Test script to verify path calculations for installation scripts

echo "=== Path Calculation Test ==="

# Simulate being in scripts/installation/ directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
DEVICE_PROJECT="$PROJECT_ROOT/src/WellMonitor.Device"

echo "Current script location: $SCRIPT_DIR"
echo "Calculated PROJECT_ROOT: $PROJECT_ROOT"
echo "Calculated DEVICE_PROJECT: $DEVICE_PROJECT"
echo ""

# Verify the paths exist
echo "=== Path Verification ==="
if [ -f "$PROJECT_ROOT/WellMonitor.sln" ]; then
    echo "✅ Found WellMonitor.sln at project root"
else
    echo "❌ WellMonitor.sln not found at: $PROJECT_ROOT/WellMonitor.sln"
fi

if [ -f "$DEVICE_PROJECT/WellMonitor.Device.csproj" ]; then
    echo "✅ Found WellMonitor.Device.csproj"
else
    echo "❌ WellMonitor.Device.csproj not found at: $DEVICE_PROJECT/WellMonitor.Device.csproj"
fi

if [ -d "$PROJECT_ROOT/src" ]; then
    echo "✅ Found src directory"
else
    echo "❌ src directory not found at: $PROJECT_ROOT/src"
fi

echo ""
echo "=== Git Information ==="
cd "$PROJECT_ROOT"
echo "Git branch: $(git branch --show-current 2>/dev/null || echo 'Not a git repo')"
echo "Last commit: $(git log -1 --oneline 2>/dev/null || echo 'No git log')"
