using PolymarketTracker.Api.Models.External;

namespace PolymarketTracker.Api.Services.Interfaces;

public interface INewsService
{
    Task<List<GdeltArticle>> SearchArticlesAsync(string query, int maxRecords = 10, CancellationToken ct = default);
}
