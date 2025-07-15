#!/bin/bash

echo "=== WellMonitor Camera Test ==="
echo ""

# Test 1: Simple libcamera-still test
echo "🧪 Test 1: Testing libcamera-still basic capture..."
if timeout 10s libcamera-still --output /tmp/test_libcamera.jpg --width 640 --height 480 --timeout 2000 --nopreview --encoding jpg >/dev/null 2>&1; then
    if [ -f /tmp/test_libcamera.jpg ] && [ -s /tmp/test_libcamera.jpg ]; then
        echo "✅ libcamera-still basic test PASSED"
        rm -f /tmp/test_libcamera.jpg
    else
        echo "❌ libcamera-still created empty/no file"
    fi
else
    echo "❌ libcamera-still basic test FAILED"
fi

echo ""

# Test 2: rpicam-still test
echo "🧪 Test 2: Testing rpicam-still basic capture..."
if timeout 10s rpicam-still --output /tmp/test_rpicam.jpg --width 640 --height 480 --timeout 2000 --nopreview >/dev/null 2>&1; then
    if [ -f /tmp/test_rpicam.jpg ] && [ -s /tmp/test_rpicam.jpg ]; then
        echo "✅ rpicam-still basic test PASSED"
        rm -f /tmp/test_rpicam.jpg
    else
        echo "❌ rpicam-still created empty/no file"
    fi
else
    echo "❌ rpicam-still basic test FAILED"
fi

echo ""

# Test 3: Test with WellMonitor default settings
echo "🧪 Test 3: Testing with WellMonitor default settings..."
if timeout 15s libcamera-still --output /tmp/test_wellmonitor.jpg --width 1920 --height 1080 --quality 85 --timeout 2000 --encoding jpg --nopreview >/dev/null 2>&1; then
    if [ -f /tmp/test_wellmonitor.jpg ] && [ -s /tmp/test_wellmonitor.jpg ]; then
        echo "✅ WellMonitor settings test PASSED"
        rm -f /tmp/test_wellmonitor.jpg
    else
        echo "❌ WellMonitor settings created empty/no file"
    fi
else
    echo "❌ WellMonitor settings test FAILED"
fi

echo ""

# Test 4: Test problematic exposure settings
echo "🧪 Test 4: Testing potentially problematic exposure settings..."
if timeout 10s libcamera-still --output /tmp/test_exposure.jpg --width 640 --height 480 --timeout 2000 --exposure off --nopreview --encoding jpg >/dev/null 2>&1; then
    if [ -f /tmp/test_exposure.jpg ] && [ -s /tmp/test_exposure.jpg ]; then
        echo "✅ Exposure 'off' setting works"
        rm -f /tmp/test_exposure.jpg
    else
        echo "⚠️  Exposure 'off' creates empty/no file"
    fi
else
    echo "❌ Exposure 'off' setting FAILED (this might be the problem!)"
fi

echo ""
echo "🔍 Camera diagnostic complete. If Test 4 failed, that's likely our camera issue."
echo "The fix should use fallback camera commands and avoid problematic exposure settings."
