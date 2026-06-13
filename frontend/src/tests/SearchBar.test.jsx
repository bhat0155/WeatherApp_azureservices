import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import SearchBar from '../components/SearchBar';

describe('SearchBar', () => {
  it('renders input and button', () => {
    render(<SearchBar onSearch={() => {}} />);
    expect(screen.getByPlaceholderText('Enter city name...')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument();
  });

  it('calls onSearch with trimmed city name when form is submitted', async () => {
    const onSearch = vi.fn();
    render(<SearchBar onSearch={onSearch} />);

    await userEvent.type(screen.getByPlaceholderText('Enter city name...'), 'London');
    fireEvent.submit(screen.getByRole('search'));

    expect(onSearch).toHaveBeenCalledWith('London');
    expect(onSearch).toHaveBeenCalledTimes(1);
  });

  it('does not call onSearch when input is empty', async () => {
    const onSearch = vi.fn();
    render(<SearchBar onSearch={onSearch} />);

    fireEvent.submit(screen.getByRole('search'));

    expect(onSearch).not.toHaveBeenCalled();
  });

  it('does not call onSearch when input is only whitespace', async () => {
    const onSearch = vi.fn();
    render(<SearchBar onSearch={onSearch} />);

    await userEvent.type(screen.getByPlaceholderText('Enter city name...'), '   ');
    fireEvent.submit(screen.getByRole('search'));

    expect(onSearch).not.toHaveBeenCalled();
  });

  it('disables input and button when disabled prop is true', () => {
    render(<SearchBar onSearch={() => {}} disabled={true} />);
    expect(screen.getByPlaceholderText('Enter city name...')).toBeDisabled();
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('shows "Searching..." label when disabled', () => {
    render(<SearchBar onSearch={() => {}} disabled={true} />);
    expect(screen.getByRole('button')).toHaveTextContent('Searching...');
  });
});
