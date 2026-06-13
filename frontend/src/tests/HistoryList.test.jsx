import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import HistoryList from '../components/HistoryList';

const sampleHistory = [
  { id: 1, city: 'London', country: 'GB', temperature: 15, feelsLike: 13, humidity: 80, description: 'clear', iconCode: '01d', searchedAt: new Date().toISOString() },
  { id: 2, city: 'Paris', country: 'FR', temperature: 20, feelsLike: 18, humidity: 70, description: 'sunny', iconCode: '01d', searchedAt: new Date().toISOString() },
];

describe('HistoryList', () => {
  it('renders empty state when history is empty', () => {
    render(<HistoryList history={[]} onSelect={() => {}} onClear={() => {}} />);
    expect(screen.getByText('No recent searches yet.')).toBeInTheDocument();
  });

  it('renders history items when history exists', () => {
    render(<HistoryList history={sampleHistory} onSelect={() => {}} onClear={() => {}} />);
    expect(screen.getByText(/London/)).toBeInTheDocument();
    expect(screen.getByText(/Paris/)).toBeInTheDocument();
  });

  it('calls onSelect with city name when a history item is clicked', () => {
    const onSelect = vi.fn();
    render(<HistoryList history={sampleHistory} onSelect={onSelect} onClear={() => {}} />);

    fireEvent.click(screen.getByText(/London/));
    expect(onSelect).toHaveBeenCalledWith('London');
  });

  it('calls onClear when clear button is clicked', () => {
    const onClear = vi.fn();
    render(<HistoryList history={sampleHistory} onSelect={() => {}} onClear={onClear} />);

    fireEvent.click(screen.getByText('Clear'));
    expect(onClear).toHaveBeenCalledTimes(1);
  });

  it('does not show clear button when history is empty', () => {
    render(<HistoryList history={[]} onSelect={() => {}} onClear={() => {}} />);
    expect(screen.queryByText('Clear')).not.toBeInTheDocument();
  });
});
