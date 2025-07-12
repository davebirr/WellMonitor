#!/bin/bash

# Diagnose Debug Image Path Configuration on Raspberry Pi
# This script checks the current debug image path configuration and shows
# where files would be saved based on the current app directory structure.

echo "🔍 Debug Image Path Diagnostics"
echo "==============================="
echo ""

# Show current working directory
echo "Current Working Directory:"
echo "  $(pwd)"
echo ""

# Show expected app directory structure
echo "Expected App Structure:"
echo "  ~/WellMonitor                                    (repo root)"
echo "  ~/WellMonitor/src/WellMonitor.Device             (project root)"  
echo "  ~/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/  (app base dir)"
echo "  ~/WellMonitor/src/WellMonitor.Device/debug_images          (desired debug dir)"
echo ""

# Show what relative paths would resolve to
BASE_DIR="/home/davidb/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0"
RELATIVE_PATH="debug_images"
ABSOLUTE_PATH="/home/davidb/WellMonitor/src/WellMonitor.Device/debug_images"

echo "Path Resolution:"
echo "  App Base Directory: $BASE_DIR"
echo "  Relative Path 'debug_images' would resolve to:"
echo "    $BASE_DIR/$RELATIVE_PATH"
echo "  Absolute Path (recommended):"  
echo "    $ABSOLUTE_PATH"
echo ""

# Check if directories exist
echo "Directory Status:"
PROJECT_DIR="/home/davidb/WellMonitor/src/WellMonitor.Device"
DESIRED_DEBUG_DIR="$PROJECT_DIR/debug_images"
CURRENT_RELATIVE_DIR="$BASE_DIR/$RELATIVE_PATH"

if [ -d "$PROJECT_DIR" ]; then
    echo "  ✅ Project directory exists: $PROJECT_DIR"
else
    echo "  ❌ Project directory missing: $PROJECT_DIR"
fi

if [ -d "$DESIRED_DEBUG_DIR" ]; then
    echo "  ✅ Desired debug directory exists: $DESIRED_DEBUG_DIR"
else
    echo "  ⚠️  Desired debug directory missing: $DESIRED_DEBUG_DIR"
    echo "     (Will be created automatically when needed)"
fi

if [ -d "$CURRENT_RELATIVE_DIR" ]; then
    echo "  ⚠️  Current relative path exists: $CURRENT_RELATIVE_DIR"
    echo "     (This may be where debug images are currently saved)"
    echo "     Files in this directory:"
    if [ "$(ls -A "$CURRENT_RELATIVE_DIR" 2>/dev/null)" ]; then
        ls -la "$CURRENT_RELATIVE_DIR" | grep -E "\.(jpg|png|jpeg)$" | head -5
    else
        echo "     (No image files found)"
    fi
else
    echo "  ℹ️  Current relative path doesn't exist: $CURRENT_RELATIVE_DIR"
fi

echo ""
echo "Recommendation:"
echo "  Run: ./scripts/update-debug-image-path.sh"
echo "  This will set cameraDebugImagePath to the absolute path:"
echo "  $ABSOLUTE_PATH"
