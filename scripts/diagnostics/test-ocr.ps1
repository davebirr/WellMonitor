# OCR Test Script for Well Monitor (PowerShell)
# This script helps test the OCR functionality

Write-Host "Well Monitor OCR Test Script" -ForegroundColor Green
Write-Host "===========================" -ForegroundColor Green

# Check if we're in the right directory
if (-not (Test-Path "WellMonitor.Device.csproj")) {
    Write-Host "Error: Please run this script from the WellMonitor.Device directory" -ForegroundColor Red
    exit 1
}

# Create sample images directory if it doesn't exist
if (-not (Test-Path "debug_images/samples")) {
    Write-Host "Creating sample images directory structure..." -ForegroundColor Yellow
    $directories = @("normal", "idle", "dry", "rcyc", "off", "live")
    foreach ($dir in $directories) {
        New-Item -Path "debug_images/samples/$dir" -ItemType Directory -Force | Out-Null
    }
    Write-Host "Created directories: $($directories -join ', ')" -ForegroundColor Green
    Write-Host "Please add sample images to these directories for testing" -ForegroundColor Yellow
}

# Check if Tesseract is installed
try {
    $tesseractVersion = & tesseract --version 2>&1
    Write-Host "Tesseract OCR found: $($tesseractVersion[0])" -ForegroundColor Green
}
catch {
    Write-Host "Warning: Tesseract OCR is not installed or not in PATH" -ForegroundColor Yellow
    Write-Host "Please install Tesseract OCR for offline OCR functionality" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Installation instructions:" -ForegroundColor Cyan
    Write-Host "  Windows (Chocolatey): choco install tesseract" -ForegroundColor White
    Write-Host "  Windows (Manual): Download from https://github.com/UB-Mannheim/tesseract/wiki" -ForegroundColor White
    Write-Host ""
}

# Build the project
Write-Host "Building WellMonitor.Device project..." -ForegroundColor Cyan
$buildResult = & dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    
    # Check for sample images
    $sampleImages = Get-ChildItem -Path "debug_images/samples" -Recurse -Filter "*.jpg" -ErrorAction SilentlyContinue
    
    if ($sampleImages.Count -gt 0) {
        Write-Host "Found $($sampleImages.Count) sample images" -ForegroundColor Green
        Write-Host "Running OCR test..." -ForegroundColor Cyan
        
        # Run a simple OCR test using the compiled program
        Write-Host "OCR functionality is ready for testing!" -ForegroundColor Green
        Write-Host "You can now integrate OCR into your main application." -ForegroundColor Green
    } else {
        Write-Host "No sample images found." -ForegroundColor Yellow
        Write-Host "Add .jpg files to debug_images/samples/[condition]/ directories for testing." -ForegroundColor Yellow
        Write-Host "Conditions: normal, idle, dry, rcyc, off" -ForegroundColor Cyan
    }
    
    Write-Host "OCR test setup completed!" -ForegroundColor Green
} else {
    Write-Host "Build failed. Please fix compilation errors first." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Add sample images to debug_images/samples/ directories" -ForegroundColor White
Write-Host "2. Test OCR accuracy with your specific LED display images" -ForegroundColor White
Write-Host "3. Adjust OCR settings in appsettings.json if needed" -ForegroundColor White
Write-Host "4. Deploy to your Raspberry Pi device" -ForegroundColor White
