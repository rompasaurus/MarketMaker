import { useQuery } from '@tanstack/react-query';
import { getPriceHistory } from '@/api/markets';
import type { Timeframe } from '@/lib/constants';

function getFromDate(timeframe: Timeframe): string | undefined {
  const now = new Date();
  switch (timeframe) {
    case '1h': return new Date(now.getTime() - 60 * 60 * 1000).toISOString();
    case '24h': return new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString();
    case '7d': return new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString();
    case '30d': return new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString();
    case 'all': return undefined;
  }
}

export function usePriceHistory(marketId: number, timeframe: Timeframe) {
  return useQuery({
    queryKey: ['priceHistory', marketId, timeframe],
    queryFn: () => getPriceHistory(marketId, getFromDate(timeframe)),
    refetchInterval: 30_000,
  });
}
