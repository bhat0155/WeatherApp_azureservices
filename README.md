# WeatherApp

A two-tier weather application built to showcase Azure DevOps CI/CD pipelines.

- **Backend**: ASP.NET Core 9 Web API + Entity Framework Core + SQL Server
- **Frontend**: React 18 + Vite
- **External data**: OpenWeatherMap API (free tier)
- **Tests**: 20 backend (xUnit) + 27 frontend (Vitest)

---

## Architecture

```
┌─────────────────┐     HTTP GET      ┌──────────────────────┐     HTTPS GET      ┌─────────────────────┐
│  React (Vite)   │ ────────────────► │  ASP.NET Core API    │ ─────────────────► │  OpenWeatherMap API │
│  :5173          │ ◄──────────────── │  :5050               │ ◄───────────────── │  api.openweather..  │
└─────────────────┘     JSON resp     │                      │     JSON resp       └─────────────────────┘
                                      │  ┌────────────────┐  │
                                      │  │  EF Core ORM   │  │
                                      │  └────────┬───────┘  │
                                      └───────────┼──────────┘
                                                  │ SQL (port 1433)
                                                  ▼
                                      ┌──────────────────────┐
                                      │  SQL Server          │
                                      │  Docker / Azure SQL  │
                                      │  WeatherAppDb        │
                                      └──────────────────────┘
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| Node.js | 20+ |
| Docker Desktop | Latest (for SQL Server) |
| OpenWeatherMap API key | Free at openweathermap.org |

> **macOS note:** LocalDB is Windows-only. This project uses Docker to run SQL Server locally.

### Get a free OpenWeatherMap API key

1. Go to https://openweathermap.org/api and create a free account.
2. Go to **API Keys** in your account dashboard.
3. Copy the default key (active within ~10 minutes of account creation).

---

## Local Database Setup (Docker)

Start a SQL Server container before running the API. Only needed once — after that just `docker start weatherapp-sql`.

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=WeatherApp@123" \
  -p 1433:1433 --name weatherapp-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Wait ~15 seconds for SQL Server to fully start, then continue with backend setup.

**On subsequent runs** (after a reboot):
```bash
docker start weatherapp-sql
```

---

## Backend Setup

### 1. Install the EF Core CLI (once)

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:/Users/$USER/.dotnet/tools"   # add to ~/.zprofile to make permanent
```

### 2. Restore packages

```bash
cd backend
dotnet restore
```

### 3. Create appsettings.Development.json

```bash
cp WeatherApp.Api/appsettings.Development.json.example WeatherApp.Api/appsettings.Development.json
```

Open the file and replace `YOUR_OPENWEATHERMAP_API_KEY_HERE` with your actual key. The Docker connection string is already filled in.

### 4. Run EF Core migrations

```bash
cd WeatherApp.Api
dotnet ef migrations add InitialCreate --output-dir Migrations   # skip if Migrations/ folder already exists
dotnet ef database update
```

### 5. Start the API

```bash
dotnet run
```

API runs at **http://localhost:5050**
Swagger UI: **http://localhost:5050/swagger**

---

## Frontend Setup

### 1. Install dependencies

```bash
cd frontend
npm install
```

### 2. Create .env file

```bash
cp .env.example .env
```

The default `VITE_API_BASE_URL=http://localhost:5050` is already correct for local dev.

### 3. Start dev server

```bash
npm run dev
```

Frontend runs at **http://localhost:5173**

---

## Running Tests

### Backend (20 tests)

```bash
cd backend
dotnet test --logger "console;verbosity=normal"
```

### Frontend (27 tests)

```bash
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

### Example response — GET /api/weather/London

```json
{
  "id": 1,
  "city": "London",
  "country": "GB",
  "temperature": 15.5,
  "feelsLike": 13.0,
  "humidity": 80,
  "description": "clear sky",
  "iconCode": "01d",
  "iconUrl": "https://openweathermap.org/img/wn/01d@2x.png",
  "searchedAt": "2026-06-12T10:30:00Z"
}
```

### Error response format

```json
{
  "error": "City 'Xyz' was not found.",
  "statusCode": 404
}
```

---

## Azure Deployment

This app is designed to be deployed via Azure DevOps pipelines to Azure App Service.

- Secrets are injected as **App Service Application Settings** — they override `appsettings.json` automatically.
- Set `ConnectionStrings__DefaultConnection` to your Azure SQL Database connection string.
- Set `OpenWeatherMap__ApiKey` to your API key.
- Set `AllowedOrigins__0` to your deployed frontend URL (CORS).
- The frontend reads `VITE_API_BASE_URL` at **build time** — pass it as a pipeline variable in Azure DevOps.

See `ARCHITECTURE.md` for the full CI/CD pipeline design.
