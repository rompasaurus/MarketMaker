namespace PolymarketTracker.Api.Configuration;

public class PolymarketOptions
{
    public string ClobBaseUrl { get; set; } = "https://clob.polymarket.com";
    public string GammaBaseUrl { get; set; } = "https://gamma-api.polymarket.com";
    public string WebSocketUrl { get; set; } = "wss://ws-subscriptions-clob.polymarket.com/ws/market";
    public int MarketSyncIntervalSeconds { get; set; } = 300;
    public int PriceSnapshotIntervalSeconds { get; set; } = 30;
}
