#!/bin/bash

# Python OCR Setup Script for WellMonitor on ARM64 Raspberry Pi
# This script installs all required Python packages for OCR functionality

echo "WellMonitor Python OCR Setup for ARM64 Raspberry Pi"
echo "=================================================="

# Update system packages
echo "Updating system packages..."
sudo apt update && sudo apt upgrade -y

# Install Python and pip
echo "Installing Python3 and pip..."
sudo apt install -y python3 python3-pip python3-venv

# Install system dependencies for OpenCV and image processing
echo "Installing system dependencies..."
sudo apt install -y \
    tesseract-ocr \
    tesseract-ocr-eng \
    libtesseract-dev \
    libopencv-dev \
    python3-opencv \
    libhdf5-dev \
    libhdf5-serial-dev \
    libatlas-base-dev \
    libjasper-dev \
    libqtgui4 \
    libqt4-test \
    libgtk2.0-dev \
    pkg-config \
    libavcodec-dev \
    libavformat-dev \
    libswscale-dev \
    libjpeg-dev \
    libpng-dev \
    libtiff-dev \
    libdc1394-22-dev \
    libv4l-dev \
    libxvidcore-dev \
    libx264-dev \
    libgtk-3-dev \
    libatlas-base-dev \
    gfortran \
    python3-dev

# Install Python packages with specific versions known to work on ARM64
echo "Installing Python packages..."

# Install Pillow first (required by pytesseract)
pip3 install --user Pillow==10.0.1

# Install pytesseract
pip3 install --user pytesseract==0.3.10

# Install OpenCV for Python
pip3 install --user opencv-python==4.8.1.78

# Install NumPy (required by OpenCV)
pip3 install --user numpy==1.24.3

# Verify installations
echo ""
echo "Verifying installations..."

echo "Python version:"
python3 --version

echo ""
echo "Tesseract version:"
tesseract --version

echo ""
echo "Testing Python imports..."
python3 -c "
try:
    import pytesseract
    print('✓ pytesseract imported successfully')
    
    import PIL
    print('✓ PIL (Pillow) imported successfully')
    
    import cv2
    print('✓ OpenCV imported successfully')
    
    import numpy as np
    print('✓ NumPy imported successfully')
    
    # Test tesseract functionality
    version = pytesseract.get_tesseract_version()
    print(f'✓ Tesseract OCR functional, version: {version}')
    
    print('')
    print('🎉 All Python OCR dependencies installed successfully!')
    print('The WellMonitor application can now use Python OCR provider.')
    
except ImportError as e:
    print(f'❌ Import error: {e}')
    exit(1)
except Exception as e:
    print(f'❌ Error: {e}')
    exit(1)
"

echo ""
echo "Installation complete!"
echo ""
echo "Next steps:"
echo "1. Run your WellMonitor application"
echo "2. The Python OCR provider should now initialize successfully"
echo "3. Check the application logs for 'Python OCR provider initialized successfully'"
echo ""
echo "If you encounter issues:"
echo "- Ensure the user has permissions to execute python3"
echo "- Check that all packages are installed: pip3 list | grep -E 'opencv|pytesseract|Pillow|numpy'"
echo "- Verify Tesseract is accessible: which tesseract"
