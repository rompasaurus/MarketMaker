using PolymarketTracker.Api.Models.External;

namespace PolymarketTracker.Api.Services.Interfaces;

public interface IPolymarketService
{
    Task<List<GammaMarketResponse>> GetMarketsAsync(CancellationToken ct = default);
    Task<decimal?> GetMidpointAsync(string tokenId, CancellationToken ct = default);
    Task<List<PolymarketPriceHistoryPoint>> GetPriceHistoryAsync(string tokenId, int fidelity = 60, CancellationToken ct = default);
}
