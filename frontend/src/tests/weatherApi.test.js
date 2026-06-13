import { describe, it, expect, vi, beforeEach } from 'vitest';

// Mock axios before importing the module
vi.mock('axios', () => {
  const mockApi = {
    get: vi.fn(),
    delete: vi.fn(),
  };
  return {
    default: {
      create: vi.fn(() => mockApi),
    },
    __mockApi: mockApi,
  };
});

// We need to mock import.meta.env before importing the module
vi.stubEnv('VITE_API_BASE_URL', 'http://localhost:5000');

describe('weatherApi', () => {
  let axiosMock;

  beforeEach(async () => {
    vi.resetModules();
    const axiosModule = await import('axios');
    axiosMock = axiosModule.__mockApi || axiosModule.default.create();
    axiosMock.get = vi.fn();
    axiosMock.delete = vi.fn();
  });

  it('fetchWeather makes GET request to correct endpoint', async () => {
    const mockData = { city: 'London', country: 'GB', temperature: 15 };
    axiosMock.get.mockResolvedValueOnce({ data: mockData });

    const { fetchWeather } = await import('../services/weatherApi');
    const result = await fetchWeather('London');

    expect(axiosMock.get).toHaveBeenCalledWith('/weather/London');
    expect(result).toEqual(mockData);
  });

  it('fetchHistory makes GET request to history endpoint', async () => {
    const mockHistory = [{ id: 1, city: 'London' }];
    axiosMock.get.mockResolvedValueOnce({ data: mockHistory });

    const { fetchHistory } = await import('../services/weatherApi');
    const result = await fetchHistory();

    expect(axiosMock.get).toHaveBeenCalledWith('/weather/history');
    expect(result).toEqual(mockHistory);
  });

  it('clearHistory makes DELETE request to history endpoint', async () => {
    axiosMock.delete.mockResolvedValueOnce({});

    const { clearHistory } = await import('../services/weatherApi');
    await clearHistory();

    expect(axiosMock.delete).toHaveBeenCalledWith('/weather/history');
  });

  it('fetchWeather encodes special characters in city name', async () => {
    axiosMock.get.mockResolvedValueOnce({ data: {} });

    const { fetchWeather } = await import('../services/weatherApi');
    await fetchWeather('New York');

    expect(axiosMock.get).toHaveBeenCalledWith('/weather/New%20York');
  });
});
