#!/bin/bash

# Tesseract OCR Installation Script for Raspberry Pi
# This script installs Tesseract OCR and required language data

echo "Installing Tesseract OCR on Raspberry Pi..."

# Update package lists
echo "Updating package lists..."
sudo apt update

# Install Tesseract OCR
echo "Installing Tesseract OCR..."
sudo apt install -y tesseract-ocr

# Install additional language data (English)
echo "Installing English language data..."
sudo apt install -y tesseract-ocr-eng

# Install additional tools
echo "Installing additional OCR tools..."
sudo apt install -y libtesseract-dev

# Verify installation
echo "Verifying Tesseract installation..."
tesseract --version

echo "Checking available languages..."
tesseract --list-langs

echo "Checking tessdata directory..."
ls -la /usr/share/tesseract-ocr/*/tessdata/ 2>/dev/null || ls -la /usr/share/tessdata/ 2>/dev/null

echo "Tesseract installation completed!"
echo ""
echo "Next steps:"
echo "1. Restart the WellMonitor service: sudo systemctl restart wellmonitor.service"
echo "2. Check the logs: sudo journalctl -u wellmonitor.service -f"
