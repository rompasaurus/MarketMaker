import { useQuery } from '@tanstack/react-query';
import { getMarkets, getMarket } from '@/api/markets';

export function useMarkets(search?: string) {
  return useQuery({
    queryKey: ['markets', search],
    queryFn: () => getMarkets({ search, pageSize: 50 }),
    refetchInterval: 30_000,
  });
}

export function useMarket(id: number) {
  return useQuery({
    queryKey: ['market', id],
    queryFn: () => getMarket(id),
    refetchInterval: 30_000,
  });
}
