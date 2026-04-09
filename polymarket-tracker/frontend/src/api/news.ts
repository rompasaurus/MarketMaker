import { api } from './client';
import type { NewsItem } from '@/types/news';

export async function getNewsByMarket(marketId: number, limit = 20): Promise<NewsItem[]> {
  const { data } = await api.get('/news', { params: { marketId, limit } });
  return data;
}
