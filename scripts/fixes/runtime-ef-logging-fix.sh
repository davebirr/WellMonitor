#!/bin/bash

echo "🔧 Runtime Entity Framework Logging Suppression"
echo "==============================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${YELLOW}This script applies immediate Entity Framework logging suppression${NC}"
echo -e "${YELLOW}without requiring a rebuild or redeploy.${NC}"
echo ""

# Stop the service first
echo -e "${BLUE}🛑 Stopping WellMonitor service...${NC}"
sudo systemctl stop wellmonitor

# Navigate to the application directory
cd /opt/wellmonitor

# Create a comprehensive appsettings.Production.json that forces EF logging off
echo -e "${BLUE}🔧 Creating comprehensive production logging configuration...${NC}"
sudo tee appsettings.Production.json > /dev/null << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "None",
      "Microsoft.EntityFrameworkCore.Database": "None",
      "Microsoft.EntityFrameworkCore.Database.Command": "None",
      "Microsoft.EntityFrameworkCore.Database.Transaction": "None",
      "Microsoft.EntityFrameworkCore.Database.Connection": "None",
      "Microsoft.EntityFrameworkCore.Infrastructure": "None",
      "Microsoft.EntityFrameworkCore.Query": "None",
      "Microsoft.EntityFrameworkCore.Update": "None",
      "Microsoft.EntityFrameworkCore.ChangeTracking": "None"
    },
    "Console": {
      "LogLevel": {
        "Microsoft.EntityFrameworkCore": "None",
        "Microsoft.EntityFrameworkCore.Database": "None",
        "Microsoft.EntityFrameworkCore.Database.Command": "None"
      }
    },
    "SystemdConsole": {
      "LogLevel": {
        "Microsoft.EntityFrameworkCore": "None",
        "Microsoft.EntityFrameworkCore.Database": "None",
        "Microsoft.EntityFrameworkCore.Database.Command": "None"
      }
    }
  }
}
EOF

# Also update the main appsettings.json to ensure EF logging is suppressed
echo -e "${BLUE}🔧 Backing up and updating main appsettings.json...${NC}"
sudo cp appsettings.json appsettings.json.backup.$(date +%Y%m%d_%H%M%S)

# Update the main appsettings.json with EF suppression
sudo tee appsettings.json > /dev/null << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=wellmonitor.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "None",
      "Microsoft.EntityFrameworkCore.Database": "None",
      "Microsoft.EntityFrameworkCore.Database.Command": "None",
      "Microsoft.EntityFrameworkCore.Database.Transaction": "None",
      "Microsoft.EntityFrameworkCore.Database.Connection": "None",
      "Microsoft.EntityFrameworkCore.Infrastructure": "None",
      "Microsoft.EntityFrameworkCore.Query": "None"
    }
  },
  "WellMonitor": {
    "MonitoringIntervalSeconds": 30,
    "TelemetryIntervalMinutes": 5,
    "SyncIntervalHours": 1,
    "DataRetentionDays": 30,
    "RelayDebounceMs": 500
  },
  "KeyVault": {
    "Uri": ""
  },
  "SecretsMode": "hybrid",
  "OCR": {
    "Provider": "Tesseract",
    "MinimumConfidence": 0.7,
    "MaxRetryAttempts": 3,
    "TimeoutSeconds": 30,
    "EnablePreprocessing": true,
    "Tesseract": {
      "Language": "eng",
      "EngineMode": 3,
      "PageSegmentationMode": 7,
      "CustomConfig": {
        "tessedit_char_whitelist": "0123456789.DryAMPSrcyc ",
        "tessedit_unrej_any_wd": "1"
      }
    },
    "AzureCognitiveServices": {
      "Endpoint": "",
      "Region": "eastus",
      "UseReadApi": true,
      "MaxPollingAttempts": 10,
      "PollingIntervalMs": 500
    },
    "ImagePreprocessing": {
      "EnableGrayscale": true,
      "EnableContrastEnhancement": true,
      "ContrastFactor": 1.5,
      "EnableBrightnessAdjustment": true,
      "BrightnessAdjustment": 10,
      "EnableNoiseReduction": true,
      "EnableEdgeEnhancement": false,
      "EnableScaling": true,
      "ScaleFactor": 2.0,
      "EnableBinaryThresholding": true,
      "BinaryThreshold": 128
    }
  },
  "Debug": {
    "DebugMode": false,
    "ImageSaveEnabled": false,
    "ImageRetentionDays": 7,
    "LogLevel": "Information",
    "EnableVerboseOcrLogging": false
  }
}
EOF

echo -e "${GREEN}✅ Comprehensive Entity Framework logging suppression applied${NC}"

# Verify environment settings
echo -e "${BLUE}🔧 Verifying environment configuration...${NC}"
if ! grep -q "ASPNETCORE_ENVIRONMENT=Production" /etc/wellmonitor/environment; then
    echo "ASPNETCORE_ENVIRONMENT=Production" | sudo tee -a /etc/wellmonitor/environment
    echo -e "${GREEN}✅ Production environment set${NC}"
else
    echo -e "${GREEN}✅ Production environment already configured${NC}"
fi

# Start the service
echo -e "${BLUE}🚀 Starting WellMonitor service...${NC}"
sudo systemctl start wellmonitor

# Wait for service to start
echo -e "${BLUE}⏳ Waiting for service to start...${NC}"
sleep 10

# Check service status
echo ""
echo -e "${BLUE}📊 Service Status:${NC}"
sudo systemctl status wellmonitor --no-pager -l | head -15

# Test for SQL logging suppression over 30 seconds
echo ""
echo -e "${BLUE}🔍 Testing Entity Framework logging suppression (30 seconds)...${NC}"
sleep 30

sql_count=$(sudo journalctl -u wellmonitor --since "25 seconds ago" | grep -E "SELECT|INSERT|Executed DbCommand|FROM.*Readings|EntityFrameworkCore" | wc -l)
echo -e "${BLUE}📊 SQL/EF queries in last 25 seconds: ${sql_count}${NC}"

if [ "$sql_count" -eq 0 ]; then
    echo -e "${GREEN}🎉 SUCCESS: Entity Framework SQL logging completely suppressed!${NC}"
elif [ "$sql_count" -lt 3 ]; then
    echo -e "${YELLOW}⚠️  Minimal SQL logging (${sql_count} entries) - significant improvement${NC}"
else
    echo -e "${RED}❌ SQL logging still present (${sql_count} entries)${NC}"
    echo -e "${YELLOW}💡 This may require a code-level fix and redeploy${NC}"
fi

echo ""
echo -e "${BLUE}📝 Recent application logs (last 20, non-SQL):${NC}"
sudo journalctl -u wellmonitor --since "1 minute ago" | grep -v -E "SELECT|INSERT|Executed DbCommand|FROM.*Readings|WHERE.*TimestampUtc|EntityFrameworkCore" | tail -10

echo ""
echo -e "${YELLOW}📋 Monitor clean logs with: journalctl -u wellmonitor -f | grep -v 'SELECT\\|INSERT\\|FROM'${NC}"
echo -e "${GREEN}✅ Runtime Entity Framework logging suppression complete${NC}"
