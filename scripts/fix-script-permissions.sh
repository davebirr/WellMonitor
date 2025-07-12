#!/bin/bash

# Fix Script Permissions
# This script ensures all .sh files have executable permissions
# Run this on the Pi after git pull if scripts aren't executable

echo "Fixing script permissions..."

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Make all .sh files in scripts directory executable
find "$SCRIPT_DIR" -name "*.sh" -exec chmod +x {} \;

echo "Script permissions fixed:"
ls -la "$SCRIPT_DIR"/*.sh

echo ""
echo "Scripts are now executable. You can run:"
echo "  ./scripts/diagnose-debug-image-path.sh"
echo "  ./scripts/update-debug-image-path.sh" 
echo "  ./scripts/deploy-to-pi.sh"
echo "  ./scripts/test-ocr.sh"
echo ""
echo "To automatically fix permissions after every git pull, add this to your .bashrc:"
echo "alias git-pull='git pull && ./scripts/fix-script-permissions.sh'"
