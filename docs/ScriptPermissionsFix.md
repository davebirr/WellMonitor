# Script Permissions Fix for Raspberry Pi

## Problem
When you `git pull` on the Raspberry Pi, shell scripts may not have executable permissions, causing "Permission denied" errors.

## Quick Fix
Run this command after any `git pull`:
```bash
./scripts/fix-script-permissions.sh
```

## Automatic Fix Options

### Option 1: Git Hooks (Recommended)
Copy the provided git hooks to automatically fix permissions:
```bash
# On the Pi, run once to set up auto-fix:
cp scripts/setup-git-hooks.sh .git/hooks/
chmod +x .git/hooks/setup-git-hooks.sh
./.git/hooks/setup-git-hooks.sh
```

### Option 2: Bash Alias
Add this to your `~/.bashrc` on the Pi:
```bash
alias git-pull='git pull && ./scripts/fix-script-permissions.sh'
```
Then use `git-pull` instead of `git pull`.

### Option 3: Manual Fix
If scripts aren't executable, manually fix with:
```bash
chmod +x scripts/*.sh
```

## Why This Happens
- Git on Windows doesn't track Unix file permissions properly
- The `core.filemode=true` setting helps but isn't perfect across platforms
- Linux/Pi requires executable bit for shell scripts

## Verification
Check if scripts are executable:
```bash
ls -la scripts/*.sh
```
Should show `-rwxr-xr-x` (with `x` for executable) not `-rw-r--r--`.

## Available Scripts
Once executable, you can run:
- `./scripts/diagnose-debug-image-path.sh` - Debug image path issues
- `./scripts/update-debug-image-path.sh` - Fix debug image configuration
- `./scripts/deploy-to-pi.sh` - Deploy and run on Pi
- `./scripts/test-ocr.sh` - Test OCR functionality
- `./scripts/fix-script-permissions.sh` - Fix script permissions
