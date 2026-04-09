namespace PolymarketTracker.Api.Models.Dto;

public record NewsItemDto(
    long Id,
    int MarketId,
    string Title,
    string Url,
    string? Source,
    decimal? Tone,
    string? ImageUrl,
    DateTime PublishedAt);
