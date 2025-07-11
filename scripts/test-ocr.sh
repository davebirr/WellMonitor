#!/bin/bash

# OCR Test Script for Well Monitor
# This script helps test the OCR functionality

echo "Well Monitor OCR Test Script"
echo "==========================="

# Check if we're in the right directory
if [ ! -f "WellMonitor.Device.csproj" ]; then
    echo "Error: Please run this script from the WellMonitor.Device directory"
    exit 1
fi

# Create sample images directory if it doesn't exist
if [ ! -d "debug_images/samples" ]; then
    echo "Creating sample images directory structure..."
    mkdir -p debug_images/samples/{normal,idle,dry,rcyc,off,live}
    echo "Created directories: normal, idle, dry, rcyc, off, live"
    echo "Please add sample images to these directories for testing"
fi

# Check if Tesseract is installed
if ! command -v tesseract &> /dev/null; then
    echo "Warning: Tesseract OCR is not installed or not in PATH"
    echo "Please install Tesseract OCR for offline OCR functionality"
    echo ""
    echo "Installation instructions:"
    echo "  Ubuntu/Debian: sudo apt-get install tesseract-ocr tesseract-ocr-eng"
    echo "  macOS: brew install tesseract"
    echo "  Windows: choco install tesseract"
    echo ""
fi

# Build the project
echo "Building WellMonitor.Device project..."
dotnet build

if [ $? -eq 0 ]; then
    echo "Build successful!"
    
    # Run OCR test
    echo "Running OCR test..."
    dotnet run --project . -- --ocr-test
    
    echo "OCR test completed!"
else
    echo "Build failed. Please fix compilation errors first."
    exit 1
fi
