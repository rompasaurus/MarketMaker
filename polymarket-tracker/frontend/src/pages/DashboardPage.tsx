import type { HubConnection } from '@microsoft/signalr';
import { MarketList } from '@/components/markets/MarketList';
import { LiveTradeFeed } from '@/components/trades/LiveTradeFeed';
import { useMarketStream } from '@/hooks/useMarketStream';

interface DashboardPageProps {
  connection: HubConnection | null;
}

export function DashboardPage({ connection }: DashboardPageProps) {
  const { trades } = useMarketStream(connection);

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <div className="lg:col-span-2">
        <h2 className="mb-4 text-lg font-semibold text-white">Top Markets</h2>
        <MarketList />
      </div>
      <div>
        <h2 className="mb-4 text-lg font-semibold text-white">Live Trades</h2>
        <LiveTradeFeed liveTrades={trades} />
      </div>
    </div>
  );
}
