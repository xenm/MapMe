#!/bin/bash
#
# Start Cosmos DB emulator and initialize MapMe database
# Shell version for macOS compatibility
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Parse command line arguments
SKIP_INIT=false
DETACHED=true

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-init)
            SKIP_INIT=true
            shift
            ;;
        --no-detach)
            DETACHED=false
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [--skip-init] [--no-detach]"
            echo "  --skip-init     Skip database initialization"
            echo "  --no-detach     Run container in foreground"
            exit 0
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

function write_step() {
    echo -e "${YELLOW}🔧 $1${NC}"
}

function write_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

function write_error() {
    echo -e "${RED}❌ $1${NC}"
}

function write_info() {
    echo -e "${CYAN}ℹ️  $1${NC}"
}

echo -e "${GREEN}🚀 Starting CosmosDB Emulator for MapMe...${NC}"

# Check if Docker is running
write_step "Checking Docker status..."
if ! docker version >/dev/null 2>&1; then
    write_error "Docker is not running. Please start Docker Desktop first."
    exit 1
fi
write_success "Docker is running"

# Stop existing container if running
write_step "Stopping existing CosmosDB container..."
docker stop mapme-cosmos-emulator 2>/dev/null || true
docker rm mapme-cosmos-emulator 2>/dev/null || true

# Start CosmosDB emulator
write_step "Starting CosmosDB emulator container..."

if [ "$DETACHED" = true ]; then
    DOCKER_CMD="docker run -d"
else
    DOCKER_CMD="docker run"
fi

DOCKER_CMD="$DOCKER_CMD --name mapme-cosmos-emulator"
DOCKER_CMD="$DOCKER_CMD --publish 8081:8081"
DOCKER_CMD="$DOCKER_CMD --publish 1234:1234"
DOCKER_CMD="$DOCKER_CMD mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview"
DOCKER_CMD="$DOCKER_CMD --protocol https"

if ! eval $DOCKER_CMD; then
    write_error "Failed to start CosmosDB emulator"
    exit 1
fi

write_step "Waiting for CosmosDB emulator to be ready..."

# Wait for emulator to be ready
MAX_ATTEMPTS=12
ATTEMPT=0
READY=false

while [ $ATTEMPT -lt $MAX_ATTEMPTS ] && [ "$READY" = false ]; do
    ATTEMPT=$((ATTEMPT + 1))
    echo "   Attempt $ATTEMPT/$MAX_ATTEMPTS..."
    
    # Check if container is running and logs show "Started"
    if docker logs mapme-cosmos-emulator 2>/dev/null | grep -q "Started"; then
        READY=true
    else
        sleep 5
    fi
done

if [ "$READY" = false ]; then
    write_error "CosmosDB emulator failed to start within timeout"
    write_info "Try running: docker logs mapme-cosmos-emulator"
    exit 1
fi

write_success "CosmosDB emulator is ready!"

# Initialize database if not skipped
if [ "$SKIP_INIT" = false ]; then
    write_step "Initializing MapMe database..."
    
    INIT_SCRIPT="$(dirname "$0")/init-cosmosdb.ps1"
    if [ -f "$INIT_SCRIPT" ]; then
        # Try to run with PowerShell if available
        if command -v pwsh >/dev/null 2>&1; then
            pwsh -File "$INIT_SCRIPT"
        elif command -v powershell >/dev/null 2>&1; then
            powershell -File "$INIT_SCRIPT"
        else
            write_info "PowerShell not available, skipping database initialization"
            write_info "Run manually: ./Scripts/init-cosmosdb.ps1"
        fi
    else
        write_info "Database initialization script not found at: $INIT_SCRIPT"
        write_info "Run manually: ./Scripts/init-cosmosdb.ps1"
    fi
fi

echo ""
write_success "🎉 CosmosDB setup completed!"
echo -e "${CYAN}📊 Connection Details:${NC}"
echo -e "   • Endpoint: https://localhost:8081"
echo -e "   • Key: C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
echo -e "   • Database: mapme"
echo ""
echo -e "${CYAN}🔗 CosmosDB Storage Explorer:${NC}"
echo -e "   • Open Storage Explorer"
echo -e "   • Add CosmosDB Account"
echo -e "   • Use connection details above"
echo ""
write_success "⚡ Ready to run MapMe!"
echo -e "${CYAN}💡 Next steps:${NC}"
echo -e "   1. Run service tests: ./Scripts/test-service-cosmos.ps1"
echo -e "   2. Run health check: ./Scripts/test-cosmos-health.sh"
echo -e "   3. Run MapMe: dotnet run --project MapMe/MapMe"
