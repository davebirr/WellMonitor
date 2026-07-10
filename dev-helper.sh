#!/bin/bash
# WellMonitor Development Helper Script
# Manages the systemd service and local development

SERVICE_NAME="wellmonitor"
PROJECT_DIR="/home/davidb/WellMonitor"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}🔍 WellMonitor Service Status:${NC}"
    sudo systemctl status $SERVICE_NAME --no-pager -l
}

stop_service() {
    echo -e "${YELLOW}🛑 Stopping WellMonitor service...${NC}"
    sudo systemctl stop $SERVICE_NAME
    sudo systemctl disable $SERVICE_NAME
    echo -e "${GREEN}✅ Service stopped and disabled${NC}"
}

start_service() {
    echo -e "${YELLOW}🚀 Starting WellMonitor service...${NC}"
    sudo systemctl enable $SERVICE_NAME
    sudo systemctl start $SERVICE_NAME
    echo -e "${GREEN}✅ Service started and enabled${NC}"
    print_status
}

run_local() {
    echo -e "${BLUE}🔍 Checking if service is running...${NC}"
    if systemctl is-active --quiet $SERVICE_NAME; then
        echo -e "${YELLOW}⚠️  WellMonitor service is running. Stopping it first...${NC}"
        stop_service
        echo ""
    else
        echo -e "${GREEN}✅ Service is not running${NC}"
    fi
    
    echo -e "${BLUE}🏃 Running WellMonitor locally...${NC}"
    cd "$PROJECT_DIR"
    
    # Source environment variables
    if [ -f ".env" ]; then
        echo -e "${GREEN}📖 Loading environment variables from .env${NC}"
        set -a  # automatically export all variables
        source .env
        set +a  # stop automatically exporting
    else
        echo -e "${RED}❌ .env file not found!${NC}"
        exit 1
    fi
    
    dotnet run --project src/WellMonitor.Device/WellMonitor.Device.csproj
}

show_logs() {
    echo -e "${BLUE}📋 WellMonitor Service Logs (last 50 lines):${NC}"
    sudo journalctl -u $SERVICE_NAME --no-pager -n 50
}

show_help() {
    echo -e "${BLUE}WellMonitor Development Helper${NC}"
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  status    - Show service status"
    echo "  stop      - Stop and disable the service"
    echo "  start     - Start and enable the service"
    echo "  dev       - Stop service and run locally for development"
    echo "  logs      - Show recent service logs"
    echo "  help      - Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 dev      # Stop service and run locally"
    echo "  $0 status   # Check service status"
    echo "  $0 logs     # View recent logs"
}

# Main script logic
case "${1:-help}" in
    "status")
        print_status
        ;;
    "stop")
        stop_service
        ;;
    "start")
        start_service
        ;;
    "dev")
        run_local
        ;;
    "logs")
        show_logs
        ;;
    "help"|*)
        show_help
        ;;
esac
