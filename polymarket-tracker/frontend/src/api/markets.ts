import { api } from './client';
import type { MarketListItem, Market } from '@/types/market';

export async function getMarkets(params?: {
  category?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}): Promise<MarketListItem[]> {
  const { data } = await api.get('/markets', { params });
  return data;
}

export async function getMarket(id: number): Promise<Market> {
  const { data } = await api.get(`/markets/${id}`);
  return data;
}

export interface PriceHistoryPoint {
  price: number;
  timestamp: string;
}

export interface PriceHistoryResponse {
  conditionId: string;
  history: PriceHistoryPoint[];
}

export async function getPriceHistory(
  marketId: number,
  from?: string,
  to?: string
): Promise<PriceHistoryResponse> {
  const { data } = await api.get(`/prices/history/${marketId}`, {
    params: { from, to },
  });
  return data;
}
