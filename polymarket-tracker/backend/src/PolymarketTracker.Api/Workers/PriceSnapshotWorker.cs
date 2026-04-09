using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PolymarketTracker.Api.Configuration;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Models.Domain;
using PolymarketTracker.Api.Services.Interfaces;

namespace PolymarketTracker.Api.Workers;

public class PriceSnapshotWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPolymarketService _polymarketService;
    private readonly PolymarketOptions _options;
    private readonly ILogger<PriceSnapshotWorker> _logger;
    private readonly SemaphoreSlim _throttle = new(10); // max 10 concurrent requests

    public PriceSnapshotWorker(
        IServiceScopeFactory scopeFactory,
        IPolymarketService polymarketService,
        IOptions<PolymarketOptions> options,
        ILogger<PriceSnapshotWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _polymarketService = polymarketService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PriceSnapshotWorker starting, interval: {Interval}s", _options.PriceSnapshotIntervalSeconds);

        // Wait for initial market sync
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CaptureSnapshotsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "PriceSnapshotWorker error during capture cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PriceSnapshotIntervalSeconds), stoppingToken);
        }
    }

    private async Task CaptureSnapshotsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var activeMarkets = await db.Markets
            .Where(m => m.Active && m.YesTokenId != null)
            .OrderByDescending(m => m.Volume)
            .Take(100) // top 100 by volume
            .ToListAsync(ct);

        if (activeMarkets.Count == 0) return;

        var snapshots = new List<PriceSnapshot>();
        var now = DateTime.UtcNow;

        var tasks = activeMarkets.Select(async market =>
        {
            await _throttle.WaitAsync(ct);
            try
            {
                var price = await _polymarketService.GetMidpointAsync(market.YesTokenId!, ct);
                if (price.HasValue)
                {
                    lock (snapshots)
                    {
                        snapshots.Add(new PriceSnapshot
                        {
                            MarketId = market.Id,
                            Price = price.Value,
                            Timestamp = now
                        });
                    }
                    market.CurrentPrice = price.Value;
                    market.UpdatedAt = now;
                }
            }
            finally
            {
                _throttle.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (snapshots.Count > 0)
        {
            db.PriceSnapshots.AddRange(snapshots);
            await db.SaveChangesAsync(ct);
        }

        _logger.LogDebug("Captured {Count} price snapshots", snapshots.Count);
    }
}
