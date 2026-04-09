export interface NewsItem {
  id: number;
  marketId: number;
  title: string;
  url: string;
  source: string | null;
  tone: number | null;
  imageUrl: string | null;
  publishedAt: string;
}
