#!/bin/bash

# MapMe Cosmos DB Stop Script (Shell Version)
# Stops Cosmos DB emulator container
# Compatible with macOS M3 and other Unix systems

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
REMOVE_CONTAINER=false
REMOVE_DATA=false

# Function to display usage
show_usage() {
    echo -e "${CYAN}MapMe Cosmos DB Stop Script${NC}"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --remove-container    Remove container after stopping"
    echo "  --remove-data        Remove data volumes (WARNING: Data loss!)"
    echo "  --help              Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                     # Stop container"
    echo "  $0 --remove-container  # Stop and remove container"
    echo "  $0 --remove-data       # Stop and remove all data"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --remove-container)
            REMOVE_CONTAINER=true
            shift
            ;;
        --remove-data)
            REMOVE_DATA=true
            REMOVE_CONTAINER=true
            shift
            ;;
        --help)
            show_usage
            exit 0
            ;;
        *)
            echo -e "${RED}❌ Unknown option: $1${NC}"
            show_usage
            exit 1
            ;;
    esac
done

echo -e "${CYAN}============================================================${NC}"
echo -e "${GREEN}  MapMe Cosmos DB Stop${NC}"
echo -e "${CYAN}============================================================${NC}"

CONTAINER_NAME="mapme-cosmos-emulator"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not running${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Docker is running${NC}"

# Check if container exists
if ! docker ps -a --format "table {{.Names}}" | grep -q "^${CONTAINER_NAME}$"; then
    echo -e "${YELLOW}⚠️  Container '$CONTAINER_NAME' does not exist${NC}"
    exit 0
fi

# Check if container is running
if docker ps --format "table {{.Names}}" | grep -q "^${CONTAINER_NAME}$"; then
    echo -e "${YELLOW}🛑 Stopping Cosmos DB container...${NC}"
    docker stop "$CONTAINER_NAME"
    echo -e "${GREEN}✅ Container stopped${NC}"
else
    echo -e "${YELLOW}ℹ️  Container is already stopped${NC}"
fi

# Remove container if requested
if [ "$REMOVE_CONTAINER" = true ]; then
    echo -e "${YELLOW}🗑️  Removing container...${NC}"
    docker rm "$CONTAINER_NAME"
    echo -e "${GREEN}✅ Container removed${NC}"
fi

# Remove data if requested
if [ "$REMOVE_DATA" = true ]; then
    echo -e "${RED}⚠️  WARNING: Removing data volumes!${NC}"
    
    # Remove named volumes if they exist
    if docker volume ls -q | grep -q "mapme-cosmos-data"; then
        echo -e "${YELLOW}🗑️  Removing data volume...${NC}"
        docker volume rm mapme-cosmos-data
        echo -e "${GREEN}✅ Data volume removed${NC}"
    fi
    
    echo -e "${RED}💀 All Cosmos DB data has been removed!${NC}"
fi

echo -e "${CYAN}============================================================${NC}"
echo -e "${GREEN}✅ Cosmos DB Stop Complete${NC}"
echo -e "${CYAN}============================================================${NC}"

if [ "$REMOVE_DATA" = true ]; then
    echo -e "${RED}⚠️  Data has been permanently removed${NC}"
elif [ "$REMOVE_CONTAINER" = true ]; then
    echo -e "${YELLOW}ℹ️  Container removed, data preserved${NC}"
else
    echo -e "${BLUE}ℹ️  Container stopped, data preserved${NC}"
    echo -e "${YELLOW}💡 Use 'docker start $CONTAINER_NAME' to restart${NC}"
fi

exit 0
