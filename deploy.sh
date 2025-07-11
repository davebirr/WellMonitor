#!/bin/bash

# Deploy Well Monitor to Raspberry Pi
# Usage: ./deploy.sh

PI_HOST="davidb@rpi4b-1407well01"
PI_PATH="/home/davidb/WellMonitor"

echo "🚀 Deploying Well Monitor to Raspberry Pi..."

# Build the project
echo "📦 Building project..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

# Copy the entire project
echo "📁 Copying files to Raspberry Pi..."
rsync -av --exclude='.git' --exclude='bin' --exclude='obj' ./ $PI_HOST:$PI_PATH/

# Copy .env file if it exists
if [ -f ".env" ]; then
    echo "🔐 Copying environment file..."
    scp .env $PI_HOST:$PI_PATH/.env
else
    echo "⚠️  No .env file found. Copy .env.example to .env and update with your values."
fi

echo "✅ Deployment complete!"
echo ""
echo "To run on Raspberry Pi:"
echo "ssh $PI_HOST"
echo "cd $PI_PATH/src/WellMonitor.Device"
echo "dotnet run --configuration Release"
