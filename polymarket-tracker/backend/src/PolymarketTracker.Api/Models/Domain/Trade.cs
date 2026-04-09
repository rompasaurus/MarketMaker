using System.ComponentModel.DataAnnotations;

namespace PolymarketTracker.Api.Models.Domain;

public class Trade
{
    public long Id { get; set; }

    public int MarketId { get; set; }
    public Market Market { get; set; } = null!;

    [MaxLength(256)]
    public required string PolymarketTradeId { get; set; }

    [MaxLength(16)]
    public required string Side { get; set; }

    [MaxLength(16)]
    public required string Outcome { get; set; }

    public decimal Price { get; set; }

    public decimal Size { get; set; }

    public DateTime Timestamp { get; set; }
}
