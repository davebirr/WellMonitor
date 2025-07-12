# Azure Integration Guide

Complete setup and configuration guide for Azure IoT Hub, storage, and cloud services integration.

## Azure IoT Hub Setup

### Creating IoT Hub Resource

1. **Create IoT Hub in Azure Portal**
   ```bash
   # Using Azure CLI
   az iot hub create --name your-hub-name --resource-group your-rg --sku S1 --location eastus
   ```

2. **Register Device**
   ```bash
   # Create device identity
   az iot hub device-identity create --device-id your-device-id --hub-name your-hub-name
   
   # Get connection string
   az iot hub device-identity connection-string show --device-id your-device-id --hub-name your-hub-name
   ```

3. **Configure Device Connection**
   ```bash
   # Set connection string on device
   sudo nano /etc/wellmonitor/environment
   
   # Add:
   WELLMONITOR_IOTHUB_CONNECTION_STRING="HostName=your-hub.azure-devices.net;DeviceId=your-device;SharedAccessKey=your-key"
   ```

### Device Twin Configuration

The device twin enables remote configuration and monitoring:

**Desired Properties (Configuration)**
```json
{
  "properties": {
    "desired": {
      "monitoringIntervalSeconds": 30,
      "telemetryIntervalMinutes": 5,
      "cameraGain": 12.0,
      "ocrProvider": "Tesseract",
      "debugMode": false
    }
  }
}
```

**Reported Properties (Status)**
```json
{
  "properties": {
    "reported": {
      "firmwareVersion": "1.0.0",
      "lastSyncTimeUtc": "2025-07-12T10:30:00Z",
      "status": "Normal",
      "lastReading": 4.2,
      "ocrConfidence": 0.95,
      "cameraStatus": "OK"
    }
  }
}
```

### Managing Device Twin

**Using Azure CLI:**
```bash
# View current device twin
az iot hub device-twin show --device-id your-device-id --hub-name your-hub-name

# Update configuration
az iot hub device-twin update --device-id your-device-id --hub-name your-hub-name \
  --set properties.desired.monitoringIntervalSeconds=60

# Monitor device twin changes
az iot hub monitor-events --device-id your-device-id --hub-name your-hub-name
```

**Using PowerShell Scripts:**
```powershell
# Set up Azure CLI (first time)
.\scripts\Setup-AzureCli.ps1

# Update LED camera settings
.\scripts\Update-LedCameraSettings.ps1

# Test configuration
.\scripts\Test-LedCameraOptimization.ps1
```

## Telemetry and Messaging

### Telemetry Format

The device sends structured telemetry messages:

```json
{
  "deviceId": "well-pump-001",
  "timestamp": "2025-07-12T10:30:00Z",
  "currentDraw": 4.2,
  "status": "Normal",
  "ocrConfidence": 0.95,
  "powerConsumption": {
    "currentHour": 1.2,
    "currentDay": 12.5,
    "currentMonth": 385.2
  },
  "systemHealth": {
    "cameraStatus": "OK",
    "ocrProvider": "Tesseract",
    "databaseSize": "15MB",
    "lastSyncTime": "2025-07-12T10:25:00Z"
  }
}
```

### Message Routing

Configure IoT Hub message routing to process telemetry:

```json
{
  "routes": [
    {
      "name": "TelemetryToStorage",
      "source": "DeviceMessages",
      "condition": "true",
      "endpointNames": ["storage-endpoint"]
    },
    {
      "name": "AlertsToEventHub",
      "source": "DeviceMessages", 
      "condition": "status = 'Dry' OR status = 'rcyc'",
      "endpointNames": ["alerts-endpoint"]
    }
  ]
}
```

### Direct Methods

The device responds to direct method calls for remote control:

**Power Cycle Command:**
```json
{
  "methodName": "powerCycle",
  "payload": {
    "delaySeconds": 5,
    "reason": "Manual cycle from PowerApp"
  }
}
```

**Configuration Update:**
```json
{
  "methodName": "updateConfig", 
  "payload": {
    "monitoringInterval": 60,
    "debugMode": true
  }
}
```

**Status Query:**
```json
{
  "methodName": "getStatus",
  "payload": {}
}
```

## Azure Storage Integration

### Storage Account Setup

```bash
# Create storage account
az storage account create --name your-storage-name --resource-group your-rg --location eastus --sku Standard_LRS

# Get connection string
az storage account show-connection-string --name your-storage-name --resource-group your-rg
```

### Configure Storage Connection

```bash
# Add to device environment
sudo nano /etc/wellmonitor/environment

# Add storage connection string
WELLMONITOR_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
```

### Storage Usage

**Telemetry Backup:**
- Long-term telemetry storage
- Historical data analysis
- Compliance and auditing

**Debug Image Storage:**
- Camera debug images for troubleshooting
- OCR training data collection
- Remote diagnostics

**Configuration Backup:**
- Device twin configuration history
- Deployment artifacts
- Service configuration files

## Azure Cognitive Services (OCR)

### Creating Cognitive Services Resource

```bash
# Create Computer Vision resource
az cognitiveservices account create \
  --name your-ocr-service \
  --resource-group your-rg \
  --kind ComputerVision \
  --sku S1 \
  --location eastus

# Get API key
az cognitiveservices account keys list \
  --name your-ocr-service \
  --resource-group your-rg
```

### Configure OCR Service

```bash
# Add OCR configuration to device
sudo nano /etc/wellmonitor/environment

# Add OCR service details
WELLMONITOR_OCR_API_KEY="your-cognitive-services-key"
WELLMONITOR_OCR_ENDPOINT="https://your-region.api.cognitive.microsoft.com/"
```

### OCR Provider Configuration

Enable Azure OCR in device twin:

```json
{
  "ocrProvider": "AzureCognitive",
  "ocrAzureEndpoint": "https://your-region.api.cognitive.microsoft.com/",
  "ocrAzureRegion": "eastus",
  "ocrMinimumConfidence": 0.8
}
```

## PowerApp Integration

### Azure Functions Backend

Create Azure Functions to bridge PowerApp and IoT Hub:

**Function: GetDeviceStatus**
```csharp
[FunctionName("GetDeviceStatus")]
public static async Task<IActionResult> GetDeviceStatus(
    [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
    ILogger log)
{
    // Query device twin for current status
    // Return formatted status for PowerApp
}
```

**Function: PowerCycleDevice**
```csharp
[FunctionName("PowerCycleDevice")]
public static async Task<IActionResult> PowerCycleDevice(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
    ILogger log)
{
    // Send direct method to device
    // Return operation status
}
```

### PowerApp Configuration

**Data Sources:**
- Azure Functions (device control)
- Azure SQL/Cosmos DB (historical data)
- SharePoint (user management)

**Key Screens:**
1. **Dashboard**: Current pump status, power consumption
2. **Historical**: Charts and trends
3. **Control**: Manual power cycle, settings
4. **Alerts**: Active alerts and notifications

### Authentication and Security

**Service Principal for PowerApp:**
```bash
# Create service principal
az ad sp create-for-rbac --name "PowerApp-WellMonitor" --role "IoT Hub Data Contributor"

# Grant permissions to IoT Hub
az role assignment create \
  --assignee <service-principal-id> \
  --role "IoT Hub Data Contributor" \
  --scope /subscriptions/<subscription-id>/resourceGroups/<rg>/providers/Microsoft.Devices/IotHubs/<hub-name>
```

**API Key Management:**
```bash
# Generate PowerApp API key
WELLMONITOR_POWERAPP_API_KEY="your-secure-api-key"

# Add to device environment
echo "WELLMONITOR_POWERAPP_API_KEY=your-secure-api-key" | sudo tee -a /etc/wellmonitor/environment
```

## Monitoring and Alerting

### Azure Monitor Integration

**Log Analytics Workspace:**
```bash
# Create Log Analytics workspace
az monitor log-analytics workspace create \
  --resource-group your-rg \
  --workspace-name wellmonitor-logs \
  --location eastus
```

**Diagnostic Settings:**
```bash
# Enable IoT Hub diagnostics
az monitor diagnostic-settings create \
  --name "IoTHubDiagnostics" \
  --resource /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.Devices/IotHubs/<hub> \
  --workspace /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.OperationalInsights/workspaces/wellmonitor-logs \
  --logs '[{"category":"Connections","enabled":true},{"category":"DeviceTelemetry","enabled":true}]'
```

### Alert Rules

**Device Offline Alert:**
```json
{
  "condition": {
    "allOf": [
      {
        "field": "category",
        "equals": "DeviceTelemetry"
      },
      {
        "field": "timeGenerated",
        "greaterThan": "PT15M"
      }
    ]
  },
  "actions": [
    {
      "actionType": "Email",
      "emailSubject": "Well Monitor Device Offline",
      "emailTo": ["admin@company.com"]
    }
  ]
}
```

**Dry Condition Alert:**
```json
{
  "condition": {
    "allOf": [
      {
        "field": "properties.status",
        "equals": "Dry"
      }
    ]
  },
  "actions": [
    {
      "actionType": "SMS",
      "phoneNumber": "+1234567890"
    },
    {
      "actionType": "PowerAutomate",
      "flowId": "your-flow-id"
    }
  ]
}
```

## Security Best Practices

### Connection String Management
- ✅ Store in environment variables, not in code
- ✅ Use managed identities where possible
- ✅ Rotate keys regularly
- ✅ Use separate keys for different environments

### Device Authentication
- ✅ Use individual device certificates
- ✅ Enable device twin access control
- ✅ Monitor connection patterns for anomalies
- ✅ Implement device attestation

### Network Security
- ✅ Use TLS for all communications
- ✅ Implement VPN for device management
- ✅ Configure firewall rules
- ✅ Monitor network traffic

### Data Protection
- ✅ Encrypt data at rest and in transit
- ✅ Implement data retention policies
- ✅ Use Azure Private Link for storage
- ✅ Regular security assessments

## Troubleshooting

### Connection Issues

```bash
# Test IoT Hub connectivity
az iot hub monitor-events --device-id your-device-id --hub-name your-hub-name

# Check device logs
sudo journalctl -u wellmonitor | grep -i "iothub\|azure\|connection"

# Verify connection string
sudo grep IOTHUB /etc/wellmonitor/environment
```

### Telemetry Issues

```bash
# Monitor telemetry transmission
sudo journalctl -u wellmonitor | grep -i "telemetry\|send\|publish"

# Check IoT Hub metrics in Azure Portal
# - Device-to-cloud messages
# - Connected devices
# - Error rates
```

### Device Twin Issues

```bash
# Check device twin synchronization
az iot hub device-twin show --device-id your-device-id --hub-name your-hub-name

# Monitor device twin updates
sudo journalctl -u wellmonitor | grep -i "device.*twin\|configuration"
```

For more troubleshooting steps, see [Troubleshooting Guide](../deployment/troubleshooting-guide.md).
For device configuration details, see [Configuration Guide](configuration-guide.md).
