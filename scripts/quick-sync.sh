#!/bin/bash

# Quick Sync Script for Development
# Pulls latest changes and shows status

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}🔄 Quick Sync - WellMonitor${NC}"
echo "=========================="

# Show current status
echo "Branch: $(git branch --show-current)"
echo "Last commit: $(git log -1 --oneline)"
echo ""

# Pull latest changes
echo -e "${BLUE}📥 Pulling latest changes...${NC}"
git pull

# Show what changed
COMMITS=$(git log --oneline HEAD~1..HEAD)
if [ -n "$COMMITS" ]; then
    echo -e "${GREEN}📋 New changes:${NC}"
    echo "$COMMITS"
else
    echo -e "${YELLOW}No new changes${NC}"
fi

echo ""
echo -e "${GREEN}✅ Sync complete!${NC}"
echo ""
echo "Next steps:"
echo "  1. To build and run: ./scripts/sync-and-run.sh"
echo "  2. To just build: dotnet build"
echo "  3. To run: cd src/WellMonitor.Device && dotnet run"
