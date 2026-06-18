import SearchBar from './components/SearchBar';
import WeatherCard from './components/WeatherCard';
import HistoryList from './components/HistoryList';
import ErrorMessage from './components/ErrorMessage';
import { useWeather } from './hooks/useWeather';
import './App.css';

export default function App() {
  const { weather, history, loading, error, search, clearHistory } = useWeather();

  return (
    <div className="app-bg">
      <div className="card">
        <h1 className="title">Ekam's Weather App</h1>
        <p className="subtitle">Search for real-time weather in any city</p>

        <SearchBar onSearch={search} disabled={loading} />

        {loading && (
          <div className="spinner-wrap" aria-label="Loading">
            <div className="spinner" />
          </div>
        )}

        <ErrorMessage message={error} />
        <WeatherCard weather={weather} />
        <HistoryList history={history} onSelect={search} onClear={clearHistory} />
      </div>
    </div>
  );
}
