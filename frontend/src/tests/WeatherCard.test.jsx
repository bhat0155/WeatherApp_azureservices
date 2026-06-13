import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import WeatherCard from '../components/WeatherCard';

const sampleWeather = {
  id: 1,
  city: 'London',
  country: 'GB',
  temperature: 15.7,
  feelsLike: 13.2,
  humidity: 80,
  description: 'clear sky',
  iconCode: '01d',
  searchedAt: new Date().toISOString(),
};

describe('WeatherCard', () => {
  it('renders nothing when weather is null', () => {
    const { container } = render(<WeatherCard weather={null} />);
    expect(container.firstChild).toBeNull();
  });

  it('displays city and country', () => {
    render(<WeatherCard weather={sampleWeather} />);
    expect(screen.getByText('London')).toBeInTheDocument();
    expect(screen.getByText('GB')).toBeInTheDocument();
  });

  it('displays rounded temperature', () => {
    render(<WeatherCard weather={sampleWeather} />);
    expect(screen.getByText('16°C')).toBeInTheDocument();
  });

  it('displays feels like temperature', () => {
    render(<WeatherCard weather={sampleWeather} />);
    expect(screen.getByText('13°C')).toBeInTheDocument();
  });

  it('displays humidity', () => {
    render(<WeatherCard weather={sampleWeather} />);
    expect(screen.getByText('80%')).toBeInTheDocument();
  });

  it('displays weather description', () => {
    render(<WeatherCard weather={sampleWeather} />);
    expect(screen.getByText('clear sky')).toBeInTheDocument();
  });

  it('renders weather icon with correct src', () => {
    render(<WeatherCard weather={sampleWeather} />);
    const img = screen.getByRole('img');
    expect(img).toHaveAttribute('src', 'https://openweathermap.org/img/wn/01d@2x.png');
    expect(img).toHaveAttribute('alt', 'clear sky');
  });

  it('does not render icon when iconCode is missing', () => {
    const weather = { ...sampleWeather, iconCode: '' };
    render(<WeatherCard weather={weather} />);
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });
});
