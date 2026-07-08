#!/bin/bash

echo "🔧 Quick Entity Framework Logging Fix"
echo "===================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${YELLOW}This script immediately suppresses Entity Framework SQL logging noise${NC}"
echo ""

# Stop the service
echo -e "${BLUE}🛑 Stopping WellMonitor service...${NC}"
sudo systemctl stop wellmonitor

# Navigate to the application directory
cd /opt/wellmonitor

# Create appsettings.Production.json to override EF logging
echo -e "${BLUE}🔧 Creating production logging configuration...${NC}"
sudo tee appsettings.Production.json > /dev/null << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "None",
      "Microsoft.EntityFrameworkCore.Database.Command": "None",
      "Microsoft.EntityFrameworkCore.Database.Transaction": "None",
      "Microsoft.EntityFrameworkCore.Database.Connection": "Error",
      "Microsoft.EntityFrameworkCore.Infrastructure": "Error",
      "Microsoft.EntityFrameworkCore.Query": "None"
    }
  }
}
EOF

echo -e "${GREEN}✅ Production logging configuration created${NC}"

# Ensure production environment is set
echo -e "${BLUE}🔧 Setting ASPNETCORE_ENVIRONMENT to Production...${NC}"
if ! grep -q "ASPNETCORE_ENVIRONMENT=Production" /etc/wellmonitor/environment; then
    echo "ASPNETCORE_ENVIRONMENT=Production" | sudo tee -a /etc/wellmonitor/environment
    echo -e "${GREEN}✅ Production environment set${NC}"
else
    echo -e "${GREEN}✅ Production environment already set${NC}"
fi

# Show current environment file
echo -e "${BLUE}📋 Current environment configuration:${NC}"
grep "ASPNETCORE_ENVIRONMENT" /etc/wellmonitor/environment || echo "Not found"

# Start the service
echo -e "${BLUE}🚀 Starting WellMonitor service...${NC}"
sudo systemctl start wellmonitor

# Wait for service to start
echo -e "${BLUE}⏳ Waiting for service to start...${NC}"
sleep 8

# Check service status
echo ""
echo -e "${BLUE}📊 Service Status:${NC}"
sudo systemctl status wellmonitor --no-pager -l | head -15

# Test for SQL logging noise
echo ""
echo -e "${BLUE}🔍 Testing Entity Framework logging (checking next 20 seconds)...${NC}"
sleep 20

sql_count=$(sudo journalctl -u wellmonitor --since "15 seconds ago" | grep -E "SELECT|INSERT|Executed DbCommand|FROM.*Readings" | wc -l)
echo -e "${BLUE}📊 SQL queries in last 15 seconds: ${sql_count}${NC}"

if [ "$sql_count" -eq 0 ]; then
    echo -e "${GREEN}🎉 SUCCESS: Entity Framework SQL logging suppressed!${NC}"
elif [ "$sql_count" -lt 3 ]; then
    echo -e "${YELLOW}⚠️  Minimal SQL logging (${sql_count} entries) - significant improvement${NC}"
else
    echo -e "${RED}❌ SQL logging still present (${sql_count} entries)${NC}"
    echo -e "${YELLOW}💡 May need to rebuild and redeploy with updated appsettings.json${NC}"
fi

echo ""
echo -e "${BLUE}📝 Recent application logs (non-SQL):${NC}"
sudo journalctl -u wellmonitor --since "30 seconds ago" | grep -v -E "SELECT|INSERT|Executed DbCommand|FROM.*Readings|WHERE.*TimestampUtc" | tail -8

echo ""
echo -e "${YELLOW}📋 Monitor logs with: journalctl -u wellmonitor -f${NC}"
echo -e "${YELLOW}🔍 If SQL logging persists, redeploy with updated source code${NC}"
