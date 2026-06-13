import styles from './HistoryList.module.css';

/**
 * @param {{ history: WeatherResponseDto[], onSelect: (city: string) => void, onClear: () => void }} props
 */
export default function HistoryList({ history = [], onSelect, onClear }) {
  if (history.length === 0) {
    return (
      <div className={styles.container}>
        <h3 className={styles.heading}>Recent Searches</h3>
        <p className={styles.empty}>No recent searches yet.</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.headerRow}>
        <h3 className={styles.heading}>Recent Searches</h3>
        <button className={styles.clearBtn} onClick={onClear} type="button">
          Clear
        </button>
      </div>
      <ul className={styles.list}>
        {history.map((item) => (
          <li key={item.id} className={styles.item}>
            <button
              className={styles.cityBtn}
              onClick={() => onSelect(item.city)}
              type="button"
            >
              <span className={styles.cityName}>{item.city}, {item.country}</span>
              <span className={styles.temp}>{Math.round(item.temperature)}°C</span>
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}
