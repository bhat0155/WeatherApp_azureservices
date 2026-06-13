import styles from './WeatherCard.module.css';

/**
 * @param {{ weather: WeatherResponseDto }} props
 */
export default function WeatherCard({ weather }) {
  if (!weather) return null;

  return (
    <div className={styles.card}>
      <div className={styles.header}>
        <div>
          <h2 className={styles.city}>{weather.city}</h2>
          <p className={styles.country}>{weather.country}</p>
        </div>
        {weather.iconCode && (
          <img
            src={`https://openweathermap.org/img/wn/${weather.iconCode}@2x.png`}
            alt={weather.description}
            className={styles.icon}
          />
        )}
      </div>

      <p className={styles.description}>{weather.description}</p>

      <div className={styles.stats}>
        <div className={styles.stat}>
          <span className={styles.label}>Temperature</span>
          <span className={styles.value}>{Math.round(weather.temperature)}°C</span>
        </div>
        <div className={styles.stat}>
          <span className={styles.label}>Feels Like</span>
          <span className={styles.value}>{Math.round(weather.feelsLike)}°C</span>
        </div>
        <div className={styles.stat}>
          <span className={styles.label}>Humidity</span>
          <span className={styles.value}>{weather.humidity}%</span>
        </div>
      </div>
    </div>
  );
}
