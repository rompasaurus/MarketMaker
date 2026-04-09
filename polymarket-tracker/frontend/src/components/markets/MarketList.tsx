import { useMarkets } from '@/hooks/useMarkets';
import { useAppStore } from '@/stores/useAppStore';
import { MarketCard } from './MarketCard';

export function MarketList() {
  const searchQuery = useAppStore((s) => s.searchQuery);
  const { data: markets, isLoading, error } = useMarkets(searchQuery || undefined);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-indigo-500 border-t-transparent" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-900 bg-red-950/50 p-4 text-sm text-red-400">
        Failed to load markets. Is the backend running?
      </div>
    );
  }

  if (!markets?.length) {
    return (
      <div className="py-12 text-center text-gray-500">
        No markets found. Markets sync every 5 minutes.
      </div>
    );
  }

  return (
    <div className="grid gap-3">
      {markets.map((market) => (
        <MarketCard key={market.id} market={market} />
      ))}
    </div>
  );
}
