using System.ComponentModel.DataAnnotations;

namespace PolymarketTracker.Api.Models.Domain;

public class NewsItem
{
    public long Id { get; set; }

    public int MarketId { get; set; }
    public Market Market { get; set; } = null!;

    [MaxLength(1024)]
    public required string Title { get; set; }

    [MaxLength(2048)]
    public required string Url { get; set; }

    [MaxLength(256)]
    public string? Source { get; set; }

    public decimal? Tone { get; set; }

    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    public DateTime PublishedAt { get; set; }

    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
