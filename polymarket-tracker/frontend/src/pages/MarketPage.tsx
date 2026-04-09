import { useState } from 'react';
import { useParams } from 'react-router-dom';
import type { HubConnection } from '@microsoft/signalr';
import { useMarket } from '@/hooks/useMarkets';
import { usePriceHistory } from '@/hooks/usePriceHistory';
import { useMarketStream } from '@/hooks/useMarketStream';
import { MarketDetail } from '@/components/markets/MarketDetail';
import { OddsChart } from '@/components/charts/OddsChart';
import { ChartControls } from '@/components/charts/ChartControls';
import { LiveTradeFeed } from '@/components/trades/LiveTradeFeed';
import { NewsFeed } from '@/components/news/NewsFeed';
import type { Timeframe } from '@/lib/constants';

interface MarketPageProps {
  connection: HubConnection | null;
}

export function MarketPage({ connection }: MarketPageProps) {
  const { id } = useParams<{ id: string }>();
  const marketId = Number(id);
  const [timeframe, setTimeframe] = useState<Timeframe>('24h');

  const { data: market, isLoading, error } = useMarket(marketId);
  const { data: priceHistory } = usePriceHistory(marketId, timeframe);
  const { trades, latestPrice } = useMarketStream(connection, market?.conditionId);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-indigo-500 border-t-transparent" />
      </div>
    );
  }

  if (error || !market) {
    return (
      <div className="rounded-lg border border-red-900 bg-red-950/50 p-4 text-sm text-red-400">
        Market not found.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <MarketDetail market={market} />

      <div>
        <div className="mb-3 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-gray-300">Price History</h3>
          <ChartControls selected={timeframe} onSelect={setTimeframe} />
        </div>
        <OddsChart
          history={priceHistory?.history ?? []}
          latestPrice={latestPrice}
        />
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <LiveTradeFeed marketId={marketId} liveTrades={trades} />
        <NewsFeed marketId={marketId} />
      </div>
    </div>
  );
}
