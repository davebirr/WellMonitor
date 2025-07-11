@echo off
REM Deploy Well Monitor to Raspberry Pi
REM Usage: deploy.bat

set PI_HOST=davidb@rpi4b-1407well01
set PI_PATH=/home/davidb/WellMonitor

echo 🚀 Deploying Well Monitor to Raspberry Pi...

REM Build the project
echo 📦 Building project...
dotnet build --configuration Release

if %errorlevel% neq 0 (
    echo ❌ Build failed
    exit /b 1
)

REM Copy the entire project (excluding .git, bin, obj)
echo 📁 Copying files to Raspberry Pi...
scp -r * %PI_HOST%:%PI_PATH%/

REM Copy .env file if it exists
if exist ".env" (
    echo 🔐 Copying environment file...
    scp .env %PI_HOST%:%PI_PATH%/.env
) else (
    echo ⚠️  No .env file found. Copy .env.example to .env and update with your values.
)

echo ✅ Deployment complete!
echo.
echo To run on Raspberry Pi:
echo ssh %PI_HOST%
echo cd %PI_PATH%/src/WellMonitor.Device
echo dotnet run --configuration Release

pause
