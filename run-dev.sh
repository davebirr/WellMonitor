#!/bin/bash
# Development runner script for Well Monitor Device

echo "Starting Well Monitor Device in Development Mode..."
echo "This will run with warnings instead of failing on missing configuration."

# Set development environment
export DOTNET_ENVIRONMENT=Development

# Navigate to the device project
cd src/WellMonitor.Device

# Run the application
dotnet run

echo "Well Monitor Device has stopped."
