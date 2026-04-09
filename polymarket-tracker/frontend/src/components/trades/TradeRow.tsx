import type { Trade } from '@/types/trade';
import { formatPrice, formatRelativeTime } from '@/lib/formatters';

interface TradeRowProps {
  trade: Trade;
}

export function TradeRow({ trade }: TradeRowProps) {
  const isYes = trade.outcome === 'YES';
  const isBuy = trade.side === 'BUY';

  return (
    <div className="flex items-center gap-3 border-b border-gray-800/50 px-3 py-2 text-sm">
      <span
        className={`w-10 rounded px-1.5 py-0.5 text-center text-xs font-bold ${
          isYes ? 'bg-green-900/50 text-green-400' : 'bg-red-900/50 text-red-400'
        }`}
      >
        {trade.outcome}
      </span>
      <span className={`w-10 text-xs ${isBuy ? 'text-green-400' : 'text-red-400'}`}>
        {trade.side}
      </span>
      <span className="text-gray-200">{formatPrice(trade.price)}</span>
      <span className="text-gray-500">${trade.size.toFixed(2)}</span>
      <span className="ml-auto text-xs text-gray-600">
        {formatRelativeTime(trade.timestamp)}
      </span>
    </div>
  );
}
