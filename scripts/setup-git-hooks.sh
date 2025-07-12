#!/bin/bash

# Setup Git Hooks for Automatic Script Permission Fixing
# Run this once on the Pi to automatically fix script permissions after git operations

echo "Setting up git hooks for automatic script permission fixing..."

# Create post-checkout hook
cat > .git/hooks/post-checkout << 'EOF'
#!/bin/bash
# Auto-fix script permissions after checkout
if [ "$3" == "1" ]; then
    echo "Auto-fixing script permissions..."
    find scripts -name "*.sh" -exec chmod +x {} \; 2>/dev/null || true
    echo "Script permissions updated."
fi
EOF

# Create post-merge hook  
cat > .git/hooks/post-merge << 'EOF'
#!/bin/bash
# Auto-fix script permissions after merge/pull
echo "Auto-fixing script permissions after merge..."
find scripts -name "*.sh" -exec chmod +x {} \; 2>/dev/null || true
echo "Script permissions updated."
EOF

# Make hooks executable
chmod +x .git/hooks/post-checkout
chmod +x .git/hooks/post-merge

# Fix current script permissions
find scripts -name "*.sh" -exec chmod +x {} \;

echo "Git hooks installed successfully!"
echo "Script permissions will now be automatically fixed after git pull, checkout, etc."
echo ""
echo "Current script permissions:"
ls -la scripts/*.sh
