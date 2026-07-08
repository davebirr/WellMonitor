#!/bin/bash
# update-camera-exposure-mode.sh
# Updates camera exposure mode configuration in Azure IoT Hub device twin

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to display usage
show_usage() {
    echo "Usage: $0 -d DEVICE_ID -h HUB_NAME -e EXPOSURE_MODE [-n]"
    echo ""
    echo "Options:"
    echo "  -d DEVICE_ID      The device ID in Azure IoT Hub"
    echo "  -h HUB_NAME       The name of the Azure IoT Hub"
    echo "  -e EXPOSURE_MODE  The exposure mode to set"
    echo "  -n                Use nested configuration format (Camera.ExposureMode)"
    echo ""
    echo "Valid exposure modes:"
    echo "  Auto (default)    - Automatic exposure mode selection"
    echo "  Normal            - Standard exposure mode for general use"
    echo "  Sport             - Fast shutter speed for moving subjects"
    echo "  Night             - Enhanced low-light performance"
    echo "  Backlight         - Compensates for bright background"
    echo "  Spotlight         - Optimized for bright spot lighting"
    echo "  Beach             - Optimized for bright beach/sand conditions"
    echo "  Snow              - Optimized for bright snow conditions"
    echo "  Fireworks         - Long exposure for fireworks"
    echo "  Party             - Indoor party lighting"
    echo "  Candlelight       - Warm, low-light conditions"
    echo "  Barcode           - High contrast for barcode/LED reading (recommended for LED displays)"
    echo "  Macro             - Close-up photography"
    echo "  Landscape         - Wide depth of field"
    echo "  Portrait          - Shallow depth of field"
    echo "  Antishake         - Reduced camera shake"
    echo "  FixedFps          - Fixed frame rate mode"
    echo ""
    echo "Examples:"
    echo "  $0 -d wellmonitor-device -h wellmonitor-hub -e Barcode"
    echo "  $0 -d wellmonitor-device -h wellmonitor-hub -e Auto -n"
}

# Parse command line arguments
DEVICE_ID=""
HUB_NAME=""
EXPOSURE_MODE=""
USE_NESTED_CONFIG=false

while getopts "d:h:e:n" opt; do
    case $opt in
        d) DEVICE_ID="$OPTARG" ;;
        h) HUB_NAME="$OPTARG" ;;
        e) EXPOSURE_MODE="$OPTARG" ;;
        n) USE_NESTED_CONFIG=true ;;
        *) show_usage; exit 1 ;;
    esac
done

# Validate required parameters
if [[ -z "$DEVICE_ID" || -z "$HUB_NAME" || -z "$EXPOSURE_MODE" ]]; then
    echo -e "${RED}Error: Missing required parameters${NC}"
    show_usage
    exit 1
fi

# Validate exposure mode
VALID_MODES=("Auto" "Normal" "Sport" "Night" "Backlight" "Spotlight" "Beach" "Snow" "Fireworks" "Party" "Candlelight" "Barcode" "Macro" "Landscape" "Portrait" "Antishake" "FixedFps")
if [[ ! " ${VALID_MODES[@]} " =~ " ${EXPOSURE_MODE} " ]]; then
    echo -e "${RED}Error: Invalid exposure mode '$EXPOSURE_MODE'${NC}"
    echo "Valid modes: ${VALID_MODES[*]}"
    exit 1
fi

# Validate Azure CLI is available
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed or not in PATH${NC}"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo -e "${RED}Error: Not logged in to Azure. Please run 'az login' first.${NC}"
    exit 1
fi

# Verify IoT Hub exists
echo -e "${YELLOW}Verifying IoT Hub '$HUB_NAME' exists...${NC}"
if ! az iot hub show --name "$HUB_NAME" --output json &> /dev/null; then
    echo -e "${RED}Error: IoT Hub '$HUB_NAME' not found or not accessible.${NC}"
    exit 1
fi

# Verify device exists
echo -e "${YELLOW}Verifying device '$DEVICE_ID' exists...${NC}"
if ! az iot hub device-identity show --device-id "$DEVICE_ID" --hub-name "$HUB_NAME" --output json &> /dev/null; then
    echo -e "${RED}Error: Device '$DEVICE_ID' not found in IoT Hub '$HUB_NAME'.${NC}"
    exit 1
fi

# Create the patch based on configuration format
if [[ "$USE_NESTED_CONFIG" == true ]]; then
    echo -e "${YELLOW}Using nested configuration format (Camera.ExposureMode)...${NC}"
    PATCH="{\"properties\":{\"desired\":{\"Camera\":{\"ExposureMode\":\"$EXPOSURE_MODE\"}}}}"
    echo -e "${GREEN}Setting Camera.ExposureMode = '$EXPOSURE_MODE'${NC}"
else
    echo -e "${YELLOW}Using legacy configuration format (cameraExposureMode)...${NC}"
    PATCH="{\"properties\":{\"desired\":{\"cameraExposureMode\":\"$EXPOSURE_MODE\"}}}"
    echo -e "${GREEN}Setting cameraExposureMode = '$EXPOSURE_MODE'${NC}"
fi

# Update device twin
echo -e "${YELLOW}Updating device twin...${NC}"
if az iot hub device-twin update --device-id "$DEVICE_ID" --hub-name "$HUB_NAME" --set "$PATCH" --output json &> /dev/null; then
    echo -e "${GREEN}✅ Device twin updated successfully!${NC}"
    echo -e "${GREEN}Camera exposure mode set to: $EXPOSURE_MODE${NC}"
    
    # Display exposure mode description
    case "$EXPOSURE_MODE" in
        "Auto") echo -e "${CYAN}Mode Description: Automatic exposure mode selection${NC}" ;;
        "Normal") echo -e "${CYAN}Mode Description: Standard exposure mode for general use${NC}" ;;
        "Sport") echo -e "${CYAN}Mode Description: Fast shutter speed for moving subjects${NC}" ;;
        "Night") echo -e "${CYAN}Mode Description: Enhanced low-light performance${NC}" ;;
        "Backlight") echo -e "${CYAN}Mode Description: Compensates for bright background${NC}" ;;
        "Spotlight") echo -e "${CYAN}Mode Description: Optimized for bright spot lighting${NC}" ;;
        "Beach") echo -e "${CYAN}Mode Description: Optimized for bright beach/sand conditions${NC}" ;;
        "Snow") echo -e "${CYAN}Mode Description: Optimized for bright snow conditions${NC}" ;;
        "Fireworks") echo -e "${CYAN}Mode Description: Long exposure for fireworks${NC}" ;;
        "Party") echo -e "${CYAN}Mode Description: Indoor party lighting${NC}" ;;
        "Candlelight") echo -e "${CYAN}Mode Description: Warm, low-light conditions${NC}" ;;
        "Barcode") echo -e "${CYAN}Mode Description: High contrast for barcode/LED reading${NC}" ;;
        "Macro") echo -e "${CYAN}Mode Description: Close-up photography${NC}" ;;
        "Landscape") echo -e "${CYAN}Mode Description: Wide depth of field${NC}" ;;
        "Portrait") echo -e "${CYAN}Mode Description: Shallow depth of field${NC}" ;;
        "Antishake") echo -e "${CYAN}Mode Description: Reduced camera shake${NC}" ;;
        "FixedFps") echo -e "${CYAN}Mode Description: Fixed frame rate mode${NC}" ;;
    esac
    
    echo ""
    echo -e "${YELLOW}The device will automatically pick up this configuration change.${NC}"
    echo -e "${YELLOW}Monitor the device logs to confirm the new exposure mode is applied.${NC}"
else
    echo -e "${RED}Error: Failed to update device twin${NC}"
    exit 1
fi

# Optional: Show current device twin desired properties
echo ""
echo -e "${YELLOW}Current device twin desired properties:${NC}"
CURRENT_TWIN=$(az iot hub device-twin show --device-id "$DEVICE_ID" --hub-name "$HUB_NAME" --output json)

if [[ "$USE_NESTED_CONFIG" == true ]]; then
    echo -e "${CYAN}Camera Configuration:${NC}"
    echo "$CURRENT_TWIN" | jq '.properties.desired.Camera // {}'
else
    echo -e "${CYAN}Camera-related properties:${NC}"
    echo "$CURRENT_TWIN" | jq '.properties.desired | to_entries[] | select(.key | startswith("camera")) | "\(.key): \(.value)"' -r
fi

echo ""
echo -e "${GREEN}✅ Camera exposure mode configuration completed!${NC}"
