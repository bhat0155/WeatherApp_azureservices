import { useState } from 'react';
import styles from './SearchBar.module.css';

/**
 * @param {{ onSearch: (city: string) => void, disabled: boolean }} props
 */
export default function SearchBar({ onSearch, disabled = false }) {
  const [city, setCity] = useState('');

  function handleSubmit(e) {
    e.preventDefault();
    const trimmed = city.trim();
    if (!trimmed) return;
    onSearch(trimmed);
  }

  return (
    <form className={styles.form} onSubmit={handleSubmit} role="search">
      <input
        className={styles.input}
        type="text"
        value={city}
        onChange={(e) => setCity(e.target.value)}
        placeholder="Enter city name..."
        aria-label="City name"
        disabled={disabled}
      />
      <button
        className={styles.button}
        type="submit"
        disabled={disabled || !city.trim()}
      >
        {disabled ? 'Searching...' : 'Search'}
      </button>
    </form>
  );
}
