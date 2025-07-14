# Hardware Specifications

This document outlines the hardware requirements and specifications for the WellMonitor system.

## Raspberry Pi 4B Requirements

### Minimum Specifications
- **Model**: Raspberry Pi 4B
- **RAM**: 4GB (8GB recommended for development)
- **Storage**: 32GB microSD card (Class 10 or better)
- **Power**: Official Raspberry Pi 4 Power Supply (5V/3A USB-C)
- **Operating System**: Raspberry Pi OS (64-bit) or Ubuntu 22.04 LTS

### Recommended Configuration
- **Model**: Raspberry Pi 4B 8GB
- **Storage**: 64GB SanDisk Extreme microSD card
- **Case**: Official Raspberry Pi 4 Case with Fan
- **Power**: Uninterruptible Power Supply (UPS) for reliability

## Camera Module

### Supported Cameras
- **Primary**: Raspberry Pi Camera Module 3 (12MP)
- **Alternative**: Raspberry Pi Camera Module 2 (8MP)
- **USB Cameras**: Any USB camera with Video4Linux2 support

### Camera Specifications
```
Resolution: 1920x1080 (configurable)
Frame Rate: 30fps (not used in monitoring mode)
Focus: Fixed focus (manual adjustment recommended)
Field of View: Optimized for close-range LED display reading
Mount: Adjustable mount for precise positioning
```

### Camera Positioning
- **Distance**: 6-12 inches from LED display
- **Angle**: Perpendicular to display surface
- **Lighting**: Avoid direct sunlight or reflective surfaces
- **Stability**: Secure mounting to prevent movement

## GPIO Pin Configuration

### Relay Control (Default Configuration)
```
GPIO Pin 18 (BCM) - Relay Control Signal
GPIO Pin 16 (BCM) - Relay Status LED (optional)
GPIO Pin 20 (BCM) - System Status LED (optional)
GPIO Pin 21 (BCM) - Emergency Stop Button (optional)
```

### Power Requirements
- **Relay Module**: 5V DC, max 250mA
- **LED Indicators**: 3.3V DC, max 20mA each
- **Total GPIO Current**: <500mA (within Pi limits)

## Relay Module Specifications

### Recommended Relay Module
- **Model**: SainSmart 1-Channel 5V Relay Module
- **Switching Capacity**: 10A @ 250VAC / 10A @ 30VDC
- **Control Voltage**: 5V DC
- **Control Current**: 15-20mA
- **Isolation**: Optocoupler isolation
- **Protection**: Flyback diode included

### Electrical Safety
⚠️ **WARNING**: Relay controls high-voltage pump circuits
- Use appropriately rated relay for pump load
- Ensure proper electrical isolation
- Follow local electrical codes
- Consider professional electrical installation

### Relay Wiring
```
Relay Module Connections:
VCC  → Pi 5V (Pin 2)
GND  → Pi Ground (Pin 6)
IN1  → Pi GPIO 18 (Pin 12)

Pump Circuit:
COM  → Pump Power Input
NO   → Pump Power Source (normally open)
NC   → Not connected (normally closed - unused)
```

## Pump Interface

### Supported Pump Displays
- **7-Segment LED Display**: Red digits preferred for OCR
- **Display Size**: Minimum 0.5" digit height
- **Current Range**: 0.0 - 50.0 amps
- **Display Format**: "XX.X" (decimal current) or status text
- **Update Rate**: 1Hz or faster

### Status Display Messages
```
Normal Operation:
- "12.5" (current in amps)
- "8.1"  (low current)
- "15.2" (high current)

Error Conditions:
- "Dry"  (dry well condition)
- "rcyc" (rapid cycling)
- "Off"  (pump off)
- "----" (no reading)
```

## Network Requirements

### Wi-Fi Configuration
- **Standard**: 802.11n (2.4GHz) minimum
- **Security**: WPA2/WPA3 with strong password
- **Range**: Strong signal strength at installation location
- **Bandwidth**: 1 Mbps sufficient for telemetry
- **Reliability**: Stable connection for cloud sync

### Ethernet Option
- **Connection**: Wired Ethernet for maximum reliability
- **Speed**: 100 Mbps (Pi limitation)
- **Configuration**: Static IP recommended
- **Security**: Network firewall protection

## Environmental Specifications

### Operating Conditions
- **Temperature**: 0°C to 50°C (32°F to 122°F)
- **Humidity**: 5% to 85% non-condensing
- **Protection**: IP54 enclosure recommended
- **Ventilation**: Passive cooling sufficient

### Installation Environment
- **Location**: Near pump control panel
- **Protection**: Weather-resistant enclosure
- **Access**: Easy access for maintenance
- **Mounting**: Vibration-resistant mounting
- **Cable Management**: Secure cable routing

## Power Supply

### Primary Power
- **Input**: 100-240VAC, 50/60Hz
- **Output**: 5V DC, 3A minimum
- **Connector**: USB-C for Pi 4B
- **Efficiency**: 80%+ efficiency rating
- **Safety**: UL/CE certified

### Backup Power (Recommended)
- **UPS**: 12V battery backup system
- **Capacity**: 2-4 hour runtime minimum
- **Auto-switch**: Seamless power transition
- **Monitoring**: Low battery alerts
- **Maintenance**: Annual battery replacement

## Storage Requirements

### Local Storage
- **Database**: SQLite (50-100MB typical)
- **Debug Images**: 1-5GB (configurable retention)
- **Logs**: 100-500MB (rotating logs)
- **System**: 8GB minimum free space
- **Backup**: Weekly backup recommended

### Performance
- **Read Speed**: Class 10 microSD minimum
- **Write Endurance**: High-endurance card recommended
- **Monitoring**: Disk usage alerts at 80%
- **Lifecycle**: 2-3 year replacement cycle

## Assembly and Installation

### Physical Assembly
1. **Pi Preparation**
   - Install Raspberry Pi OS
   - Enable camera interface
   - Configure GPIO permissions

2. **Camera Mount**
   - Position for optimal LED viewing
   - Secure against vibration
   - Test image capture quality

3. **Relay Installation**
   - Mount relay module securely
   - Connect control wiring
   - Test relay operation

4. **Enclosure**
   - Weather-resistant case
   - Adequate ventilation
   - Cable entry sealing

### Testing Checklist
- [ ] Camera image quality
- [ ] OCR text recognition
- [ ] Relay control operation
- [ ] Network connectivity
- [ ] Azure IoT Hub connection
- [ ] Power supply stability
- [ ] Environmental protection

## Maintenance Schedule

### Monthly
- Check image quality
- Verify network connectivity
- Review system logs
- Test relay operation

### Quarterly
- Clean camera lens
- Check cable connections
- Update system software
- Backup configuration

### Annually
- Replace UPS battery
- Check enclosure seals
- Verify electrical connections
- Performance optimization

## Troubleshooting

### Common Hardware Issues

#### Camera Problems
```bash
# Check camera detection
vcgencmd get_camera

# Test camera capture
libcamera-still -o test.jpg

# Check camera permissions
ls -l /dev/video*
```

#### GPIO Issues
```bash
# Check GPIO permissions
groups $USER

# Test GPIO control
gpio -g write 18 1
gpio -g write 18 0
```

#### Network Connectivity
```bash
# Check Wi-Fi status
iwconfig wlan0

# Test internet connectivity
ping 8.8.8.8

# Check Azure IoT Hub connectivity
curl -I https://your-hub.azure-devices.net
```

## Safety Considerations

### Electrical Safety
- Turn off pump power during installation
- Use proper electrical safety equipment
- Follow local electrical codes
- Consider professional installation

### System Security
- Change default passwords
- Enable firewall
- Regular security updates
- Monitor access logs

### Environmental Protection
- Weatherproof enclosures
- Proper grounding
- Surge protection
- Temperature monitoring

## Compatibility Matrix

| Component | Minimum | Recommended | Notes |
|-----------|---------|-------------|--------|
| Pi Model | 4B 4GB | 4B 8GB | More RAM for development |
| OS Version | Bullseye | Bookworm | Latest stable release |
| .NET Runtime | 8.0 | 8.0 LTS | Long-term support |
| Camera | Module 2 | Module 3 | Better image quality |
| Storage | 32GB Class 10 | 64GB A2 | Application class |
| Power | 3A Official | 3A + UPS | Backup power |

## Vendor Information

### Recommended Suppliers
- **Raspberry Pi**: Official distributors
- **Cameras**: Adafruit, Pimoroni, Element14
- **Relays**: SainSmart, ELEGOO, HiLetgo
- **Enclosures**: Hammond, Polycase, Bud Industries
- **Power**: Official Pi supplies, Anker

### Part Numbers
```
Raspberry Pi 4B 8GB: SC0194
Pi Camera Module 3: SC1093
Official Power Supply: SC0218
SainSmart Relay: 101-70-103
```
