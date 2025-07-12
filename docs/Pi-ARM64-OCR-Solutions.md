# ARM64 Raspberry Pi OCR Solutions

The .NET Tesseract wrapper has native library compatibility issues on ARM64 Raspberry Pi. Here are several solutions to get OCR working properly.

## Quick Test: Verify Current Fix

First, test if the fallback mechanism works:

```bash
cd ~/WellMonitor
git pull origin main
cd src/WellMonitor.Device
dotnet run
```

**Expected Output**: The application should now start successfully and show:
```
info: OCR provider initialization completed: 0 successful, 2 failed
warn: No OCR providers were successfully initialized - continuing with limited functionality
info: Hardware initialization completed successfully
```

## Solution 1: Python OCR Bridge (Recommended)

Since Python Tesseract works reliably on ARM64, create a Python bridge service.

### Install Python Tesseract

```bash
# Install Python and Tesseract
sudo apt update
sudo apt install python3 python3-pip tesseract-ocr tesseract-ocr-eng

# Install Python OCR libraries
pip3 install pytesseract pillow flask
```

### Create Python OCR Service

Create `/home/pi/WellMonitor/python-ocr-service.py`:

```python
#!/usr/bin/env python3
import base64
import io
from flask import Flask, request, jsonify
from PIL import Image
import pytesseract
import time
import logging

app = Flask(__name__)
logging.basicConfig(level=logging.INFO)

@app.route('/ocr', methods=['POST'])
def extract_text():
    try:
        # Get image data from request
        data = request.get_json()
        if not data or 'image' not in data:
            return jsonify({'error': 'No image data provided'}), 400
        
        # Decode base64 image
        image_data = base64.b64decode(data['image'])
        image = Image.open(io.BytesIO(image_data))
        
        # OCR processing
        start_time = time.time()
        text = pytesseract.image_to_string(image, config='--psm 8 -c tessedit_char_whitelist=0123456789.')
        processing_time = int((time.time() - start_time) * 1000)
        
        # Get confidence (simplified)
        confidence = 0.9 if text.strip() else 0.0
        
        return jsonify({
            'success': True,
            'rawText': text.strip(),
            'processedText': text.strip(),
            'confidence': confidence,
            'provider': 'PythonTesseract',
            'processingDurationMs': processing_time
        })
        
    except Exception as e:
        logging.error(f"OCR processing failed: {str(e)}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500

@app.route('/health', methods=['GET'])
def health_check():
    return jsonify({'status': 'healthy'})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8080)
```

### Create System Service

Create `/etc/systemd/system/python-ocr.service`:

```ini
[Unit]
Description=Python OCR Service for WellMonitor
After=network.target

[Service]
Type=simple
User=pi
WorkingDirectory=/home/pi/WellMonitor
ExecStart=/usr/bin/python3 /home/pi/WellMonitor/python-ocr-service.py
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

Enable and start the service:

```bash
sudo systemctl enable python-ocr.service
sudo systemctl start python-ocr.service
sudo systemctl status python-ocr.service
```

## Solution 2: Create HTTP OCR Provider

Add an HTTP-based OCR provider to the .NET application.

Create `src/WellMonitor.Device/Services/HttpOcrProvider.cs`:

```csharp
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using WellMonitor.Device.Models;

namespace WellMonitor.Device.Services;

public class HttpOcrProvider : IOcrProvider, IDisposable
{
    private readonly ILogger<HttpOcrProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public string Name => "HttpOcr";
    public bool IsAvailable { get; private set; }

    public HttpOcrProvider(ILogger<HttpOcrProvider> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _baseUrl = "http://localhost:8080";
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/health", cancellationToken);
            IsAvailable = response.IsSuccessStatusCode;
            
            if (IsAvailable)
            {
                _logger.LogInformation("HTTP OCR provider initialized successfully");
            }
            else
            {
                _logger.LogWarning("HTTP OCR service not available");
            }
            
            return IsAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize HTTP OCR provider");
            IsAvailable = false;
            return false;
        }
    }

    public async Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert image to base64
            var imageBytes = await ReadStreamToByteArrayAsync(imageStream, cancellationToken);
            var base64Image = Convert.ToBase64String(imageBytes);

            // Prepare request
            var requestData = new { image = base64Image };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Make OCR request
            var response = await _httpClient.PostAsync($"{_baseUrl}/ocr", content, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<OcrResult>(responseJson);
                return result ?? CreateErrorResult("Failed to parse OCR response");
            }
            else
            {
                return CreateErrorResult($"HTTP OCR service error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP OCR processing failed");
            return CreateErrorResult(ex.Message);
        }
    }

    private async Task<byte[]> ReadStreamToByteArrayAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    private OcrResult CreateErrorResult(string errorMessage)
    {
        return new OcrResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Provider = Name,
            ProcessedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
```

Register it in `Program.cs`:

```csharp
services.AddSingleton<IOcrProvider, HttpOcrProvider>();
```

## Solution 3: Alternative .NET OCR Libraries

Try OCR libraries with better ARM64 support:

### Option A: Windows.Media.Ocr (if available)
```bash
dotnet add package Microsoft.Windows.SDK.Contracts
```

### Option B: IronOcr (commercial)
```bash
dotnet add package IronOcr
```

### Option C: Azure Computer Vision (cloud-based)
Already implemented as `AzureCognitiveServicesOcrProvider`.

## Testing OCR Solutions

### Test Python OCR Service

```bash
# Test the Python service directly
curl -X POST http://localhost:8080/health

# Test with base64 image (create test script)
python3 -c "
import base64
import requests
import json

# Read an image file and convert to base64
with open('test_image.jpg', 'rb') as f:
    image_data = base64.b64encode(f.read()).decode('utf-8')

response = requests.post('http://localhost:8080/ocr', 
                        json={'image': image_data})
print(json.dumps(response.json(), indent=2))
"
```

### Test .NET Application

```bash
cd ~/WellMonitor/src/WellMonitor.Device
dotnet run
```

Look for log messages indicating which OCR provider is being used.

## Device Twin Configuration

Configure OCR provider priority via device twin:

```json
{
  "properties": {
    "desired": {
      "ocrProvider": "HttpOcr",
      "ocrFallbackEnabled": true,
      "ocrHttpEndpoint": "http://localhost:8080"
    }
  }
}
```

## Troubleshooting

### Python Service Issues

```bash
# Check Python service logs
sudo journalctl -u python-ocr.service -f

# Test Python Tesseract directly
python3 -c "import pytesseract; print(pytesseract.get_tesseract_version())"

# Check if service is listening
netstat -tlnp | grep 8080
```

### .NET Application Issues

```bash
# Check if HTTP OCR provider initializes
grep "HTTP OCR" /var/log/wellmonitor.log

# Test network connectivity
curl http://localhost:8080/health
```

## Performance Comparison

| Provider | ARM64 Support | Speed | Accuracy | Maintenance |
|----------|---------------|-------|----------|-------------|
| .NET Tesseract | ❌ | Fast | High | Low |
| Python Tesseract | ✅ | Medium | High | Medium |
| HTTP Bridge | ✅ | Medium | High | Medium |
| Azure Cognitive | ✅ | Slow | Very High | Low |
| NullProvider | ✅ | Instant | None | None |

## Recommended Approach

1. **Short term**: Use the NullOcrProvider fallback to get the application running
2. **Medium term**: Implement Python OCR bridge service for reliable OCR
3. **Long term**: Wait for .NET Tesseract ARM64 support or switch to alternative library

The application will now start successfully and capture images. OCR functionality can be added incrementally without breaking the core monitoring system.
