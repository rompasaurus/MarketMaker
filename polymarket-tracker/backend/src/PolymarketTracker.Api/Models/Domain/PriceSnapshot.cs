namespace PolymarketTracker.Api.Models.Domain;

public class PriceSnapshot
{
    public long Id { get; set; }

    public int MarketId { get; set; }
    public Market Market { get; set; } = null!;

    public decimal Price { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
