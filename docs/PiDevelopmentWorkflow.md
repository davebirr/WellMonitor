# Raspberry Pi Development Workflow Guide

## 🔄 **Recommended Sync Methods**

### **Method 1: Full Sync & Run (Recommended for Testing)**
```bash
# Make script executable (first time only)
chmod +x scripts/sync-and-run.sh

# Standard sync, build, and run
./scripts/sync-and-run.sh

# Clean build and run
./scripts/sync-and-run.sh --clean

# Build only (don't run)
./scripts/sync-and-run.sh --no-run
```

### **Method 2: Quick Sync (Fast Development)**
```bash
# Make script executable (first time only)
chmod +x scripts/quick-sync.sh

# Quick pull and status
./scripts/quick-sync.sh

# Then manually run
cd src/WellMonitor.Device
dotnet run
```

### **Method 3: Manual Git Pull (Most Control)**
```bash
# Check status first
git status
git log --oneline -5

# Pull latest changes
git pull

# Build and run
dotnet build
cd src/WellMonitor.Device
dotnet run
```

## 📋 **Development Workflow Patterns**

### **Pattern 1: Rapid Iteration**
Best for quick code changes and testing:

```bash
# On development machine
git add -A
git commit -m "Fix OCR initialization issue"
git push

# On Pi
./scripts/quick-sync.sh
cd src/WellMonitor.Device
dotnet run
```

### **Pattern 2: Thorough Testing** 
Best for major changes or before deployment:

```bash
# On Pi
./scripts/sync-and-run.sh --clean
# Automatically builds and runs with clean state
```

### **Pattern 3: Debug & Iterate**
Best when troubleshooting issues:

```bash
# On Pi - build without running
./scripts/sync-and-run.sh --no-run

# Then run with different configurations
cd src/WellMonitor.Device
dotnet run --configuration Debug
# or
dotnet run --configuration Release
```

## 🛠️ **Pi-Specific Considerations**

### **Performance Optimization**
```bash
# Use Release build for better performance
dotnet run --configuration Release

# Pre-compile for faster startup
dotnet publish -c Release -o /home/davidb/wellmonitor-published
/home/davidb/wellmonitor-published/WellMonitor.Device
```

### **Service Mode (Production)**
```bash
# Install as systemd service
sudo cp scripts/wellmonitor.service /etc/systemd/system/
sudo systemctl enable wellmonitor
sudo systemctl start wellmonitor

# Check service status
sudo systemctl status wellmonitor
sudo journalctl -u wellmonitor -f
```

### **Development vs Production**
```bash
# Development (with console output)
dotnet run

# Production (as service)
sudo systemctl restart wellmonitor
```

## 📊 **Monitoring Sync Status**

### **Check for Updates**
```bash
# Check if Pi is behind
git fetch
git status

# See what's new
git log --oneline HEAD..origin/main

# Show differences
git diff HEAD..origin/main
```

### **Verify Successful Sync**
```bash
# Check last commit matches repository
git log -1 --oneline

# Verify application runs
cd src/WellMonitor.Device
dotnet build --dry-run
```

## 🔧 **Troubleshooting Sync Issues**

### **Common Problems & Solutions**

**1. Merge Conflicts**
```bash
# Reset to remote state (loses local changes)
git fetch origin
git reset --hard origin/main

# Or stash local changes first
git stash
git pull
git stash pop  # Apply stashed changes if needed
```

**2. Permission Issues**
```bash
# Fix script permissions
chmod +x scripts/*.sh

# Fix .NET permissions
sudo chown -R davidb:davidb /home/davidb/WellMonitor
```

**3. Build Failures After Sync**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# Or use clean sync
./scripts/sync-and-run.sh --clean
```

**4. Outdated Packages**
```bash
# Update packages
dotnet restore --force

# Clear NuGet cache if needed
dotnet nuget locals all --clear
dotnet restore
```

## 📁 **File Organization on Pi**

```
/home/davidb/
├── WellMonitor/              # Main repository
│   ├── src/WellMonitor.Device/  # Application code
│   ├── scripts/              # Sync and utility scripts
│   └── docs/                 # Documentation
├── wellmonitor-published/    # Compiled release (optional)
└── wellmonitor-data/         # Application data (logs, db)
```

## ⚡ **Quick Commands Reference**

```bash
# Essential Pi commands
./scripts/sync-and-run.sh           # Full sync, build, run
./scripts/quick-sync.sh             # Quick sync only
git pull && dotnet run              # Manual sync and run
sudo systemctl restart wellmonitor # Restart service
sudo journalctl -u wellmonitor -f  # View live logs

# Development helpers
git log --oneline -10               # Recent commits
git status                          # Current state
dotnet build --verbosity minimal   # Quick build check
ps aux | grep dotnet               # Check if running
```

## 💡 **Best Practices**

1. **Always sync before major testing**: Use `sync-and-run.sh --clean`
2. **Check for conflicts before pulling**: Run `git status` first
3. **Use Release builds for performance**: Add `--configuration Release`
4. **Monitor system resources**: `htop` during development
5. **Keep backups of working configs**: Git tags for stable versions
6. **Test after major updates**: Full application workflow verification

## 🎯 **Recommended Workflow**

For most development scenarios, this workflow is optimal:

```bash
# 1. Quick sync and check
./scripts/quick-sync.sh

# 2. If major changes, do clean build
./scripts/sync-and-run.sh --clean

# 3. For rapid iteration during debugging
git pull && cd src/WellMonitor.Device && dotnet run

# 4. When ready for production testing
sudo systemctl restart wellmonitor
sudo journalctl -u wellmonitor -f
```

This approach gives you the flexibility to choose the right sync method based on what you're testing, while maintaining good development velocity.
