import type { NewsItem } from '@/types/news';
import { formatRelativeTime, toneToColor, toneToLabel } from '@/lib/formatters';
import { ExternalLink } from 'lucide-react';

interface NewsCardProps {
  item: NewsItem;
}

export function NewsCard({ item }: NewsCardProps) {
  return (
    <a
      href={item.url}
      target="_blank"
      rel="noopener noreferrer"
      className="flex gap-3 border-b border-gray-800/50 p-3 transition-colors hover:bg-gray-800/30"
    >
      {item.imageUrl && (
        <img
          src={item.imageUrl}
          alt=""
          className="h-16 w-16 rounded-lg object-cover flex-shrink-0"
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
      )}
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-gray-200 line-clamp-2">{item.title}</p>
        <div className="mt-1 flex items-center gap-3 text-xs">
          {item.source && <span className="text-gray-500">{item.source}</span>}
          <span className={toneToColor(item.tone)}>{toneToLabel(item.tone)}</span>
          <span className="text-gray-600">{formatRelativeTime(item.publishedAt)}</span>
        </div>
      </div>
      <ExternalLink size={14} className="mt-1 flex-shrink-0 text-gray-600" />
    </a>
  );
}
