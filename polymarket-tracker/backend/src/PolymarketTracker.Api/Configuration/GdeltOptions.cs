namespace PolymarketTracker.Api.Configuration;

public class GdeltOptions
{
    public string BaseUrl { get; set; } = "https://api.gdeltproject.org";
    public int SyncIntervalMinutes { get; set; } = 15;
    public int MaxMarketsPerSync { get; set; } = 20;
    public int MaxArticlesPerMarket { get; set; } = 10;
}
