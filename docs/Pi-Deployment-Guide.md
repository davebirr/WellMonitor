# Raspberry Pi Deployment Guide

This guide covers the recommended repository-based deployment workflow for the WellMonitor.Device application.

## Repository-Based Deployment (Recommended)

### Prerequisites on Raspberry Pi
```bash
# Ensure git is installed and repository is cloned
sudo apt update
sudo apt install git

# Clone repository if not already done
cd /home/pi
git clone https://github.com/davebirr/WellMonitor.git
cd WellMonitor
```

### Deployment Steps

1. **On Development Machine**: Commit and push changes
   ```powershell
   git add .
   git commit -m "Your changes description"
   git push
   ```

2. **On Raspberry Pi**: Pull latest changes
   ```bash
   cd /home/pi/WellMonitor
   git pull origin main
   ```

3. **Build and Deploy** (if needed):
   ```bash
   # Stop the service first
   sudo systemctl stop wellmonitor.service
   
   # Build the application
   cd src/WellMonitor.Device
   dotnet build --configuration Release
   
   # Start the service
   sudo systemctl start wellmonitor.service
   
   # Check status
   sudo systemctl status wellmonitor.service
   ```

### Alternative: Quick Deployment Script

If you need to build and deploy in one step, use the provided script:

```bash
# Make script executable
chmod +x scripts/deploy-to-pi.sh

# Run deployment
./scripts/deploy-to-pi.sh
```

## File Structure After Deployment

```
/home/pi/WellMonitor/
├── src/WellMonitor.Device/
│   ├── bin/Release/net8.0/          # Built application
│   ├── debug_images/                # OCR debug images (created at runtime)
│   ├── WellMonitor.Device.csproj
│   └── Program.cs
├── docs/                            # Documentation
├── scripts/                         # Deployment scripts
└── .gitignore                       # Excludes build artifacts
```

## Configuration Management

### Device Twin Configuration
- All 42 configuration parameters are managed via Azure IoT Hub device twin
- No manual configuration files needed on the Pi
- Changes take effect without service restart

### Secrets Management
- Create `/home/pi/.wellmonitor-secrets.json` with Azure IoT Hub connection string
- This file is excluded from git via .gitignore
- See `docs/Pi-Dependency-Fix.md` for secret file format

## Troubleshooting

### Common Issues

1. **Service won't start**: Check dependency injection registration
   - See `docs/Pi-Dependency-Fix.md` for solution

2. **OCR not working**: Verify Tesseract installation
   ```bash
   sudo apt install tesseract-ocr
   tesseract --version
   ```

3. **Camera access denied**: Add user to video group
   ```bash
   sudo usermod -a -G video pi
   ```

4. **GPIO permission issues**: 
   ```bash
   sudo usermod -a -G gpio pi
   # Reboot required
   ```

### Service Management

```bash
# Check service status
sudo systemctl status wellmonitor.service

# View logs
sudo journalctl -u wellmonitor.service -f

# Restart service
sudo systemctl restart wellmonitor.service
```

## Best Practices

1. **Always use git workflow**: Commit → Push → Pull → Deploy
2. **Test locally first**: Build and test on development machine
3. **Monitor logs**: Check service logs after deployment
4. **Keep secrets separate**: Never commit connection strings or API keys
5. **Use relative paths**: All debug and log paths are relative for portability

## Build Artifacts

The following directories are excluded from git and created at runtime:
- `bin/` and `obj/` - Build outputs
- `publish/` - Deployment artifacts  
- `debug_images/*.jpg` - OCR debug images
- `*.log` - Log files
- `*.db`, `*.sqlite` - Database files

This ensures clean repository sync without polluting git history with build artifacts.
