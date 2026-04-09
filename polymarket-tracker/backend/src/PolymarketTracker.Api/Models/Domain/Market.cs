using System.ComponentModel.DataAnnotations;

namespace PolymarketTracker.Api.Models.Domain;

public class Market
{
    public int Id { get; set; }

    [MaxLength(256)]
    public required string ConditionId { get; set; }

    [MaxLength(512)]
    public required string Question { get; set; }

    public string? Description { get; set; }

    [MaxLength(128)]
    public string? Category { get; set; }

    public DateTime? EndDate { get; set; }

    public bool Active { get; set; } = true;

    public decimal Volume { get; set; }

    public decimal Liquidity { get; set; }

    public decimal CurrentPrice { get; set; }

    [MaxLength(1024)]
    public string? ImageUrl { get; set; }

    [MaxLength(256)]
    public string? YesTokenId { get; set; }

    [MaxLength(256)]
    public string? NoTokenId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PriceSnapshot> PriceSnapshots { get; set; } = [];
    public ICollection<Trade> Trades { get; set; } = [];
    public ICollection<NewsItem> NewsItems { get; set; } = [];
}
