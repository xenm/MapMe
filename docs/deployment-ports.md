# MapMe Port Configuration for Different Environments

## Port Usage by Environment

### **Local Development**
```
Application: https://localhost:8008
CosmosDB Emulator: https://localhost:8081
CosmosDB Services: 10251-10255 (internal)
```

### **Docker Local Testing**
```
Application: http://localhost:8080
CosmosDB Emulator: https://localhost:8081
CosmosDB Services: 10251-10255 (internal)
```

### **Azure Kubernetes Service (AKS) Production**
```
Application: https://your-domain.com (port 443 via ingress)
Internal Service: port 8080 (container port)
Database: Azure Cosmos DB (managed service, no local ports)
```

## Google OAuth Configuration by Environment

### **Development OAuth Settings (Simplified)**
```
Authorized JavaScript Origins:
- https://localhost:8008

Authorized Redirect URIs:
- https://localhost:8008/signin-google
```

### **Production OAuth Settings (Add These)**
```
Authorized JavaScript Origins:
- https://your-production-domain.com
- https://mapme.yourdomain.com

Authorized Redirect URIs:
- https://your-production-domain.com/signin-google
- https://mapme.yourdomain.com/signin-google
```

## Recommended Simplification

### **Simplified Port Configuration**
**✅ Keep Only:**
- `https://localhost:8008` (MapMe's primary development port)

**❌ Remove These Unused Ports:**
- `http://localhost:5000` / `https://localhost:5001` (ASP.NET Core defaults - not used)
- `https://localhost:7160` / `https://localhost:7198` (Not used by MapMe)
- `http://localhost:8080` (Only needed for Docker testing)

### **Minimal Development OAuth Config**
```json
{
  "web": {
    "client_id": "your-client-id.apps.googleusercontent.com",
    "auth_uri": "https://accounts.google.com/o/oauth2/auth",
    "token_uri": "https://oauth2.googleapis.com/token",
    "javascript_origins": [
      "https://localhost:8008"
    ],
    "redirect_uris": [
      "https://localhost:8008/signin-google"
    ]
  }
}
```

## Azure Deployment Considerations

### **Kubernetes Service Configuration**
```yaml
apiVersion: v1
kind: Service
metadata:
  name: mapme-service
spec:
  selector:
    app: mapme
  ports:
    - port: 80
      targetPort: 8080  # Container port
  type: LoadBalancer
```

### **Ingress Configuration**
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: mapme-ingress
spec:
  tls:
    - hosts:
        - mapme.yourdomain.com
      secretName: mapme-tls
  rules:
    - host: mapme.yourdomain.com
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: mapme-service
                port:
                  number: 80
```

### **Environment Variables for Production**
```yaml
env:
  - name: ASPNETCORE_URLS
    value: "http://+:8080"
  - name: Cosmos__Endpoint
    valueFrom:
      secretKeyRef:
        name: cosmos-secret
        key: endpoint
  - name: Cosmos__Key
    valueFrom:
      secretKeyRef:
        name: cosmos-secret
        key: key
```

## Port Security Considerations

### **Development (Localhost)**
- Port 8008: HTTPS with self-signed cert
- Port 8081: CosmosDB Emulator (local only)
- Ports 10251-10255: Internal services (not exposed)

### **Production (Azure)**
- Port 443: HTTPS with valid SSL certificate
- Port 8080: Internal container port (not directly exposed)
- No CosmosDB ports (managed service)

## Summary

**For Google OAuth:**
- **Development**: Only need `https://localhost:8008`
- **Production**: Add your actual domain `https://mapme.yourdomain.com`
- **Remove**: Unused ports (5000, 5001) unless specifically needed

**For Azure Deployment:**
- Container runs on port 8080 internally
- Kubernetes exposes via ingress on port 443
- No CosmosDB Emulator ports needed (use Azure Cosmos DB)
