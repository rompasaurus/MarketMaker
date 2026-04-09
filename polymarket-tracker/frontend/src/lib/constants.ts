export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';
export const SIGNALR_URL = import.meta.env.VITE_SIGNALR_URL || '/hubs/market';

export const TIMEFRAMES = [
  { label: '1H', value: '1h' },
  { label: '24H', value: '24h' },
  { label: '7D', value: '7d' },
  { label: '30D', value: '30d' },
  { label: 'All', value: 'all' },
] as const;

export type Timeframe = (typeof TIMEFRAMES)[number]['value'];
