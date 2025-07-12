#!/bin/bash

# WellMonitor Pi Sync and Run Script
# Usage: ./sync-and-run.sh [--clean] [--no-run]

set -e  # Exit on any error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DEVICE_PROJECT="$PROJECT_ROOT/src/WellMonitor.Device"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Parse arguments
CLEAN_BUILD=false
NO_RUN=false

for arg in "$@"; do
    case $arg in
        --clean)
            CLEAN_BUILD=true
            shift
            ;;
        --no-run)
            NO_RUN=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [--clean] [--no-run]"
            echo "  --clean    Clean build (removes bin/obj folders)"
            echo "  --no-run   Build only, don't run the application"
            exit 0
            ;;
    esac
done

echo -e "${BLUE}🔄 WellMonitor Pi Sync and Build${NC}"
echo "========================================"

# Check if we're in a git repository
if [ ! -d "$PROJECT_ROOT/.git" ]; then
    echo -e "${RED}❌ Error: Not in a git repository${NC}"
    exit 1
fi

cd "$PROJECT_ROOT"

# Show current status
echo -e "${BLUE}📍 Current status:${NC}"
echo "  Branch: $(git branch --show-current)"
echo "  Location: $PROJECT_ROOT"
echo "  Last commit: $(git log -1 --oneline)"
echo ""

# Check for uncommitted changes
if ! git diff --quiet || ! git diff --cached --quiet; then
    echo -e "${YELLOW}⚠️  Warning: You have uncommitted changes${NC}"
    git status --porcelain
    echo ""
    read -p "Continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Aborting."
        exit 1
    fi
fi

# Fetch and pull latest changes
echo -e "${BLUE}📥 Fetching latest changes...${NC}"
git fetch origin

# Check if we're behind
BEHIND=$(git rev-list --count HEAD..origin/$(git branch --show-current))
if [ "$BEHIND" -gt 0 ]; then
    echo -e "${GREEN}🔄 Pulling $BEHIND new commit(s)...${NC}"
    git pull
else
    echo -e "${GREEN}✅ Already up to date${NC}"
fi

# Clean build if requested
if [ "$CLEAN_BUILD" = true ]; then
    echo -e "${BLUE}🧹 Cleaning previous build...${NC}"
    dotnet clean
    find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
    find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
fi

# Restore and build
echo -e "${BLUE}📦 Restoring packages...${NC}"
dotnet restore

echo -e "${BLUE}🔨 Building project...${NC}"
if dotnet build --configuration Release; then
    echo -e "${GREEN}✅ Build successful${NC}"
else
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
fi

# Run tests if they exist
if [ -d "tests" ] && [ "$(find tests -name "*.csproj" | wc -l)" -gt 0 ]; then
    echo -e "${BLUE}🧪 Running tests...${NC}"
    if dotnet test --no-build --configuration Release --verbosity minimal; then
        echo -e "${GREEN}✅ Tests passed${NC}"
    else
        echo -e "${YELLOW}⚠️  Some tests failed${NC}"
    fi
fi

echo ""
echo -e "${GREEN}🎉 Sync and build completed successfully!${NC}"

# Run the application if requested
if [ "$NO_RUN" = false ]; then
    echo ""
    echo -e "${BLUE}🚀 Starting WellMonitor Device...${NC}"
    echo "Press Ctrl+C to stop the application"
    echo "========================================"
    cd "$DEVICE_PROJECT"
    exec dotnet run --configuration Release --no-build
else
    echo ""
    echo -e "${BLUE}📝 To run the application manually:${NC}"
    echo "cd $DEVICE_PROJECT"
    echo "dotnet run --configuration Release --no-build"
fi
