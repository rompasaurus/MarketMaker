namespace PolymarketTracker.Api.Models.Dto;

public record MarketDto(
    int Id,
    string ConditionId,
    string Question,
    string? Description,
    string? Category,
    DateTime? EndDate,
    bool Active,
    decimal Volume,
    decimal Liquidity,
    decimal CurrentPrice,
    string? ImageUrl,
    DateTime UpdatedAt);

public record MarketListDto(
    int Id,
    string ConditionId,
    string Question,
    string? Category,
    decimal Volume,
    decimal CurrentPrice,
    string? ImageUrl,
    bool Active);
