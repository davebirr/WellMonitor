#!/bin/bash
# Quick check of what user the service runs as
echo "Checking wellmonitor service user configuration:"
if [[ -f "/etc/systemd/system/wellmonitor.service" ]]; then
    echo "Service file exists, checking User= setting:"
    grep -i "user=" /etc/systemd/system/wellmonitor.service || echo "No User= setting found (runs as root)"
    echo ""
    echo "Checking Group= setting:"
    grep -i "group=" /etc/systemd/system/wellmonitor.service || echo "No Group= setting found"
    echo ""
    echo "Current process user when running:"
    if systemctl is-active --quiet wellmonitor; then
        PID=$(systemctl show wellmonitor --property=MainPID --value)
        if [[ "$PID" != "0" ]]; then
            echo "Service PID: $PID"
            echo "Process user: $(ps -o user= -p $PID)"
            echo "Process group: $(ps -o group= -p $PID)"
        fi
    else
        echo "Service not currently running"
    fi
else
    echo "Service file not found"
fi
