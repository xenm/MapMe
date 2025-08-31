# Docker Deployment

## Docker Configuration

### Multi-Stage Dockerfile

**Recommended Dockerfile structure:**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files
COPY ["MapMe/MapMe/MapMe.csproj", "MapMe/MapMe/"]
COPY ["MapMe/MapMe.Client/MapMe.Client.csproj", "MapMe/MapMe.Client/"]
RUN dotnet restore "MapMe/MapMe/MapMe.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/MapMe/MapMe"
RUN dotnet publish "MapMe.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=https://+:443;http://+:80

# Expose ports
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "MapMe.dll"]
```

### Docker Compose

**Development with Cosmos DB:**
```yaml
version: '3.8'
services:
  mapme:
    build: .
    ports:
      - "8008:80"
      - "8009:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - GOOGLE_MAPS_API_KEY=${GOOGLE_MAPS_API_KEY}
      - COSMOSDB_CONNECTION_STRING=AccountEndpoint=https://cosmosdb:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw==
    depends_on:
      - cosmosdb
    volumes:
      - ~/.aspnet/https:/https:ro

  cosmosdb:
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
    ports:
      - "8081:8081"
      - "10251:10251"
      - "10252:10252"
      - "10253:10253"
      - "10254:10254"
    environment:
      - AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10
      - AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true
    volumes:
      - cosmos-data:/data
    tty: true

volumes:
  cosmos-data:
```

## Build Process

### Release Build
```bash
# Standard release build
dotnet publish MapMe/MapMe/MapMe.csproj -c Release -o out

# Self-contained deployment
dotnet publish MapMe/MapMe/MapMe.csproj -c Release -o out --self-contained true -r linux-x64

# Framework-dependent deployment
dotnet publish MapMe/MapMe/MapMe.csproj -c Release -o out --self-contained false
```

### Docker Build Commands
```bash
# Build image
docker build -t mapme:latest .

# Build with build arguments
docker build --build-arg ASPNETCORE_ENVIRONMENT=Production -t mapme:latest .

# Multi-platform build
docker buildx build --platform linux/amd64,linux/arm64 -t mapme:latest .
```

## Container Configuration

### Environment Variables
```bash
# Core configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80

# Application secrets
GOOGLE_MAPS_API_KEY=your-api-key
JWT_SECRET_KEY=your-jwt-secret-key

# Database
COSMOSDB_CONNECTION_STRING=your-connection-string
COSMOSDB_DATABASE_NAME=mapme

# Logging
SERILOG_MINIMUM_LEVEL=Information
```

### Volume Mounts
```bash
# HTTPS certificates (development)
-v ~/.aspnet/https:/https:ro

# Application data (if using file storage)
-v /app/data:/app/data

# Logs (if using file logging)
-v /app/logs:/app/logs
```

### Health Checks
```dockerfile
# Add health check to Dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1
```

## Production Deployment

### Container Registry
```bash
# Tag for registry
docker tag mapme:latest your-registry.azurecr.io/mapme:latest

# Push to registry
docker push your-registry.azurecr.io/mapme:latest
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mapme
spec:
  replicas: 3
  selector:
    matchLabels:
      app: mapme
  template:
    metadata:
      labels:
        app: mapme
    spec:
      containers:
      - name: mapme
        image: your-registry.azurecr.io/mapme:latest
        ports:
        - containerPort: 80
        - containerPort: 443
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: GOOGLE_MAPS_API_KEY
          valueFrom:
            secretKeyRef:
              name: mapme-secrets
              key: google-maps-api-key
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
```

### Azure Container Instances
```bash
# Deploy to ACI
az container create \
  --resource-group myResourceGroup \
  --name mapme \
  --image your-registry.azurecr.io/mapme:latest \
  --dns-name-label mapme-app \
  --ports 80 443 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
  --secure-environment-variables \
    GOOGLE_MAPS_API_KEY=your-api-key \
    JWT_SECRET_KEY=your-jwt-secret
```

## Monitoring and Logging

### Container Logs
```bash
# View container logs
docker logs mapme-container

# Follow logs
docker logs -f mapme-container

# Logs with timestamps
docker logs -t mapme-container
```

### Performance Monitoring
```bash
# Container resource usage
docker stats mapme-container

# Container inspection
docker inspect mapme-container
```

## Troubleshooting

### Common Issues
- **Port conflicts**: Ensure ports 80/443 are available
- **Certificate issues**: Verify HTTPS certificate configuration
- **Environment variables**: Check all required variables are set
- **Database connectivity**: Verify Cosmos DB connection string
- **API key restrictions**: Ensure Google Maps API key allows container IP

### Debug Commands
```bash
# Run container interactively
docker run -it mapme:latest /bin/bash

# Execute commands in running container
docker exec -it mapme-container /bin/bash

# Check container health
docker inspect --format='{{.State.Health.Status}}' mapme-container
```

---

**Related Documentation:**
- [Deployment Overview](README.md)
- [Environment Configuration](environments.md)
- [Infrastructure](infrastructure.md)
- [CI/CD Pipeline](ci-cd.md)
