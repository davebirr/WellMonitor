#!/bin/bash
# Check if systemd service can see environment variables

echo "🔍 Checking Environment Variables for WellMonitor Service"
echo "========================================================"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

print_section() {
    echo ""
    print_status $CYAN "📋 $1"
    echo "----------------------------------------"
}

print_section "1. User Environment Variables"
print_status $BLUE "Environment variables in current user session:"
printenv | grep WELLMONITOR | while read line; do
    echo "  $line"
done

if [[ -z "$(printenv | grep WELLMONITOR)" ]]; then
    print_status $RED "❌ No WELLMONITOR environment variables found in user session"
else
    print_status $GREEN "✅ WELLMONITOR environment variables found in user session"
fi

print_section "2. Service Environment Variables"
print_status $BLUE "Checking if wellmonitor service can see environment variables..."

# Check systemd service environment
if systemctl is-active --quiet wellmonitor; then
    SERVICE_PID=$(systemctl show wellmonitor --property=MainPID --value)
    
    if [[ "$SERVICE_PID" != "0" ]] && [[ -n "$SERVICE_PID" ]]; then
        print_status $GREEN "✅ Service is running with PID: $SERVICE_PID"
        
        # Try to read service environment (requires root)
        if [[ $EUID -eq 0 ]]; then
            print_status $BLUE "Reading service environment variables..."
            
            if [[ -f "/proc/$SERVICE_PID/environ" ]]; then
                SERVICE_ENV=$(cat /proc/$SERVICE_PID/environ 2>/dev/null | tr '\0' '\n' | grep WELLMONITOR)
                
                if [[ -n "$SERVICE_ENV" ]]; then
                    print_status $GREEN "✅ Service can see WELLMONITOR environment variables:"
                    echo "$SERVICE_ENV" | while read line; do
                        if [[ "$line" == *"CONNECTION_STRING"* ]]; then
                            # Mask the connection string for security
                            VAR_NAME=$(echo "$line" | cut -d'=' -f1)
                            VAR_VALUE=$(echo "$line" | cut -d'=' -f2-)
                            MASKED_VALUE="${VAR_VALUE:0:30}...${VAR_VALUE: -10}"
                            echo "  $VAR_NAME=$MASKED_VALUE"
                        else
                            echo "  $line"
                        fi
                    done
                else
                    print_status $RED "❌ Service cannot see WELLMONITOR environment variables"
                fi
            else
                print_status $YELLOW "⚠️  Cannot read service environment (insufficient permissions)"
            fi
        else
            print_status $YELLOW "⚠️  Run with sudo to check service environment variables"
        fi
    else
        print_status $RED "❌ Cannot get service PID"
    fi
else
    print_status $RED "❌ WellMonitor service is not running"
fi

print_section "3. Systemd Service Configuration"
SERVICE_FILE="/etc/systemd/system/wellmonitor.service"

if [[ -f "$SERVICE_FILE" ]]; then
    print_status $GREEN "✅ Service file exists: $SERVICE_FILE"
    
    # Check if environment variables are defined in service file
    ENV_VARS=$(grep -i "Environment.*WELLMONITOR" "$SERVICE_FILE" 2>/dev/null)
    
    if [[ -n "$ENV_VARS" ]]; then
        print_status $GREEN "✅ Environment variables defined in service file:"
        echo "$ENV_VARS" | while read line; do
            if [[ "$line" == *"CONNECTION_STRING"* ]]; then
                # Mask connection string
                echo "  $(echo "$line" | sed 's/=.*azure-devices.net.*/=HostName=***masked***/')"
            else
                echo "  $line"
            fi
        done
    else
        print_status $YELLOW "⚠️  No WELLMONITOR environment variables in service file"
        print_status $BLUE "💡 Variables should be inherited from user environment or system environment"
    fi
    
    # Check for EnvironmentFile directive
    ENV_FILE=$(grep -i "EnvironmentFile" "$SERVICE_FILE" 2>/dev/null)
    if [[ -n "$ENV_FILE" ]]; then
        print_status $BLUE "📁 Environment file configured: $ENV_FILE"
    fi
else
    print_status $RED "❌ Service file not found: $SERVICE_FILE"
fi

print_section "4. System Environment Configuration"

# Check /etc/environment
if [[ -f "/etc/environment" ]]; then
    SYSTEM_ENV=$(grep WELLMONITOR /etc/environment 2>/dev/null)
    if [[ -n "$SYSTEM_ENV" ]]; then
        print_status $GREEN "✅ WELLMONITOR variables found in /etc/environment"
        echo "$SYSTEM_ENV" | while read line; do
            if [[ "$line" == *"CONNECTION_STRING"* ]]; then
                echo "  $(echo "$line" | sed 's/=.*azure-devices.net.*/=HostName=***masked***/')"
            else
                echo "  $line"
            fi
        done
    else
        print_status $YELLOW "⚠️  No WELLMONITOR variables in /etc/environment"
    fi
else
    print_status $YELLOW "⚠️  /etc/environment file not found"
fi

print_section "5. Recommendations"

print_status $BLUE "💡 To ensure the service can see environment variables:"
echo ""
echo "Option 1: Add to systemd service file (recommended):"
echo "  sudo systemctl edit wellmonitor"
echo "  Add:"
echo "  [Service]"
echo "  Environment=WELLMONITOR_SECRETS_MODE=environment"
echo "  Environment=WELLMONITOR_IOTHUB_CONNECTION_STRING=HostName=..."
echo ""
echo "Option 2: Add to /etc/environment (system-wide):"
echo "  sudo nano /etc/environment"
echo "  Add:"
echo "  WELLMONITOR_SECRETS_MODE=environment"
echo "  WELLMONITOR_IOTHUB_CONNECTION_STRING=HostName=..."
echo ""
echo "Option 3: Use EnvironmentFile in service:"
echo "  Create /etc/wellmonitor/environment"
echo "  Add EnvironmentFile=/etc/wellmonitor/environment to service"
echo ""

if systemctl is-active --quiet wellmonitor; then
    print_status $BLUE "🔄 After making changes, restart the service:"
    echo "  sudo systemctl daemon-reload"
    echo "  sudo systemctl restart wellmonitor"
fi
