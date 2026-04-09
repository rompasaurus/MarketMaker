using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PolymarketTracker.Api.Configuration;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Models.Domain;
using PolymarketTracker.Api.Services.Interfaces;

namespace PolymarketTracker.Api.Workers;

public class NewsSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INewsService _newsService;
    private readonly GdeltOptions _options;
    private readonly ILogger<NewsSyncWorker> _logger;

    private static readonly HashSet<string> StopWords =
    [
        "will", "the", "be", "to", "of", "and", "a", "in", "that", "have",
        "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
        "this", "but", "his", "by", "from", "they", "we", "say", "her",
        "she", "or", "an", "my", "one", "all", "would", "there", "their",
        "what", "so", "up", "out", "if", "about", "who", "get", "which",
        "go", "me", "when", "can", "no", "just", "him", "how", "its",
        "yes", "before", "after", "than", "into", "could", "does", "did",
        "has", "had", "is", "are", "was", "were", "been", "being"
    ];

    public NewsSyncWorker(
        IServiceScopeFactory scopeFactory,
        INewsService newsService,
        IOptions<GdeltOptions> options,
        ILogger<NewsSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _newsService = newsService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NewsSyncWorker starting, interval: {Interval} min", _options.SyncIntervalMinutes);

        // Wait for market sync
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncNewsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "NewsSyncWorker error during sync cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.SyncIntervalMinutes), stoppingToken);
        }
    }

    private async Task SyncNewsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var markets = await db.Markets
            .Where(m => m.Active)
            .OrderByDescending(m => m.Volume)
            .Take(_options.MaxMarketsPerSync)
            .ToListAsync(ct);

        var totalNew = 0;

        foreach (var market in markets)
        {
            var keywords = ExtractKeywords(market.Question);
            if (string.IsNullOrEmpty(keywords)) continue;

            var articles = await _newsService.SearchArticlesAsync(keywords, _options.MaxArticlesPerMarket, ct);

            foreach (var article in articles)
            {
                if (string.IsNullOrEmpty(article.Url)) continue;

                var exists = await db.NewsItems.AnyAsync(n => n.Url == article.Url, ct);
                if (exists) continue;

                decimal? tone = null;
                if (!string.IsNullOrEmpty(article.Tone))
                {
                    var parts = article.Tone.Split(',');
                    if (parts.Length > 0 && decimal.TryParse(parts[0], CultureInfo.InvariantCulture, out var t))
                        tone = t;
                }

                DateTime publishedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(article.SeenDate) && article.SeenDate.Length >= 14)
                {
                    DateTime.TryParseExact(article.SeenDate[..14], "yyyyMMddHHmmss",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out publishedAt);
                }

                db.NewsItems.Add(new NewsItem
                {
                    MarketId = market.Id,
                    Title = article.Title,
                    Url = article.Url,
                    Source = article.Domain,
                    Tone = tone,
                    ImageUrl = article.SocialImage,
                    PublishedAt = publishedAt
                });
                totalNew++;
            }

            // Rate limit: 1 request per second
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        if (totalNew > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("NewsSyncWorker added {Count} new articles", totalNew);
        }
    }

    private static string ExtractKeywords(string question)
    {
        var words = question.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => w.Trim('?', '.', ',', '!', '\'', '"').ToLowerInvariant())
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .OrderByDescending(w => w.Length)
            .Take(3);

        return string.Join(" ", words);
    }
}
