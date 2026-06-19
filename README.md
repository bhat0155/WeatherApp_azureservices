# WeatherApp

A containerized two-tier weather application deployed to Azure Kubernetes Service (AKS) via an Azure DevOps CI/CD pipeline with full observability.

- **Backend**: ASP.NET Core 9 Web API + Entity Framework Core + Azure SQL
- **Frontend**: React 18 + Vite, served by NGINX
- **External data**: OpenWeatherMap API (free tier)
- **Tests**: 20 backend (xUnit) + 27 frontend (Vitest)
- **Registry**: Azure Container Registry (ACR)
- **Cluster**: AKS with NGINX Ingress Controller
- **Monitoring**: Azure Monitor Container Insights + Prometheus + Grafana

---

## Architecture

```
                        git push to main
                               │
                               ▼
                    ┌─────────────────────┐
                    │   Azure DevOps      │
                    │   CI/CD Pipeline    │
                    │                     │
                    │  Stage 1: Build     │
                    │  dotnet test (gate) │
                    │                     │
                    │  Stage 2: Docker    │
                    │  build + push → ACR │
                    │                     │
                    │  Stage 3: Deploy    │
                    │  kubectl apply      │
                    │  rolling update     │
                    │  smoke test         │
                    └──────────┬──────────┘
                               │
                               ▼
              ┌────────────────────────────────┐
              │   Azure Container Registry     │
              │   weatherapp-api:buildId        │
              │   weatherapp-frontend:buildId   │
              └────────────────┬───────────────┘
                               │ AKS pulls images
                               ▼
┌──────────────────────────────────────────────────────────┐
│                    AKS CLUSTER                           │
│                                                          │
│  ┌─────────────────────────────────────────────────┐    │
│  │  NGINX Ingress Controller  (public IP)           │    │
│  │  /api/* → weatherapp-api-svc                    │    │
│  │  /      → weatherapp-frontend-svc               │    │
│  └───────────────┬────────────────┬────────────────┘    │
│                  │                │                      │
│    ┌─────────────▼──────┐  ┌──────▼──────────────┐     │
│    │  weatherapp-api     │  │  weatherapp-frontend │     │
│    │  2 pods (replicas)  │  │  2 pods (replicas)  │     │
│    │  .NET 9 API         │  │  NGINX + React SPA  │     │
│    │  port 8080          │  │  port 80            │     │
│    │  /metrics ←─────────┼──┼── Prometheus scrape │     │
│    └────────┬────────────┘  └─────────────────────┘     │
│             │                                            │
│             ▼                                            │
│    ┌─────────────────┐   ┌──────────────────────────┐   │
│    │  Azure SQL      │   │  monitoring namespace     │   │
│    │  (external)     │   │  Prometheus               │   │
│    └─────────────────┘   │  Grafana                  │   │
│                          │  Alertmanager             │   │
│                          │  Node Exporter (DaemonSet)│   │
│                          └──────────────────────────┘   │
└──────────────────────────────────────────────────────────┘
```

---

## CI/CD Pipeline

Three stages — each gates the next:

| Stage | What happens | Time |
|---|---|---|
| **Build & Test** | `dotnet restore` → `dotnet build` → `dotnet test` (20 tests, gate) → publish results | ~1.5 min |
| **DockerBuild** | Login to ACR → build API image → build Frontend image (with `VITE_API_BASE_URL` baked in) → push both with `buildId` and `latest` tags | ~4 min |
| **DeployDev** | `az aks get-credentials` → apply namespace/configmap/secret → `sed` image tags into manifests → `kubectl apply` → `kubectl rollout status --timeout=5m` → `curl /api/health` smoke test | ~3 min |

**Total: ~9 minutes from push to live. Zero manual steps.**

Rolling update strategy (`maxUnavailable: 0`, `maxSurge: 1`): a new pod starts and passes its readiness probe before an old pod is terminated. Traffic is never dropped.

---

## Repository Structure

```
weather-app/
├── backend/
│   ├── Dockerfile                   ← multi-stage build (.NET SDK → runtime)
│   └── WeatherApp.Api/
│       ├── Program.cs               ← UseHttpMetrics() + MapMetrics() for Prometheus
│       └── WeatherApp.Api.csproj    ← prometheus-net.AspNetCore NuGet package
├── frontend/
│   ├── Dockerfile                   ← multi-stage build (Node → NGINX)
│   ├── nginx.conf                   ← try_files for SPA routing
│   └── src/
├── k8s/
│   ├── namespace.yaml
│   ├── configmap.yaml
│   ├── api-deployment.yaml          ← IMAGE_TAG placeholder replaced by pipeline sed
│   ├── api-service.yaml             ← ClusterIP, named port (required by ServiceMonitor)
│   ├── frontend-deployment.yaml
│   ├── frontend-service.yaml
│   ├── ingress.yaml                 ← /api → backend, / → frontend
│   └── servicemonitor.yaml          ← Prometheus scrape config via Operator CRD
├── docker-compose.yml               ← full local stack (db + api + frontend)
├── azure-pipelines.yml              ← 3-stage pipeline
└── azure-pipelines-project1.yml     ← original App Service pipeline (preserved)
```

> `secret.yaml` is **not in the repo**. The pipeline creates the Kubernetes Secret at deploy time using values from the Azure DevOps Variable Group. Committing a secret file risks it being applied by `kubectl apply -f k8s/` and overwriting the real secret.

---

## Local Development

### Option A — Docker Compose (recommended)

Runs all three services (SQL Server, API, frontend) with one command. No local .NET or Node install required.

```bash
# Create a .env file at the repo root
echo "OWM_API_KEY=your_openweathermap_key" > .env

# Build and start everything
docker-compose up --build

# API:      http://localhost:5050
# Frontend: http://localhost:3000

# Stop
docker-compose down
```

### Option B — Run services individually

**Prerequisites:**

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| Node.js | 20+ |
| Docker Desktop | Latest (for SQL Server) |
| OpenWeatherMap API key | Free at openweathermap.org |

**1. Start SQL Server:**

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=WeatherApp@123" \
  -p 1433:1433 --name weatherapp-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

**2. Backend:**

```bash
cd backend

# Install EF Core CLI (once)
dotnet tool install --global dotnet-ef

dotnet restore

# Copy and fill in dev config
cp WeatherApp.Api/appsettings.Development.json.example WeatherApp.Api/appsettings.Development.json
# Set your OWM API key in the file

cd WeatherApp.Api
dotnet ef database update
dotnet run
# API: http://localhost:5050
```

**3. Frontend:**

```bash
cd frontend
npm install
cp .env.example .env   # VITE_API_BASE_URL=http://localhost:5050
npm run dev
# Frontend: http://localhost:5173
```

---

## Running Tests

```bash
# Backend (20 tests)
cd backend
dotnet test --logger "console;verbosity=normal"

# Frontend (27 tests)
cd frontend
npm test
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/weather/{city}` | Fetch weather for a city (saves to history) |
| GET | `/api/weather/history` | Return last 10 searches |
| DELETE | `/api/weather/history` | Clear all history |
| GET | `/api/health` | Health check |
| GET | `/metrics` | Prometheus metrics (internal only — not via Ingress) |

### Example response — GET /api/weather/London

```json
{
  "city": "London",
  "country": "GB",
  "temperature": 15.5,
  "feelsLike": 13.0,
  "humidity": 80,
  "description": "clear sky",
  "iconCode": "01d",
  "iconUrl": "https://openweathermap.org/img/wn/01d@2x.png",
  "searchedAt": "2026-06-18T10:30:00Z"
}
```

---

## Observability

Two complementary monitoring layers:

### Azure Monitor Container Insights
Enabled on the AKS cluster. Deploys as a DaemonSet (one agent per node). Collects infrastructure metrics — node CPU/memory, pod restarts, scheduling failures — with zero code changes.

### Prometheus + Grafana
Installed via `kube-prometheus-stack` Helm chart. Covers application-level metrics.

The API exposes `/metrics` via `prometheus-net.AspNetCore`:

```csharp
app.UseHttpMetrics();  // instruments every HTTP request
app.MapMetrics();      // registers GET /metrics
```

A `ServiceMonitor` CRD tells the Prometheus Operator to scrape the API every 15 seconds:

```
ServiceMonitor → weatherapp-api Service → API pods → /metrics
```

**RED method dashboards in Grafana:**

| Metric | PromQL |
|---|---|
| Request Rate | `rate(http_requests_received_total{job="weatherapp-api-svc"}[5m])` |
| Error Rate (5xx) | `rate(http_requests_received_total{job="weatherapp-api-svc", code=~"5.."}[5m])` |
| p95 Latency | `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job="weatherapp-api-svc"}[5m]))` |

**Access Grafana locally:**

```bash
kubectl port-forward svc/monitoring-grafana 3000:80 -n monitoring
# http://localhost:3000  (admin / WeatherApp@Grafana123)
```

**Verify Prometheus targets:**

```bash
kubectl port-forward svc/monitoring-kube-prometheus-prometheus 9090:9090 -n monitoring
# http://localhost:9090/targets — weatherapp-api-monitor should show 2/2 UP
```

---

## Azure DevOps Variable Group

Pipeline variables stored in `weatherapp-vars-dev`:

| Variable | Description | Secret |
|---|---|---|
| `ACR_LOGIN_SERVER` | `<acr-name>.azurecr.io` | No |
| `ACR_USERNAME` | ACR admin username | No |
| `ACR_PASSWORD` | ACR admin password | Yes |
| `AKS_RESOURCE_GROUP` | `rg-weatherapp-dev` | No |
| `AKS_CLUSTER_NAME` | `weatherapp-aks` | No |
| `SQL_CONNECTION_STRING` | Azure SQL connection string | Yes |
| `OWM_API_KEY` | OpenWeatherMap API key | Yes |
| `INGRESS_EXTERNAL_IP` | NGINX Ingress public IP | No |
| `VITE_API_BASE_URL` | `http://<INGRESS_EXTERNAL_IP>` | No |
