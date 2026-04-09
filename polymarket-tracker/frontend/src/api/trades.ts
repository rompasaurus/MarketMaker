import { api } from './client';
import type { Trade } from '@/types/trade';

export async function getTradesByMarket(marketId: number, limit = 50): Promise<Trade[]> {
  const { data } = await api.get(`/trades/${marketId}`, { params: { limit } });
  return data;
}

export async function getRecentTrades(limit = 50): Promise<Trade[]> {
  const { data } = await api.get('/trades/recent', { params: { limit } });
  return data;
}
