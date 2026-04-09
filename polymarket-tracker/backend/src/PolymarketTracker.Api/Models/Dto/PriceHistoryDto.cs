namespace PolymarketTracker.Api.Models.Dto;

public record PriceHistoryDto(
    decimal Price,
    DateTime Timestamp);

public record PriceHistoryResponse(
    string ConditionId,
    List<PriceHistoryDto> History);
