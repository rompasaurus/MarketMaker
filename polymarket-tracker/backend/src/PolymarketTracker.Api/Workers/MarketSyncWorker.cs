using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PolymarketTracker.Api.Configuration;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Models.Domain;
using PolymarketTracker.Api.Models.External;
using PolymarketTracker.Api.Services.Interfaces;

namespace PolymarketTracker.Api.Workers;

public class MarketSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPolymarketService _polymarketService;
    private readonly PolymarketOptions _options;
    private readonly ILogger<MarketSyncWorker> _logger;

    public MarketSyncWorker(
        IServiceScopeFactory scopeFactory,
        IPolymarketService polymarketService,
        IOptions<PolymarketOptions> options,
        ILogger<MarketSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _polymarketService = polymarketService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketSyncWorker starting, interval: {Interval}s", _options.MarketSyncIntervalSeconds);

        // Initial sync after short delay to let app start up
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncMarketsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "MarketSyncWorker error during sync cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.MarketSyncIntervalSeconds), stoppingToken);
        }
    }

    private async Task SyncMarketsAsync(CancellationToken ct)
    {
        var gammaMarkets = await _polymarketService.GetMarketsAsync(ct);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingByConditionId = await db.Markets
            .ToDictionaryAsync(m => m.ConditionId, ct);

        var newCount = 0;
        var updatedCount = 0;

        foreach (var gm in gammaMarkets)
        {
            if (string.IsNullOrEmpty(gm.ConditionId)) continue;

            // Parse token IDs from clob_token_ids JSON array
            string? yesTokenId = null;
            string? noTokenId = null;
            if (!string.IsNullOrEmpty(gm.ClobTokenIds))
            {
                try
                {
                    var tokens = JsonSerializer.Deserialize<List<string>>(gm.ClobTokenIds);
                    if (tokens is { Count: >= 1 }) yesTokenId = tokens[0];
                    if (tokens is { Count: >= 2 }) noTokenId = tokens[1];
                }
                catch { /* ignore parse errors */ }
            }

            if (existingByConditionId.TryGetValue(gm.ConditionId, out var existing))
            {
                existing.Question = gm.Question;
                existing.Description = gm.Description;
                existing.Category = gm.Category;
                existing.Active = gm.Active;
                existing.Volume = gm.VolumeNum;
                existing.Liquidity = gm.LiquidityNum;
                existing.ImageUrl = gm.Image;
                existing.YesTokenId = yesTokenId;
                existing.NoTokenId = noTokenId;
                existing.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(gm.EndDateIso) && DateTime.TryParse(gm.EndDateIso, out var endDate))
                    existing.EndDate = endDate;

                updatedCount++;
            }
            else
            {
                var market = new Market
                {
                    ConditionId = gm.ConditionId,
                    Question = gm.Question,
                    Description = gm.Description,
                    Category = gm.Category,
                    Active = gm.Active,
                    Volume = gm.VolumeNum,
                    Liquidity = gm.LiquidityNum,
                    ImageUrl = gm.Image,
                    YesTokenId = yesTokenId,
                    NoTokenId = noTokenId,
                };

                if (!string.IsNullOrEmpty(gm.EndDateIso) && DateTime.TryParse(gm.EndDateIso, out var endDate))
                    market.EndDate = endDate;

                db.Markets.Add(market);
                newCount++;
            }
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("MarketSync complete: {New} new, {Updated} updated markets", newCount, updatedCount);
    }
}
