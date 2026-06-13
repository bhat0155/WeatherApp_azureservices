import styles from './ErrorMessage.module.css';

/**
 * @param {{ message: string | null }} props
 */
export default function ErrorMessage({ message }) {
  if (!message) return null;

  return (
    <div className={styles.error} role="alert">
      <span className={styles.icon}>!</span>
      <p className={styles.text}>{message}</p>
    </div>
  );
}
