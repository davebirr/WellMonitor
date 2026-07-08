#!/bin/bash

echo "🔍 SQL Logging Noise Diagnostic"
echo "==============================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}Analyzing SQL logging patterns in wellmonitor service...${NC}"
echo ""

# Check current environment
echo -e "${BLUE}📋 Current Environment Configuration:${NC}"
echo "ASPNETCORE_ENVIRONMENT: $(grep ASPNETCORE_ENVIRONMENT /etc/wellmonitor/environment 2>/dev/null || echo 'Not set')"

# Check logging configuration files
echo ""
echo -e "${BLUE}📋 Logging Configuration Files:${NC}"
if [ -f /opt/wellmonitor/appsettings.json ]; then
    echo "✅ appsettings.json exists"
    grep -A 10 "EntityFrameworkCore" /opt/wellmonitor/appsettings.json 2>/dev/null || echo "  No EF config found"
else
    echo "❌ appsettings.json not found"
fi

if [ -f /opt/wellmonitor/appsettings.Production.json ]; then
    echo "✅ appsettings.Production.json exists"
    grep -A 10 "EntityFrameworkCore" /opt/wellmonitor/appsettings.Production.json 2>/dev/null || echo "  No EF config found"
else
    echo "❌ appsettings.Production.json not found"
fi

# Count SQL queries in recent logs
echo ""
echo -e "${BLUE}🔍 SQL Query Analysis (last 2 minutes):${NC}"
total_sql=$(sudo journalctl -u wellmonitor --since "2 minutes ago" | grep -E "SELECT|INSERT|UPDATE|DELETE|Executed DbCommand" | wc -l)
readings_queries=$(sudo journalctl -u wellmonitor --since "2 minutes ago" | grep -E "FROM.*Readings|SELECT.*r\." | wc -l)
insert_queries=$(sudo journalctl -u wellmonitor --since "2 minutes ago" | grep -E "INSERT INTO" | wc -l)

echo "Total SQL queries: $total_sql"
echo "Readings queries: $readings_queries"
echo "Insert queries: $insert_queries"

if [ "$total_sql" -gt 20 ]; then
    echo -e "${RED}❌ EXCESSIVE SQL LOGGING (${total_sql} queries in 2 minutes)${NC}"
elif [ "$total_sql" -gt 5 ]; then
    echo -e "${YELLOW}⚠️  Moderate SQL logging (${total_sql} queries)${NC}"
else
    echo -e "${GREEN}✅ Minimal SQL logging (${total_sql} queries)${NC}"
fi

# Show query patterns
echo ""
echo -e "${BLUE}📊 Most Common Query Patterns:${NC}"
sudo journalctl -u wellmonitor --since "5 minutes ago" | grep -E "FROM.*Readings" | head -3

# Check service activity
echo ""
echo -e "${BLUE}🔍 Service Activity Analysis:${NC}"
web_activity=$(sudo journalctl -u wellmonitor --since "2 minutes ago" | grep -i -E "web|dashboard|signalr|hub" | wc -l)
telemetry_activity=$(sudo journalctl -u wellmonitor --since "2 minutes ago" | grep -i -E "telemetry|sync" | wc -l)
monitoring_activity=$(sudo journalctl -u wellmonitor --since "2 minutes ago" | grep -i -E "monitoring|capture|camera" | wc -l)

echo "Web/Dashboard activity: $web_activity"
echo "Telemetry/Sync activity: $telemetry_activity"  
echo "Monitoring/Camera activity: $monitoring_activity"

# Check query frequency
echo ""
echo -e "${BLUE}⏱️  Query Frequency (every 5 seconds suggests web dashboard):${NC}"
sudo journalctl -u wellmonitor --since "30 seconds ago" | grep -E "Executed DbCommand.*SELECT" | while read line; do
    echo "$line" | grep -o "Jul [0-9]* [0-9]*:[0-9]*:[0-9]*"
done | tail -5

echo ""
echo -e "${YELLOW}💡 Solutions:${NC}"
echo "1. Quick fix: ./scripts/fixes/fix-sql-logging-noise.sh"
echo "2. Set Production environment: Add ASPNETCORE_ENVIRONMENT=Production to /etc/wellmonitor/environment"
echo "3. Redeploy with latest code that has Entity Framework logging suppression"

echo ""
echo -e "${BLUE}📋 Monitor real-time: journalctl -u wellmonitor -f | grep -v 'SELECT\|INSERT\|FROM'${NC}"
