#!/bin/bash

# Well Monitor Pi Deployment Script
# This script builds and deploys the WellMonitor.Device application to Raspberry Pi

set -e  # Exit on any error

echo "🚀 Well Monitor Pi Deployment Starting..."

# Configuration
PI_USER="davidb"
PI_HOST="rpi4b-1407well01"
PI_PATH="/home/davidb/WellMonitor"
PROJECT_PATH="src/WellMonitor.Device"
SERVICE_NAME="wellmonitor"

echo "📦 Building project..."
cd "$PROJECT_PATH"
dotnet publish -c Release -o ./publish --self-contained false

echo "🔄 Stopping service on Pi..."
ssh "${PI_USER}@${PI_HOST}" "sudo systemctl stop ${SERVICE_NAME} || true"

echo "📤 Deploying files to Pi..."
rsync -avz --delete ./publish/ "${PI_USER}@${PI_HOST}:${PI_PATH}/"

echo "🔧 Setting permissions..."
ssh "${PI_USER}@${PI_HOST}" "chmod +x ${PI_PATH}/WellMonitor.Device"

echo "🔄 Starting service on Pi..."
ssh "${PI_USER}@${PI_HOST}" "sudo systemctl start ${SERVICE_NAME}"

echo "📊 Checking service status..."
ssh "${PI_USER}@${PI_HOST}" "sudo systemctl status ${SERVICE_NAME} --no-pager"

echo "📋 Recent logs..."
ssh "${PI_USER}@${PI_HOST}" "sudo journalctl -u ${SERVICE_NAME} -n 20 --no-pager"

echo "✅ Deployment complete!"
echo ""
echo "🔍 To monitor logs: ssh ${PI_USER}@${PI_HOST} 'sudo journalctl -u ${SERVICE_NAME} -f'"
echo "🛠️  To check status: ssh ${PI_USER}@${PI_HOST} 'sudo systemctl status ${SERVICE_NAME}'"
