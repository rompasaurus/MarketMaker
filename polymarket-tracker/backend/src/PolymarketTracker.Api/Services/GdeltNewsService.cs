using System.Text.Json;
using PolymarketTracker.Api.Models.External;
using PolymarketTracker.Api.Services.Interfaces;

namespace PolymarketTracker.Api.Services;

public class GdeltNewsService : INewsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GdeltNewsService> _logger;

    public GdeltNewsService(HttpClient httpClient, ILogger<GdeltNewsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<GdeltArticle>> SearchArticlesAsync(
        string query, int maxRecords = 10, CancellationToken ct = default)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"/api/v2/doc/doc?query={encodedQuery}&mode=ArtList&format=json&maxrecords={maxRecords}&sort=DateDesc&timespan=24h";

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<GdeltDocResponse>(json);

            var articles = result?.Articles ?? [];
            _logger.LogDebug("GDELT returned {Count} articles for query '{Query}'", articles.Count, query);

            return articles;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch GDELT articles for query '{Query}'", query);
            return [];
        }
    }
}
