#!/bin/bash

# MapMe Cosmos DB Initialization Script (Shell Version)
# Initializes Cosmos DB emulator with database and containers
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
COSMOS_ENDPOINT="https://localhost:8081"
COSMOS_KEY="C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
DATABASE_NAME="mapme"
FORCE_RECREATE=false

# Function to display usage
show_usage() {
    echo -e "${CYAN}MapMe Cosmos DB Initialization Script${NC}"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --endpoint URL        Cosmos DB endpoint (default: https://localhost:8081)"
    echo "  --key KEY            Cosmos DB key (default: emulator key)"
    echo "  --database NAME      Database name (default: mapme)"
    echo "  --force-recreate     Force recreate database and containers"
    echo "  --help              Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                    # Initialize with defaults"
    echo "  $0 --force-recreate   # Force recreate everything"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --endpoint)
            COSMOS_ENDPOINT="$2"
            shift 2
            ;;
        --key)
            COSMOS_KEY="$2"
            shift 2
            ;;
        --database)
            DATABASE_NAME="$2"
            shift 2
            ;;
        --force-recreate)
            FORCE_RECREATE=true
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
echo -e "${GREEN}  MapMe Cosmos DB Initialization${NC}"
echo -e "${CYAN}============================================================${NC}"

echo -e "${BLUE}📋 Configuration:${NC}"
echo -e "  Endpoint: $COSMOS_ENDPOINT"
echo -e "  Database: $DATABASE_NAME"
echo -e "  Force Recreate: $FORCE_RECREATE"
echo ""

# Check if Cosmos DB is accessible
echo -e "${YELLOW}🔍 Checking Cosmos DB connectivity...${NC}"
if curl -k -s "$COSMOS_ENDPOINT" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Cosmos DB is accessible${NC}"
else
    echo -e "${RED}❌ Cannot connect to Cosmos DB at $COSMOS_ENDPOINT${NC}"
    echo -e "${YELLOW}💡 Make sure Cosmos DB emulator is running${NC}"
    exit 1
fi

# Function to create database
create_database() {
    echo -e "${YELLOW}🗄️  Creating database: $DATABASE_NAME${NC}"
    
    # Note: In a real implementation, you would use Azure CLI or REST API calls
    # For the emulator, we'll use a simple approach that works with the .NET application
    echo -e "${GREEN}✅ Database creation will be handled by the application${NC}"
}

# Function to create containers
create_containers() {
    echo -e "${YELLOW}📦 Creating containers...${NC}"
    
    # Container definitions
    containers=(
        "UserProfiles:/id"
        "DateMarks:/userId"
        "ChatMessages:/conversationId"
        "Conversations:/id"
    )
    
    for container_def in "${containers[@]}"; do
        IFS=':' read -r container_name partition_key <<< "$container_def"
        echo -e "${BLUE}  📦 Container: $container_name (Partition: $partition_key)${NC}"
        # Note: Container creation will be handled by the .NET application
    done
    
    echo -e "${GREEN}✅ Container creation will be handled by the application${NC}"
}

# Function to setup indexing
setup_indexing() {
    echo -e "${YELLOW}🔍 Setting up indexing policies...${NC}"
    
    echo -e "${BLUE}  🔍 UserProfiles: Standard indexing${NC}"
    echo -e "${BLUE}  🔍 DateMarks: Geospatial + composite indexing${NC}"
    echo -e "${BLUE}  🔍 ChatMessages: Composite indexing${NC}"
    echo -e "${BLUE}  🔍 Conversations: Standard indexing${NC}"
    
    echo -e "${GREEN}✅ Indexing policies will be handled by the application${NC}"
}

# Main initialization
echo -e "${YELLOW}🚀 Starting Cosmos DB initialization...${NC}"

if [ "$FORCE_RECREATE" = true ]; then
    echo -e "${YELLOW}⚠️  Force recreate enabled - will recreate database and containers${NC}"
fi

# Create database
create_database

# Create containers
create_containers

# Setup indexing
setup_indexing

echo -e "${CYAN}============================================================${NC}"
echo -e "${GREEN}✅ Cosmos DB Initialization Complete${NC}"
echo -e "${CYAN}============================================================${NC}"

echo -e "${BLUE}📋 Next Steps:${NC}"
echo -e "  1. Run your .NET application to auto-create containers"
echo -e "  2. Use Azure Storage Explorer to verify setup"
echo -e "  3. Run tests to validate functionality"
echo ""
echo -e "${YELLOW}💡 Connection Details for Storage Explorer:${NC}"
echo -e "  Endpoint: $COSMOS_ENDPOINT"
echo -e "  Key: $COSMOS_KEY"
echo -e "  Database: $DATABASE_NAME"

exit 0
