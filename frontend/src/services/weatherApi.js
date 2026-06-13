import axios from 'axios';

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

const api = axios.create({
  baseURL: `${BASE_URL}/api`,
  timeout: 10000,
});

/**
 * Fetch current weather for a city and persist it to history.
 * @param {string} city
 * @returns {Promise<WeatherResponseDto>}
 */
export async function fetchWeather(city) {
  const { data } = await api.get(`/weather/${encodeURIComponent(city)}`);
  return data;
}

/**
 * Retrieve the last 10 weather searches.
 * @returns {Promise<WeatherResponseDto[]>}
 */
export async function fetchHistory() {
  const { data } = await api.get('/weather/history');
  return data;
}

/**
 * Delete all weather history records.
 * @returns {Promise<void>}
 */
export async function clearHistory() {
  await api.delete('/weather/history');
}

export default api;
