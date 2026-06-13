import { useState, useCallback, useEffect } from 'react';
import { fetchWeather, fetchHistory, clearHistory } from '../services/weatherApi';

export function useWeather() {
  const [weather, setWeather] = useState(null);
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const loadHistory = useCallback(async () => {
    try {
      const data = await fetchHistory();
      setHistory(data);
    } catch {
      // History failure is non-fatal — keep showing whatever was loaded before
    }
  }, []);

  useEffect(() => {
    loadHistory();
  }, [loadHistory]);

  const search = useCallback(async (city) => {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchWeather(city);
      setWeather(data);
      await loadHistory();
    } catch (err) {
      const message =
        err?.response?.data?.error ||
        err?.message ||
        'Something went wrong. Please try again.';
      setError(message);
      setWeather(null);
    } finally {
      setLoading(false);
    }
  }, [loadHistory]);

  const handleClearHistory = useCallback(async () => {
    try {
      await clearHistory();
      setHistory([]);
    } catch {
      setError('Failed to clear history.');
    }
  }, []);

  return { weather, history, loading, error, search, clearHistory: handleClearHistory };
}
