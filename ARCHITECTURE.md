# WeatherApp — Architecture Deep Dive

---

## 1. The 10,000-Foot View

```
┌─────────────────────────────────────────────────────────────────┐
│                          USER'S BROWSER                         │
│                                                                 │
│   Types "London" → clicks Search                                │
└───────────────────────────┬─────────────────────────────────────┘
                            │  HTTP GET /api/weather/London
                            │  (axios, port 5050)
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                     ASP.NET CORE API                            │
│                      localhost:5050                             │
│                                                                 │
│   Receives request → validates → calls OpenWeatherMap           │
│   → saves to DB → returns JSON                                  │
└────────────┬──────────────────────────────┬────────────────────┘
             │                              │
             │ EF Core (SQL)                │ HttpClient (HTTPS)
             ▼                              ▼
┌────────────────────────┐    ┌─────────────────────────────────┐
│   SQL SERVER (Docker)  │    │     OPENWEATHERMAP API          │
│   localhost:1433       │    │     api.openweathermap.org      │
│   WeatherAppDb         │    │     (external, free tier)       │
│   WeatherRecords table │    └─────────────────────────────────┘
└────────────────────────┘
```

**In plain English:**
The browser talks only to our API. Our API talks to two things — the database (to save/read history) and OpenWeatherMap (to get live weather). The browser never talks directly to the database or OpenWeatherMap. Our API is the single gatekeeper.

---

## 2. Frontend Architecture

```
frontend/src/
│
├── main.jsx                   Entry point. Mounts <App /> into index.html's
│                              <div id="root">. One line of real work.
│
├── App.jsx                    Root component. Owns the layout.
│                              Calls useWeather() hook to get all state.
│                              Passes state down to child components as props.
│
├── hooks/
│   └── useWeather.js          The brain of the frontend.
│                              ┌─────────────────────────────────┐
│                              │  State managed here:            │
│                              │  - weather (current result)     │
│                              │  - history (last 10 searches)   │
│                              │  - loading (boolean)            │
│                              │  - error (string or null)       │
│                              │                                 │
│                              │  Functions exposed:             │
│                              │  - search(city)                 │
│                              │  - clearHistory()               │
│                              └─────────────────────────────────┘
│                              Calls weatherApi.js for all HTTP.
│                              On mount: auto-loads history.
│
├── services/
│   └── weatherApi.js          Pure HTTP layer. Nothing else.
│                              axios instance with baseURL from .env
│                              Three functions:
│                              - fetchWeather(city)  → GET /api/weather/:city
│                              - fetchHistory()      → GET /api/weather/history
│                              - clearHistory()      → DELETE /api/weather/history
│
└── components/
    ├── SearchBar.jsx           Input + button. Controlled component.
    │                          Fires onSearch(city) prop. Blocks empty submit.
    │                          Shows "Searching..." when disabled.
    │
    ├── WeatherCard.jsx         Displays one weather result.
    │                          City, country, temp, feels like,
    │                          humidity, description, OWM icon image.
    │                          Returns null if no weather yet.
    │
    ├── HistoryList.jsx         Renders last 10 searches as clickable buttons.
    │                          Clicking a city re-triggers search.
    │                          Shows empty state if no history.
    │                          Has a Clear button.
    │
    └── ErrorMessage.jsx        Renders only when error prop is non-null.
                               Red alert box. Accessible (role="alert").
```

### How data flows in the frontend

```
User types "London" and clicks Search
            │
            ▼
      SearchBar.jsx
      calls onSearch("London")
            │
            ▼
      App.jsx receives it
      passes to search() from useWeather hook
            │
            ▼
      useWeather.js
      sets loading = true, error = null
      calls weatherApi.fetchWeather("London")
            │
            ▼
      weatherApi.js
      axios.get("/api/weather/London")
      ──────────────────────────────► [API responds with JSON]
            │
            ▼
      useWeather.js
      sets weather = response data
      calls weatherApi.fetchHistory() to refresh list
      sets loading = false
            │
            ▼
      App.jsx re-renders
      passes weather → WeatherCard (shows result)
      passes history → HistoryList (shows updated list)
```

---

## 3. Backend Architecture

```
HTTP Request arrives
        │
        ▼
┌───────────────────────────────────────────────────────┐
│               MIDDLEWARE PIPELINE                     │
│                                                       │
│  1. GlobalExceptionMiddleware  ← wraps everything     │
│     catches all unhandled exceptions                  │
│     maps them to clean JSON error responses           │
│                                                       │
│  2. CORS Middleware            ← checks Origin header │
│     only allows requests from configured origins      │
│     (localhost:5173 in dev, Azure URL in prod)        │
│                                                       │
│  3. Routing → Controller                              │
└───────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────┐
│                   CONTROLLERS                         │
│                                                       │
│  WeatherController                                    │
│  ├── GET  /api/weather/{city}  → GetWeather()         │
│  ├── GET  /api/weather/history → GetHistory()         │
│  └── DELETE /api/weather/history → ClearHistory()     │
│                                                       │
│  HealthController                                     │
│  └── GET  /api/health          → Get()                │
│                                                       │
│  Responsibility: validate input, call service,        │
│  return correct HTTP status code. Nothing else.       │
└───────────────────────────────────────────────────────┘
        │
        │  calls (via Dependency Injection)
        ▼
┌───────────────────────────────────────────────────────┐
│                    SERVICE LAYER                      │
│                                                       │
│  WeatherService                                       │
│  ├── GetWeatherAsync(city)                            │
│  │   1. Builds OWM API URL with ApiKey from config    │
│  │   2. HttpClient.GetAsync(url)                      │
│  │   3. If 404 → throw CityNotFoundException          │
│  │   4. If network error → throw ExternalServiceEx   │
│  │   5. Parse JSON response manually                  │
│  │   6. Build WeatherRecord entity                    │
│  │   7. repository.SaveAsync(record)                  │
│  │   8. Return WeatherResponseDto                     │
│  │                                                    │
│  ├── GetHistoryAsync()                                │
│  │   repository.GetHistoryAsync() → map to DTOs       │
│  │                                                    │
│  └── ClearHistoryAsync()                              │
│      repository.ClearHistoryAsync()                   │
│                                                       │
│  Responsibility: all business logic lives here.       │
│  Knows about external APIs and the database.          │
└───────────────────────────────────────────────────────┘
        │
        │  calls (via Dependency Injection)
        ▼
┌───────────────────────────────────────────────────────┐
│                  REPOSITORY LAYER                     │
│                                                       │
│  WeatherRepository                                    │
│  ├── SaveAsync(record)                                │
│  │   Sets SearchedAt = DateTime.UtcNow                │
│  │   db.WeatherRecords.Add(record)                    │
│  │   await db.SaveChangesAsync()                      │
│  │                                                    │
│  ├── GetHistoryAsync(count = 10)                      │
│  │   db.WeatherRecords                                │
│  │      .OrderByDescending(r => r.SearchedAt)         │
│  │      .Take(10)                                     │
│  │      .ToListAsync()                                │
│  │                                                    │
│  └── ClearHistoryAsync()                              │
│      RemoveRange all records → SaveChangesAsync()     │
│                                                       │
│  Responsibility: ONLY talks to the database.          │
│  No business logic. No HTTP calls.                    │
└───────────────────────────────────────────────────────┘
        │
        │  EF Core translates C# to SQL
        ▼
┌───────────────────────────────────────────────────────┐
│               AppDbContext (EF Core)                  │
│                                                       │
│  Registered in DI as scoped (one per HTTP request)    │
│  Reads connection string from appsettings             │
│  Manages WeatherRecords DbSet                         │
│  Tracks changes, generates SQL, executes queries      │
└───────────────────────────────────────────────────────┘
        │
        │  SQL over TCP port 1433
        ▼
┌───────────────────────────────────────────────────────┐
│             SQL SERVER (Docker / Azure SQL)           │
│                                                       │
│  Database: WeatherAppDb                               │
│  Table: WeatherRecords                                │
│  ┌────┬──────────┬─────────┬─────────┬─────────────┐ │
│  │ Id │ City     │ Country │ TempC   │ SearchedAt  │ │
│  ├────┼──────────┼─────────┼─────────┼─────────────┤ │
│  │  1 │ London   │ GB      │ 15.5    │ 2026-06-12  │ │
│  │  2 │ Ottawa   │ CA      │  8.2    │ 2026-06-12  │ │
│  │  3 │ Tokyo    │ JP      │ 28.1    │ 2026-06-12  │ │
│  └────┴──────────┴─────────┴─────────┴─────────────┘ │
└───────────────────────────────────────────────────────┘
```

---

## 4. The Dependency Injection (DI) Container

This is the most important .NET concept in the whole app. Everything is wired here in `Program.cs` at startup:

```
Program.cs registers:
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  AppDbContext         → scoped   (new instance per request)  │
│  IWeatherRepository   → scoped   (new instance per request)  │
│  IWeatherService      → via HttpClient factory               │
│  OpenWeatherMapOptions → singleton (config, read once)       │
│  CORS policy          → singleton                            │
│                                                              │
└──────────────────────────────────────────────────────────────┘

When GET /api/weather/London arrives:
  .NET creates → WeatherController
                    needs IWeatherService
                    .NET creates → WeatherService
                                      needs IWeatherRepository
                                      .NET creates → WeatherRepository
                                                         needs AppDbContext
                                                         .NET creates → AppDbContext
                                      needs HttpClient → already pooled
                                      needs IOptions<OWMOptions> → from config
                    needs ILogger → from logging system

All of this happens automatically. You never call `new`.
```

**Why does this matter?**
In tests, you replace `IWeatherRepository` with a `Mock<IWeatherRepository>`. The controller and service have no idea — they just see the interface. The real database is never touched. Tests run in milliseconds.

---

## 5. Configuration Flow

```
appsettings.json              ← base config, committed to repo
      +
appsettings.Development.json  ← local overrides, GITIGNORED
      +
Environment Variables         ← injected by Azure App Service in prod
      │
      │  .NET merges all three. Later source wins.
      ▼
IConfiguration (available everywhere via DI)
      │
      ├── GetConnectionString("DefaultConnection")
      │     └── passed to AppDbContext → SQL Server
      │
      └── GetSection("OpenWeatherMap")
            └── bound to OpenWeatherMapOptions
                  └── injected into WeatherService
                        └── used to build API URL
```

**The Azure override trick:**
Azure App Service lets you set "Application Settings" in the portal. It converts them to environment variables. .NET automatically reads environment variables with `__` as a nested separator:

```
Portal setting:  OpenWeatherMap__ApiKey = abc123
.NET reads it as: config["OpenWeatherMap:ApiKey"] = "abc123"
Overrides whatever is in appsettings.json silently.
```

No code changes. No redeployment. Change a secret in the portal, restart the app, done.

---

## 6. Error Handling Flow

```
Request: GET /api/weather/FakeCityXYZ
         │
         ▼
WeatherController.GetWeather("FakeCityXYZ")
         │
         ▼
WeatherService.GetWeatherAsync("FakeCityXYZ")
         │
         ▼
HttpClient calls OpenWeatherMap
         │
         ▼
OpenWeatherMap returns HTTP 404
         │
         ▼
WeatherService checks: response.StatusCode == NotFound
  → throws new CityNotFoundException("City 'FakeCityXYZ' was not found.")
         │
         │  exception bubbles up through service → controller
         │  controller does NOT catch it
         │  exception reaches middleware
         ▼
GlobalExceptionMiddleware catches CityNotFoundException
  → sets response status = 404
  → writes body:
    {
      "error": "City 'FakeCityXYZ' was not found.",
      "statusCode": 404
    }
         │
         ▼
Browser receives clean 404 JSON
useWeather hook sets error = "City 'FakeCityXYZ' was not found."
ErrorMessage component renders the red alert box
```

**Three exception types and their HTTP mappings:**
```
CityNotFoundException      → 404 Not Found
ExternalServiceException   → 503 Service Unavailable
Any other Exception        → 500 Internal Server Error
```

---

## 7. Database Schema and Migrations

```
WeatherRecord (C# Entity)              WeatherRecords (SQL Table)
──────────────────────────             ──────────────────────────────────
int Id                        ──►      Id          INT IDENTITY PK
string City (max 100)         ──►      City        NVARCHAR(100) NOT NULL
string Country (max 10)       ──►      Country     NVARCHAR(10)
double Temperature            ──►      Temperature FLOAT
double FeelsLike              ──►      FeelsLike   FLOAT
int Humidity                  ──►      Humidity    INT
string Description (max 200)  ──►      Description NVARCHAR(200)
string IconCode (max 10)      ──►      IconCode    NVARCHAR(10)
DateTime SearchedAt           ──►      SearchedAt  DATETIME2 DEFAULT GETUTCDATE()

EF Core reads the C# class → generates this exact SQL table
```

**Migration lifecycle:**
```
You change the C# entity (add a new field, change a max length)
            │
            ▼
dotnet ef migrations add SomeDescriptiveName
  → generates a C# migration file with Up() and Down() methods
  → Up() = SQL to apply the change
  → Down() = SQL to reverse it
            │
            ▼
dotnet ef database update
  → runs pending migrations against the database
  → records which migrations ran in __EFMigrationsHistory table
            │
            ▼
In Production (Azure):
  → Program.cs calls db.Database.Migrate() on startup
  → automatically applies any pending migrations
  → safe to run multiple times (skips already-applied ones)
```

---

## 8. The Full Request Lifecycle (Happy Path)

```
1. USER types "Tokyo" and clicks Search
   │
2. SearchBar fires onSearch("Tokyo")
   │
3. useWeather.search("Tokyo") called
   sets: loading=true, error=null
   │
4. weatherApi.fetchWeather("Tokyo")
   axios.get("http://localhost:5050/api/weather/Tokyo")
   │
5. Request hits GlobalExceptionMiddleware (passes through, no error yet)
   │
6. CORS middleware checks Origin: http://localhost:5173 ✓ allowed
   │
7. Router matches GET /api/weather/{city} → WeatherController.GetWeather("Tokyo")
   │
8. Controller: city is not empty ✓
   calls _weatherService.GetWeatherAsync("Tokyo")
   │
9. WeatherService:
   builds URL: https://api.openweathermap.org/data/2.5/weather?q=Tokyo&appid=KEY&units=metric
   calls HttpClient.GetAsync(url)
   │
10. OpenWeatherMap responds 200 OK with JSON:
    { "name": "Tokyo", "sys": { "country": "JP" },
      "main": { "temp": 28.1, "feels_like": 31.2, "humidity": 74 },
      "weather": [{ "description": "scattered clouds", "icon": "03d" }] }
   │
11. WeatherService parses JSON → builds WeatherRecord entity
    calls repository.SaveAsync(record)
   │
12. WeatherRepository:
    sets record.SearchedAt = DateTime.UtcNow
    db.WeatherRecords.Add(record)
    await db.SaveChangesAsync()
    EF Core executes: INSERT INTO WeatherRecords (City, Country, ...) VALUES (...)
    SQL Server assigns Id = 4, returns it
   │
13. WeatherService maps WeatherRecord → WeatherResponseDto
    (adds computed IconUrl field)
    returns DTO to controller
   │
14. Controller: return Ok(dto) → HTTP 200 with JSON body
   │
15. axios receives response
    useWeather sets: weather=data, loading=false
    calls fetchHistory() to refresh the list
   │
16. React re-renders:
    WeatherCard displays Tokyo weather with icon
    HistoryList shows Tokyo at the top of the list
```

---

## 9. Test Architecture

```
WeatherApp.Tests/
│
├── Services/WeatherServiceTests.cs
│   Tests WeatherService in complete isolation.
│   Real: WeatherService logic, JSON parsing, exception throwing
│   Mocked: HttpClient (returns fake JSON), IWeatherRepository (no DB)
│
│   Key tests:
│   ✓ Returns correct DTO when OWM API returns valid JSON
│   ✓ Throws CityNotFoundException when OWM returns 404
│   ✓ Throws ExternalServiceException when network is down
│   ✓ Throws ExternalServiceException when OWM returns 500
│   ✓ Calls repository.SaveAsync with correct city/country data
│   ✓ Maps all history records to DTOs correctly
│   ✓ Delegates ClearHistory to repository
│
├── Controllers/WeatherControllerTests.cs
│   Tests WeatherController in complete isolation.
│   Real: Controller routing logic, input validation, status codes
│   Mocked: IWeatherService (no business logic runs)
│
│   Key tests:
│   ✓ Returns 200 OK with DTO for valid city
│   ✓ Returns 400 Bad Request for empty string city
│   ✓ Returns 400 Bad Request for whitespace-only city
│   ✓ Propagates CityNotFoundException (middleware handles it)
│   ✓ Returns 200 with list for history endpoint
│   ✓ Returns 204 No Content for clear history
│   ✓ Health check returns 200 with timestamp
│
└── Repositories/WeatherRepositoryTests.cs
    Tests WeatherRepository with real EF Core, no Docker needed.
    Real: All EF Core query logic, ordering, pagination
    Database: EF Core InMemory provider (lives in RAM, per test)

    Key tests:
    ✓ SaveAsync assigns an Id greater than 0
    ✓ SaveAsync sets SearchedAt to UTC time
    ✓ GetHistoryAsync returns max 10 records
    ✓ GetHistoryAsync orders by SearchedAt descending (newest first)
    ✓ GetHistoryAsync returns empty list when no records exist
    ✓ ClearHistoryAsync removes all records
```

**The testing pyramid:**
```
        ▲
       /█\        E2E tests (not written — would use Playwright/Cypress)
      /███\       Tests full browser → API → DB flow
     /─────\
    /███████\     Integration tests (not written — would use WebApplicationFactory)
   /█████████\    Tests full API pipeline with real middleware
  /───────────\
 /█████████████\  Unit tests ← THIS IS WHAT WE BUILT (47 tests)
/███████████████\ Tests each layer in isolation, fast, no network/DB needed
─────────────────
```

---

## 10. Local Development Setup vs Production

```
LOCAL DEVELOPMENT                    AZURE PRODUCTION
─────────────────────────────────    ─────────────────────────────────
React on localhost:5173 (Vite)  →    React on Azure Static Web Apps
.NET API on localhost:5050      →    .NET API on Azure App Service
SQL Server in Docker :1433      →    Azure SQL Database
appsettings.Development.json    →    App Service Application Settings
Manual dotnet run               →    Azure DevOps deploys automatically
Manual npm run dev              →    Built by CI, served from CDN
docker start weatherapp-sql     →    Azure manages DB availability
```

---

## 11. The Deployment Pipeline (What Comes Next)

```
DEVELOPER                AZURE DEVOPS              AZURE CLOUD
──────────               ────────────              ───────────

git push main
      │
      │ triggers
      ▼
              ┌─────────────────┐
              │  BUILD PIPELINE │
              │  (CI)           │
              │                 │
              │ dotnet restore  │
              │ dotnet build    │
              │ dotnet test ────┼── if any of 20 tests fail → STOP
              │ dotnet publish  │
              │ npm install     │
              │ npm run build   │
              │ upload artifact │
              └────────┬────────┘
                       │ artifact (compiled app zip)
                       ▼
              ┌─────────────────┐
              │ RELEASE PIPELINE│
              │ (CD)            │
              │                 │
              │  ┌───────────┐  │        ┌─────────────────────┐
              │  │    DEV    │──┼──────► │ App Service: DEV    │
              │  │ (auto)    │  │        │ Azure SQL: Dev DB   │
              │  └─────┬─────┘  │        └─────────────────────┘
              │        │ passes  │
              │  ┌─────▼─────┐  │        ┌─────────────────────┐
              │  │ STAGING   │──┼──────► │ App Service: STG    │
              │  │ (auto)    │  │        │ Azure SQL: Stg DB   │
              │  └─────┬─────┘  │        └─────────────────────┘
              │        │ passes  │
              │  ┌─────▼─────┐  │
              │  │ APPROVAL  │  │  ◄── human gets email, reviews,
              │  │   GATE    │  │      clicks Approve in portal
              │  └─────┬─────┘  │
              │        │approved │
              │  ┌─────▼─────┐  │        ┌─────────────────────┐
              │  │PRODUCTION │──┼──────► │ App Service: PROD   │
              │  │ (manual)  │  │        │ Azure SQL: Prod DB  │
              │  └───────────┘  │        │ + Azure Monitor     │
              └─────────────────┘        │ + App Insights      │
                                         └─────────────────────┘
```

**What Azure Monitor watches after production deploy:**
```
Response time   → alert if > 2 seconds
Error rate      → alert if > 1% of requests fail
Exceptions      → every unhandled error logged with full stack trace
Dependencies    → tracks calls to SQL Server and OpenWeatherMap separately
Availability    → pings /api/health every 5 minutes from multiple regions
CPU / Memory    → alert if App Service is under resource pressure
```

---

## 12. Security Considerations

```
WHAT WE PROTECTED                HOW
─────────────────────────────    ──────────────────────────────────────
OpenWeatherMap API key           Never in repo. In appsettings.Development
                                 .json (gitignored) locally. In Azure App
                                 Service Application Settings in prod.

Database connection string       Same — gitignored locally, App Settings
                                 in Azure. Never hardcoded.

SQL Injection                    EF Core uses parameterized queries
                                 automatically. Raw SQL never written.

CORS                             Only allowed origins can call the API.
                                 Configured per environment.

Input validation                 Controller rejects empty/whitespace city.
                                 OWM API key never exposed to frontend.
                                 Frontend never talks to OWM directly.
```

---

## Quick Reference: Every File and Its Job

```
weather-app/
├── backend/
│   ├── WeatherApp.sln                    Solution file — groups both projects
│   ├── WeatherApp.Api/
│   │   ├── Program.cs                    Startup: DI registration, middleware, routing
│   │   ├── WeatherApp.Api.csproj         Dependencies (NuGet packages)
│   │   ├── appsettings.json              Base config (safe to commit)
│   │   ├── appsettings.Development.json  Local secrets (GITIGNORED)
│   │   ├── Properties/launchSettings.json Sets dev port to 5050
│   │   ├── Controllers/
│   │   │   ├── WeatherController.cs      4 endpoints for weather + history
│   │   │   └── HealthController.cs       GET /api/health
│   │   ├── Services/
│   │   │   ├── IWeatherService.cs        Interface (contract)
│   │   │   └── WeatherService.cs         Calls OWM, saves to DB, returns DTO
│   │   ├── Repositories/
│   │   │   ├── IWeatherRepository.cs     Interface (contract)
│   │   │   └── WeatherRepository.cs      All SQL via EF Core
│   │   ├── Data/
│   │   │   └── AppDbContext.cs           EF Core DB manager
│   │   ├── Entities/
│   │   │   └── WeatherRecord.cs          Maps to WeatherRecords SQL table
│   │   ├── DTOs/
│   │   │   └── WeatherResponseDto.cs     What the API sends to frontend
│   │   ├── Middleware/
│   │   │   └── GlobalExceptionMiddleware.cs  Catches all errors, formats JSON
│   │   ├── Configuration/
│   │   │   └── OpenWeatherMapOptions.cs  Typed config class for OWM settings
│   │   └── Migrations/
│   │       └── *_InitialCreate.cs        Auto-generated SQL for WeatherRecords table
│   └── WeatherApp.Tests/
│       ├── WeatherApp.Tests.csproj       Test dependencies (xUnit, Moq, FluentAssertions)
│       ├── Services/WeatherServiceTests.cs      7 tests
│       ├── Controllers/WeatherControllerTests.cs 8 tests
│       └── Repositories/WeatherRepositoryTests.cs 5 tests
│
├── frontend/
│   ├── index.html                        Vite entry HTML
│   ├── vite.config.js                    Vite + Vitest config
│   ├── package.json                      npm dependencies
│   ├── .env                              VITE_API_BASE_URL=http://localhost:5050
│   ├── .env.example                      Template (safe to commit)
│   └── src/
│       ├── main.jsx                      Mounts App into DOM
│       ├── App.jsx                       Root component, layout
│       ├── App.css                       Global styles, spinner
│       ├── hooks/useWeather.js           All state and API calls
│       ├── services/weatherApi.js        axios HTTP functions
│       ├── components/
│       │   ├── SearchBar.jsx + .module.css
│       │   ├── WeatherCard.jsx + .module.css
│       │   ├── HistoryList.jsx + .module.css
│       │   └── ErrorMessage.jsx + .module.css
│       └── tests/
│           ├── setup.js                  jest-dom matchers
│           ├── SearchBar.test.jsx        6 tests
│           ├── WeatherCard.test.jsx      8 tests
│           ├── HistoryList.test.jsx      5 tests
│           ├── ErrorMessage.test.jsx     4 tests
│           └── weatherApi.test.js        4 tests
│
├── .gitignore                            Excludes secrets, build output, node_modules
├── .env.example                          Root env template
├── README.md                             Setup instructions
└── SETUP_VERIFICATION.md                 End-to-end checklist
```
