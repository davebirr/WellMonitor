# Quick Fix for Dependency Injection Issue

## 🚨 Problem
The PumpStatusAnalyzer service cannot be created because `AlertOptions` and other options classes are not registered in the dependency injection container.

## ✅ Solution
Add the missing service registrations to Program.cs.

## 🛠️ Manual Fix Steps

1. **Open Program.cs on your Pi** (around line 50):
```bash
nano /home/davidb/WellMonitor/src/WellMonitor.Device/Program.cs
```

2. **Find this section**:
```csharp
// Register options pattern for services
services.AddSingleton(gpioOptions);
services.AddSingleton(cameraOptions);
```

3. **Replace it with**:
```csharp
// Register options pattern for services
services.AddSingleton(gpioOptions);
services.AddSingleton(cameraOptions);

// Register additional options classes
services.AddSingleton(new AlertOptions());
services.AddSingleton(new MonitoringOptions());
services.AddSingleton(new ImageQualityOptions());
services.AddSingleton(new DebugOptions());
services.AddSingleton(new PumpAnalysisOptions());
services.AddSingleton(new PowerManagementOptions());
services.AddSingleton(new StatusDetectionOptions());
```

4. **Build and run**:
```bash
cd /home/davidb/WellMonitor/src/WellMonitor.Device
dotnet build
dotnet run
```

## 🔧 Alternative: Service File Fix

If you're running as a systemd service, update the service:

```bash
# Build the project
cd /home/davidb/WellMonitor/src/WellMonitor.Device
dotnet publish -c Release -o /home/davidb/wellmonitor-app

# Restart the service
sudo systemctl restart wellmonitor
sudo systemctl status wellmonitor
```

## 📋 Verification

After the fix, you should see:
```
info: WellMonitor.Device.Services.MonitoringBackgroundService[0]
      Monitoring background service started
```

Instead of the dependency injection error.

## 🎯 What This Fixes

The error occurs because:
1. `PumpStatusAnalyzer` constructor requires `AlertOptions`
2. `AlertOptions` was not registered in the DI container
3. When the hosting service tries to create `PumpStatusAnalyzer`, it fails

The fix registers all the options classes that the enhanced services need, allowing the dependency injection to work correctly.

## 📞 If You Need Help

Run this command to see the exact error:
```bash
cd /home/davidb/WellMonitor/src/WellMonitor.Device
dotnet run 2>&1 | head -20
```

This will show the first 20 lines of output, including any remaining dependency injection issues.
