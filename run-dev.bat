@echo off
REM Development runner script for Well Monitor Device

echo Starting Well Monitor Device in Development Mode...
echo This will run with warnings instead of failing on missing configuration.

REM Set development environment
set DOTNET_ENVIRONMENT=Development

REM Navigate to the device project
cd src\WellMonitor.Device

REM Run the application
dotnet run

echo Well Monitor Device has stopped.
pause
