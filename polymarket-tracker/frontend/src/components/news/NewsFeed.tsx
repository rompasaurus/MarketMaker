import { useQuery } from '@tanstack/react-query';
import { getNewsByMarket } from '@/api/news';
import { NewsCard } from './NewsCard';

interface NewsFeedProps {
  marketId: number;
}

export function NewsFeed({ marketId }: NewsFeedProps) {
  const { data: news, isLoading } = useQuery({
    queryKey: ['news', marketId],
    queryFn: () => getNewsByMarket(marketId),
    refetchInterval: 60_000,
  });

  return (
    <div className="rounded-xl border border-gray-800 bg-gray-900">
      <div className="border-b border-gray-800 px-4 py-3">
        <h3 className="text-sm font-semibold text-gray-300">Related News</h3>
      </div>
      <div className="max-h-96 overflow-y-auto">
        {isLoading ? (
          <p className="p-4 text-center text-sm text-gray-600">Loading news...</p>
        ) : !news?.length ? (
          <p className="p-4 text-center text-sm text-gray-600">No news articles yet</p>
        ) : (
          news.map((item) => <NewsCard key={item.id} item={item} />)
        )}
      </div>
    </div>
  );
}
