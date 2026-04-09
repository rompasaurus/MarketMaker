namespace PolymarketTracker.Api.Models.Dto;

public record TradeDto(
    long Id,
    int MarketId,
    string Side,
    string Outcome,
    decimal Price,
    decimal Size,
    DateTime Timestamp);
