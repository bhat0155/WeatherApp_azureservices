# WeatherApp

A two-tier weather application built to showcase Azure DevOps CI/CD pipelines.

- **Backend**: ASP.NET Core 9 Web API + Entity Framework Core + SQL Server
- **Frontend**: React 18 + Vite
- **External data**: OpenWeatherMap API (free tier)
- **Tests**: 20 backend (xUnit) + 27 frontend (Vitest)

---

## Architecture

```
╔══════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                                        USER'S BROWSER                                               ║
║                                                                                                      ║
║   1. Types city name        2. Sees weather card          3. Sees recent searches                    ║
║      in search box             with temp, humidity,          (last 10, clickable)                    ║
║                                icon, description                                                     ║
╚══════════════════════╦═══════════════════════════════════════════════════════════════════════════════╝
                       ║
                       ║  axios HTTP requests
                       ║
╔══════════════════════╩═══════════════════════════════════════════════════════════════════════════════╗
║                              FRONTEND  —  React 18 + Vite  (localhost:5173)                         ║
║                                                                                                      ║
║   src/                                                                                               ║
║   ├── App.jsx                  Root layout. Calls useWeather() hook for all state.                   ║
║   │                                                                                                  ║
║   ├── hooks/useWeather.js      Brain of the frontend. Manages:                                       ║
║   │                            • weather (current result)   • loading (boolean)                      ║
║   │                            • history (last 10 items)    • error (string or null)                 ║
║   │                            Calls weatherApi.js for every HTTP interaction.                       ║
║   │                                                                                                  ║
║   ├── services/weatherApi.js   axios client. Base URL from VITE_API_BASE_URL env var.                ║
║   │                            fetchWeather(city)  → GET  /api/weather/:city                         ║
║   │                            fetchHistory()      → GET  /api/weather/history                       ║
║   │                            clearHistory()      → DELETE /api/weather/history                     ║
║   │                                                                                                  ║
║   └── components/                                                                                    ║
║       ├── SearchBar.jsx        Controlled input + submit button. Blocks empty input.                  ║
║       ├── WeatherCard.jsx      Displays city, country, temp, feels like, humidity,                   ║
║       │                        description, and OWM weather icon.                                    ║
║       ├── HistoryList.jsx      Renders up to 10 past searches. Each item re-triggers search.         ║
║       └── ErrorMessage.jsx     Shown only on error. role="alert" for accessibility.                  ║
║                                                                                                      ║
╚══════════════════════╦═══════════════════════════════════════════════════════════════════════════════╝
                       ║
         ┌─────────────╩──────────────────────────────────────┐
         │                                                     │
         │  GET /api/weather/London                            │  GET /api/weather/history
         │  GET /api/health                                    │  DELETE /api/weather/history
         │                                                     │
         ▼                                                     ▼
╔═════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                         BACKEND  —  ASP.NET Core 9 Web API  (localhost:5050)                        ║
║                                                                                                      ║
║  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐    ║
║  │  MIDDLEWARE PIPELINE  (every request passes through this in order)                          │    ║
║  │                                                                                             │    ║
║  │  1. GlobalExceptionMiddleware   Catches ALL unhandled exceptions from any layer.            │    ║
║  │                                 CityNotFoundException    → 404 + JSON error body            │    ║
║  │                                 ExternalServiceException → 503 + JSON error body            │    ║
║  │                                 Anything else           → 500 + JSON error body             │    ║
║  │                                 Always returns: { "error": "...", "statusCode": 404 }       │    ║
║  │                                                                                             │    ║
║  │  2. CORS Middleware             Checks the Origin header on every request.                  │    ║
║  │                                 Allows: http://localhost:5173 (dev)                         │    ║
║  │                                         https://your-azure-url (prod)                      │    ║
║  │                                 Blocks all other origins.                                   │    ║
║  │                                                                                             │    ║
║  │  3. Routing → Controller        Matches URL pattern to the right controller method.         │    ║
║  └─────────────────────────────────────────────────────────────────────────────────────────────┘    ║
║                                              │                                                       ║
║                                              ▼                                                       ║
║  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐    ║
║  │  CONTROLLERS  (receive HTTP, validate input, return HTTP status codes)                      │    ║
║  │                                                                                             │    ║
║  │  WeatherController                          HealthController                                │    ║
║  │  ├─ GET  /api/weather/{city}                └─ GET /api/health                             │    ║
║  │  │       • rejects empty/whitespace city          returns 200 + timestamp                  │    ║
║  │  │       • calls WeatherService                   used by Azure Monitor                    │    ║
║  │  │       • returns 200 OK + WeatherResponseDto                                             │    ║
║  │  │                                                                                         │    ║
║  │  ├─ GET  /api/weather/history                                                              │    ║
║  │  │       • calls WeatherService                                                            │    ║
║  │  │       • returns 200 OK + WeatherResponseDto[]                                           │    ║
║  │  │                                                                                         │    ║
║  │  └─ DELETE /api/weather/history                                                            │    ║
║  │          • calls WeatherService                                                            │    ║
║  │          • returns 204 No Content                                                          │    ║
║  └─────────────────────────────────────────────────────────────────────────────────────────────┘    ║
║                                              │                                                       ║
║                           injected by .NET Dependency Injection                                      ║
║                                              ▼                                                       ║
║  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐    ║
║  │  SERVICE LAYER  (all business logic lives here)                                             │    ║
║  │                                                                                             │    ║
║  │  WeatherService                                                                             │    ║
║  │  ├─ GetWeatherAsync(city)                                                                   │    ║
║  │  │       1. Build OWM URL using ApiKey from OpenWeatherMapOptions (IOptions pattern)        │    ║
║  │  │       2. HttpClient.GetAsync(url)  ─────────────────────────────────────────────────────╫──► ║
║  │  │       3. HTTP 404 from OWM? throw CityNotFoundException                                  ║    ║
║  │  │       4. Network down?     throw ExternalServiceException                                ║    ║
║  │  │       5. Parse JSON → build WeatherRecord entity                                         ║    ║
║  │  │       6. repository.SaveAsync(record) → persists to DB                                   ║    ║
║  │  │       7. Map entity → WeatherResponseDto → return                                        ║    ║
║  │  │                                                                                         │    ║
║  │  ├─ GetHistoryAsync()                                                                       │    ║
║  │  │       repository.GetHistoryAsync(10) → map to DTOs → return                             │    ║
║  │  │                                                                                         │    ║
║  │  └─ ClearHistoryAsync()                                                                     │    ║
║  │          repository.ClearHistoryAsync()                                                    │    ║
║  └─────────────────────────────────────────────────────────────────────────────────────────────┘    ║
║                                              │                                                       ║
║                           injected by .NET Dependency Injection                                      ║
║                                              ▼                                                       ║
║  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐    ║
║  │  REPOSITORY LAYER  (only layer that touches the database)                                   │    ║
║  │                                                                                             │    ║
║  │  WeatherRepository                                                                          │    ║
║  │  ├─ SaveAsync(record)         Sets SearchedAt=UtcNow, INSERT via EF Core                    │    ║
║  │  ├─ GetHistoryAsync(n=10)     SELECT TOP 10 ORDER BY SearchedAt DESC                        │    ║
║  │  └─ ClearHistoryAsync()       DELETE all rows                                               │    ║
║  └─────────────────────────────────────────────────────────────────────────────────────────────┘    ║
║                                              │                                                       ║
║                                    EF Core translates                                                ║
║                                    C# LINQ → SQL queries                                             ║
║                                              │                                                       ║
║  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐    ║
║  │  AppDbContext  (EF Core — one instance per HTTP request via DI)                             │    ║
║  │  Reads connection string from appsettings → manages WeatherRecords DbSet                    │    ║
║  │  Tracks changes, generates parameterized SQL, prevents SQL injection                        │    ║
║  └───────────────────────────────────────┬─────────────────────────────────────────────────────┘    ║
╚═══════════════════════════════════════════╬══════════════════════════════════════════════════════════╝
                                            ║
                                            ║  SQL over TCP port 1433
                                            ▼
╔═══════════════════════════════════════════════════════════════════════════════════════════════════╗
║                     DATABASE  —  SQL Server 2022  (Docker locally / Azure SQL in prod)            ║
║                                                                                                    ║
║   Database: WeatherAppDb                                                                           ║
║   Table: WeatherRecords                                                                            ║
║                                                                                                    ║
║   ┌────┬──────────────┬─────────┬─────────────┬───────────┬─────────────┬──────────┬───────────┐  ║
║   │ Id │ City         │ Country │ Temperature │ FeelsLike │ Humidity    │ IconCode │SearchedAt │  ║
║   ├────┼──────────────┼─────────┼─────────────┼───────────┼─────────────┼──────────┼───────────┤  ║
║   │  1 │ London       │ GB      │ 15.5        │ 13.0      │ 80          │ 01d      │ 2026-06.. │  ║
║   │  2 │ Ottawa       │ CA      │  8.2        │  5.1      │ 65          │ 13d      │ 2026-06.. │  ║
║   │  3 │ Tokyo        │ JP      │ 28.1        │ 31.2      │ 74          │ 03d      │ 2026-06.. │  ║
║   └────┴──────────────┴─────────┴─────────────┴───────────┴─────────────┴──────────┴───────────┘  ║
║                                                                                                    ║
║   Migration history tracked in __EFMigrationsHistory table (managed by EF Core automatically)     ║
╚═══════════════════════════════════════════════════════════════════════════════════════════════════╝

                                  ▲  (HttpClient call from WeatherService)
                                  │
╔═════════════════════════════════╩═════════════════════════════════════════════════════════════════╗
║                    EXTERNAL  —  OpenWeatherMap API  (api.openweathermap.org)                       ║
║                                                                                                    ║
║   Endpoint used:  GET /data/2.5/weather?q={city}&appid={key}&units=metric                         ║
║   Auth:           API key as query param (stored in appsettings, never exposed to browser)         ║
║   Response:       JSON with name, sys.country, main.temp, main.feels_like,                        ║
║                   main.humidity, weather[0].description, weather[0].icon                          ║
║   Error cases:    404 → city not found   |   5xx / timeout → service unavailable                  ║
╚═══════════════════════════════════════════════════════════════════════════════════════════════════╝
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
