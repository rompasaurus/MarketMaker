export interface MarketListItem {
  id: number;
  conditionId: string;
  question: string;
  category: string | null;
  volume: number;
  currentPrice: number;
  imageUrl: string | null;
  active: boolean;
}

export interface Market {
  id: number;
  conditionId: string;
  question: string;
  description: string | null;
  category: string | null;
  endDate: string | null;
  active: boolean;
  volume: number;
  liquidity: number;
  currentPrice: number;
  imageUrl: string | null;
  updatedAt: string;
}
