# WeatherApp

A two-tier weather application built to showcase Azure DevOps CI/CD pipelines.

- **Backend**: ASP.NET Core 8 Web API with Entity Framework Core + SQL Server
- **Frontend**: React 18 + Vite
- **External data**: OpenWeatherMap API (free tier)

---

## Architecture

```
┌─────────────────┐       HTTP        ┌──────────────────────┐
│  React (Vite)   │ ────────────────► │  ASP.NET Core API    │
│  :5173          │                   │  :5000               │
└─────────────────┘                   │  ┌────────────────┐  │
                                      │  │  EF Core ORM   │  │
                    HTTPS             │  └────────┬───────┘  │
                ┌────────────────────►│           │          │
                │  OpenWeatherMap API │  ┌────────▼───────┐  │
                └────────────────────┘  │  SQL Server    │  │
                                        │  (LocalDB/     │  │
                                        │   Azure SQL)   │  │
                                        └────────────────┘  │
                                      └──────────────────────┘
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ (targets net9.0) |
| Node.js | 20+ |
| SQL Server LocalDB | Included with Visual Studio or standalone |
| OpenWeatherMap API key | Free at openweathermap.org |

### Get a free OpenWeatherMap API key

1. Go to https://openweathermap.org/api and create a free account.
2. Go to **API Keys** in your account dashboard.
3. Copy the default key (active within ~10 minutes of account creation).

---

## Backend Setup

### 1. Restore packages

```bash
cd backend
dotnet restore
```

### 2. Create appsettings.Development.json

```bash
cp WeatherApp.Api/appsettings.Development.json.example WeatherApp.Api/appsettings.Development.json
```

Open the file and replace `YOUR_OPENWEATHERMAP_API_KEY_HERE` with your actual key.

The file already has the LocalDB connection string — no changes needed for local dev.

### 3. Run EF Core migrations

```bash
cd WeatherApp.Api
dotnet ef database update
```

If `dotnet ef` is not installed:

```bash
dotnet tool install --global dotnet-ef
```

To create a migration from scratch (only needed if you change the schema):

```bash
dotnet ef migrations add InitialCreate --output-dir Migrations
```

### 4. Start the API

```bash
dotnet run --project WeatherApp.Api
```

API will be available at **http://localhost:5000**.  
Swagger UI: http://localhost:5000/swagger

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

The default `VITE_API_BASE_URL=http://localhost:5000` is already correct for local dev.

### 3. Start dev server

```bash
npm run dev
```

Frontend will be available at **http://localhost:5173**.

---

## Running Tests

### Backend tests

```bash
cd backend
dotnet test --logger "console;verbosity=normal"
```

### Frontend tests

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
  "searchedAt": "2024-01-15T10:30:00Z"
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

## Azure Deployment Notes

- All secrets are injected as **App Service Application Settings** (they override appsettings.json).
- Set `ConnectionStrings__DefaultConnection` and `OpenWeatherMap__ApiKey` in Azure App Service config.
- Set `AllowedOrigins__0` to your Azure Static Web App or CDN URL for CORS.
- The frontend reads `VITE_API_BASE_URL` at **build time** — set it in the Azure DevOps pipeline variable group and pass it as a build argument.
