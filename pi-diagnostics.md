# Raspberry Pi Service Diagnostics

Run these commands on your Raspberry Pi to diagnose the service issue:

## 1. Run the diagnostic script
```bash
cd ~/WellMonitor
chmod +x scripts/diagnose-service.sh
./scripts/diagnose-service.sh
```

## 2. Check detailed service logs
```bash
# Get the last 50 lines of service logs
sudo journalctl -u wellmonitor -n 50 --no-pager

# Get real-time logs (press Ctrl+C to exit)
sudo journalctl -u wellmonitor -f
```

## 3. Try manual execution to see exact error
```bash
cd ~/WellMonitor/src/WellMonitor.Device
export DOTNET_EnableDiagnostics=0
/usr/bin/dotnet bin/Release/net8.0/linux-arm64/WellMonitor.Device
```

## 4. Check dependencies
```bash
# Check if dotnet runtime is available
dotnet --list-runtimes

# Check if the executable exists and is accessible
ls -la bin/Release/net8.0/linux-arm64/WellMonitor.Device

# Check file permissions
ls -la bin/Release/net8.0/linux-arm64/
```

## 5. Check configuration files
```bash
# Check if secrets.json exists
ls -la secrets.json

# Check if appsettings.json exists  
ls -la appsettings.json
```

## 6. Alternative: Try running as current user (not as service)
```bash
cd ~/WellMonitor/src/WellMonitor.Device
dotnet run --configuration Release
```

Run these commands and let me know what output you get, especially from the diagnostic script and the manual execution.
