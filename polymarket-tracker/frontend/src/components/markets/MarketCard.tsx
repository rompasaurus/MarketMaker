import { Link } from 'react-router-dom';
import type { MarketListItem } from '@/types/market';
import { formatPrice, formatVolume } from '@/lib/formatters';

interface MarketCardProps {
  market: MarketListItem;
}

export function MarketCard({ market }: MarketCardProps) {
  const priceColor = market.currentPrice > 0.5 ? 'text-green-400' : 'text-red-400';

  return (
    <Link
      to={`/markets/${market.id}`}
      className="block rounded-xl border border-gray-800 bg-gray-900 p-4 transition-colors hover:border-gray-700 hover:bg-gray-800/50"
    >
      <div className="flex items-start gap-3">
        {market.imageUrl && (
          <img
            src={market.imageUrl}
            alt=""
            className="h-10 w-10 rounded-lg object-cover"
          />
        )}
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-gray-200 line-clamp-2">
            {market.question}
          </p>
          <div className="mt-2 flex items-center gap-4 text-xs text-gray-500">
            {market.category && (
              <span className="rounded bg-gray-800 px-2 py-0.5">{market.category}</span>
            )}
            <span>Vol: {formatVolume(market.volume)}</span>
          </div>
        </div>
        <div className="text-right">
          <p className={`text-xl font-bold ${priceColor}`}>
            {formatPrice(market.currentPrice)}
          </p>
          <p className="text-xs text-gray-500">YES</p>
        </div>
      </div>
    </Link>
  );
}
