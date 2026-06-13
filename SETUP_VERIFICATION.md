# Setup Verification Checklist

Run these commands in order to confirm everything works end-to-end.

---

## 1. Backend: Build

```bash
cd backend
dotnet build
```

**Expected:** `Build succeeded.`

---

## 2. Backend: Run unit tests

```bash
cd backend
dotnet test --logger "console;verbosity=normal"
```

**Expected:** All tests pass (≥15 tests).

---

## 3. Backend: Create appsettings.Development.json

```bash
cp WeatherApp.Api/appsettings.Development.json.example WeatherApp.Api/appsettings.Development.json
# Edit the file: set your real OpenWeatherMap API key
```

---

## 4. Backend: Run EF migrations

```bash
cd WeatherApp.Api
dotnet ef database update
```

**Expected:** `Done.` (database created in LocalDB)

---

## 5. Backend: Start API

```bash
dotnet run --project WeatherApp.Api --launch-profile http
```

**Expected:** `Now listening on: http://localhost:5000`

---

## 6. Backend: Health check (new terminal)

```bash
curl http://localhost:5000/api/health
```

**Expected:** `{"status":"healthy","timestamp":"..."}`

---

## 7. Backend: Fetch weather

```bash
curl "http://localhost:5000/api/weather/London"
```

**Expected:** JSON with city, temperature, humidity, etc.

---

## 8. Backend: Check history

```bash
curl "http://localhost:5000/api/weather/history"
```

**Expected:** JSON array with the London result from step 7.

---

## 9. Frontend: Install and start

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

**Expected:** `Local: http://localhost:5173/`

---

## 10. Frontend: Run tests

```bash
cd frontend
npm test
```

**Expected:** All tests pass (≥10 tests).

---

## 11. Full flow smoke test

1. Open http://localhost:5173 in a browser.
2. Type `London` in the search box and click **Search**.
3. Confirm weather card appears with temperature, humidity, description, and icon.
4. Search for `Tokyo`.
5. Confirm "Recent Searches" section shows both cities.
6. Click a city in history to re-search it.
7. Click **Clear** — history should empty.

---

## All green? You're ready for Azure DevOps.
