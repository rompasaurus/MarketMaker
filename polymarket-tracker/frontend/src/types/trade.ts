export interface Trade {
  id: number;
  marketId: number;
  side: string;
  outcome: string;
  price: number;
  size: number;
  timestamp: string;
}
