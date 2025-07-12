#!/bin/bash
# WellMonitor Service Management Script
# Quick commands for managing the WellMonitor service

SERVICE_NAME="wellmonitor"

case "$1" in
    start)
        echo "🚀 Starting WellMonitor service..."
        sudo systemctl start $SERVICE_NAME
        sleep 2
        sudo systemctl status $SERVICE_NAME --no-pager
        ;;
    stop)
        echo "🛑 Stopping WellMonitor service..."
        sudo systemctl stop $SERVICE_NAME
        sudo systemctl status $SERVICE_NAME --no-pager
        ;;
    restart)
        echo "🔄 Restarting WellMonitor service..."
        sudo systemctl restart $SERVICE_NAME
        sleep 2
        sudo systemctl status $SERVICE_NAME --no-pager
        ;;
    status)
        echo "📊 WellMonitor service status:"
        sudo systemctl status $SERVICE_NAME --no-pager
        ;;
    logs)
        echo "📜 WellMonitor service logs (press Ctrl+C to exit):"
        sudo journalctl -u $SERVICE_NAME -f
        ;;
    recent)
        echo "📜 Recent WellMonitor service logs:"
        sudo journalctl -u $SERVICE_NAME --since "10 minutes ago" --no-pager
        ;;
    debug)
        echo "🐛 Debug information:"
        echo
        echo "Service Status:"
        sudo systemctl status $SERVICE_NAME --no-pager
        echo
        echo "Recent Logs:"
        sudo journalctl -u $SERVICE_NAME --since "5 minutes ago" --no-pager | tail -20
        echo
        echo "Debug Images:"
        ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/ | tail -10
        echo
        echo "Executable Check:"
        EXEC_PATH="~/WellMonitor/src/WellMonitor.Device/bin/Release/net8.0/linux-arm64/WellMonitor.Device"
        if [ -f "$EXEC_PATH" ]; then
            echo "✅ Executable found: $EXEC_PATH"
        else
            echo "❌ Executable not found: $EXEC_PATH"
            echo "Available executables:"
            find ~/WellMonitor/src/WellMonitor.Device/bin -name "WellMonitor.Device*" -type f 2>/dev/null || echo "None found"
        fi
        ;;
    images)
        echo "📸 Recent debug images:"
        ls -la ~/WellMonitor/src/WellMonitor.Device/debug_images/ | tail -10
        ;;
    *)
        echo "WellMonitor Service Management"
        echo "Usage: $0 {start|stop|restart|status|logs|recent|debug|images}"
        echo
        echo "Commands:"
        echo "  start   - Start the service"
        echo "  stop    - Stop the service"
        echo "  restart - Restart the service"
        echo "  status  - Show service status"
        echo "  logs    - Follow live logs (Ctrl+C to exit)"
        echo "  recent  - Show recent logs"
        echo "  debug   - Show debug information"
        echo "  images  - Show recent debug images"
        echo
        echo "Examples:"
        echo "  $0 restart        # Restart after code changes"
        echo "  $0 logs           # Monitor live activity"
        echo "  $0 debug          # Quick troubleshooting info"
        exit 1
        ;;
esac
