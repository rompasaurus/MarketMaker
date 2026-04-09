import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import type { Trade } from '@/types/trade';
import { getTradesByMarket, getRecentTrades } from '@/api/trades';
import { TradeRow } from './TradeRow';

interface LiveTradeFeedProps {
  marketId?: number;
  liveTrades: Trade[];
}

export function LiveTradeFeed({ marketId, liveTrades }: LiveTradeFeedProps) {
  const { data: historicalTrades } = useQuery({
    queryKey: ['trades', marketId],
    queryFn: () =>
      marketId ? getTradesByMarket(marketId) : getRecentTrades(),
  });

  const [allTrades, setAllTrades] = useState<Trade[]>([]);

  useEffect(() => {
    const historical = historicalTrades ?? [];
    const merged = [...liveTrades, ...historical];

    // Deduplicate by id (live trades have id=0, so also dedupe by timestamp+price)
    const seen = new Set<string>();
    const unique = merged.filter((t) => {
      const key = t.id > 0 ? `id:${t.id}` : `${t.timestamp}:${t.price}:${t.outcome}`;
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    });

    setAllTrades(unique.slice(0, 100));
  }, [liveTrades, historicalTrades]);

  return (
    <div className="rounded-xl border border-gray-800 bg-gray-900">
      <div className="border-b border-gray-800 px-4 py-3">
        <h3 className="text-sm font-semibold text-gray-300">
          {marketId ? 'Market Trades' : 'Recent Trades'}
        </h3>
      </div>
      <div className="max-h-96 overflow-y-auto">
        {allTrades.length === 0 ? (
          <p className="p-4 text-center text-sm text-gray-600">No trades yet</p>
        ) : (
          allTrades.map((trade, i) => <TradeRow key={`${trade.id}-${i}`} trade={trade} />)
        )}
      </div>
    </div>
  );
}
