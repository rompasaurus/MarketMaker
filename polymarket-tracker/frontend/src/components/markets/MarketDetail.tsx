import type { Market } from '@/types/market';
import { formatPrice, formatVolume, formatTimestamp } from '@/lib/formatters';

interface MarketDetailProps {
  market: Market;
}

export function MarketDetail({ market }: MarketDetailProps) {
  const priceColor = market.currentPrice > 0.5 ? 'text-green-400' : 'text-red-400';

  return (
    <div className="rounded-xl border border-gray-800 bg-gray-900 p-6">
      <div className="flex items-start gap-4">
        {market.imageUrl && (
          <img src={market.imageUrl} alt="" className="h-14 w-14 rounded-xl object-cover" />
        )}
        <div className="flex-1">
          <h2 className="text-xl font-bold text-white">{market.question}</h2>
          {market.description && (
            <p className="mt-2 text-sm text-gray-400 line-clamp-3">{market.description}</p>
          )}
        </div>
        <div className="text-right">
          <p className={`text-3xl font-bold ${priceColor}`}>
            {formatPrice(market.currentPrice)}
          </p>
          <p className="text-sm text-gray-500">YES probability</p>
        </div>
      </div>

      <div className="mt-4 flex flex-wrap gap-6 border-t border-gray-800 pt-4 text-sm">
        <div>
          <span className="text-gray-500">Volume</span>
          <p className="font-medium text-gray-200">{formatVolume(market.volume)}</p>
        </div>
        <div>
          <span className="text-gray-500">Liquidity</span>
          <p className="font-medium text-gray-200">{formatVolume(market.liquidity)}</p>
        </div>
        {market.category && (
          <div>
            <span className="text-gray-500">Category</span>
            <p className="font-medium text-gray-200">{market.category}</p>
          </div>
        )}
        {market.endDate && (
          <div>
            <span className="text-gray-500">Resolves</span>
            <p className="font-medium text-gray-200">{formatTimestamp(market.endDate)}</p>
          </div>
        )}
        <div>
          <span className="text-gray-500">Last Updated</span>
          <p className="font-medium text-gray-200">{formatTimestamp(market.updatedAt)}</p>
        </div>
      </div>
    </div>
  );
}
