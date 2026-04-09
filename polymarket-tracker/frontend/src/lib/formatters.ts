import { formatDistanceToNow, format } from 'date-fns';

export function formatPrice(price: number): string {
  return `${(price * 100).toFixed(1)}%`;
}

export function formatVolume(volume: number): string {
  if (volume >= 1_000_000) return `$${(volume / 1_000_000).toFixed(1)}M`;
  if (volume >= 1_000) return `$${(volume / 1_000).toFixed(1)}K`;
  return `$${volume.toFixed(0)}`;
}

export function formatRelativeTime(dateStr: string): string {
  return formatDistanceToNow(new Date(dateStr), { addSuffix: true });
}

export function formatTimestamp(dateStr: string): string {
  return format(new Date(dateStr), 'MMM d, yyyy HH:mm');
}

export function toneToColor(tone: number | null): string {
  if (tone === null) return 'text-gray-400';
  if (tone > 2) return 'text-green-400';
  if (tone < -2) return 'text-red-400';
  return 'text-yellow-400';
}

export function toneToLabel(tone: number | null): string {
  if (tone === null) return 'Neutral';
  if (tone > 2) return 'Positive';
  if (tone < -2) return 'Negative';
  return 'Neutral';
}
