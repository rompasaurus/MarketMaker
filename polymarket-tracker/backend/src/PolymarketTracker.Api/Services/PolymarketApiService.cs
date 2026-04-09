using System.Text.Json;
using PolymarketTracker.Api.Models.External;
using PolymarketTracker.Api.Services.Interfaces;

namespace PolymarketTracker.Api.Services;

public class PolymarketApiService : IPolymarketService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PolymarketApiService> _logger;

    public PolymarketApiService(
        HttpClient httpClient,
        IHttpClientFactory httpClientFactory,
        ILogger<PolymarketApiService> logger)
    {
        _httpClient = httpClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<GammaMarketResponse>> GetMarketsAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("GammaApi");
        var allMarkets = new List<GammaMarketResponse>();
        var offset = 0;
        const int limit = 100;

        while (true)
        {
            var url = $"/markets?closed=false&limit={limit}&offset={offset}&order=volume_num&ascending=false";
            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var markets = JsonSerializer.Deserialize<List<GammaMarketResponse>>(json) ?? [];

            if (markets.Count == 0) break;

            allMarkets.AddRange(markets);
            offset += limit;

            if (markets.Count < limit) break;
            if (allMarkets.Count >= 500) break; // cap at 500 markets for MVP
        }

        _logger.LogInformation("Fetched {Count} markets from Gamma API", allMarkets.Count);
        return allMarkets;
    }

    public async Task<decimal?> GetMidpointAsync(string tokenId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/midpoint?token_id={tokenId}", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<PolymarketMidpointResponse>(json);

            if (result != null && decimal.TryParse(result.Mid, out var mid))
                return mid;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get midpoint for token {TokenId}", tokenId);
            return null;
        }
    }

    public async Task<List<PolymarketPriceHistoryPoint>> GetPriceHistoryAsync(
        string tokenId, int fidelity = 60, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(
            $"/prices-history?market={tokenId}&interval=max&fidelity={fidelity}", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var history = JsonSerializer.Deserialize<List<PolymarketPriceHistoryPoint>>(json) ?? [];

        return history;
    }
}
