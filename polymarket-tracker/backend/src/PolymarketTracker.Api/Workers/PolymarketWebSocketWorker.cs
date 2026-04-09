using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PolymarketTracker.Api.Configuration;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Hubs;
using PolymarketTracker.Api.Models.Domain;
using PolymarketTracker.Api.Models.Dto;
using PolymarketTracker.Api.Models.External;

namespace PolymarketTracker.Api.Workers;

public class PolymarketWebSocketWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly PolymarketOptions _options;
    private readonly ILogger<PolymarketWebSocketWorker> _logger;
    private readonly Channel<(PolymarketTradeMessage Trade, int MarketId, string ConditionId)> _tradeChannel;

    public PolymarketWebSocketWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<MarketHub> hubContext,
        IOptions<PolymarketOptions> options,
        ILogger<PolymarketWebSocketWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _options = options.Value;
        _logger = logger;
        _tradeChannel = Channel.CreateUnbounded<(PolymarketTradeMessage, int, string)>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PolymarketWebSocketWorker starting");

        // Start the DB persistence consumer
        var persistTask = PersistTradesAsync(stoppingToken);

        // Wait for markets to be synced
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        var retryDelay = TimeSpan.FromSeconds(1);
        var maxRetryDelay = TimeSpan.FromSeconds(60);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndListenAsync(stoppingToken);
                retryDelay = TimeSpan.FromSeconds(1); // reset on clean disconnect
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "WebSocket disconnected, reconnecting in {Delay}s", retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, stoppingToken);
                retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, maxRetryDelay.TotalSeconds));
            }
        }

        await persistTask;
    }

    private async Task ConnectAndListenAsync(CancellationToken ct)
    {
        // Get active market token IDs
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var markets = await db.Markets
            .Where(m => m.Active && m.YesTokenId != null)
            .OrderByDescending(m => m.Volume)
            .Take(50)
            .ToDictionaryAsync(m => m.YesTokenId!, m => (m.Id, m.ConditionId), ct);

        if (markets.Count == 0)
        {
            _logger.LogWarning("No active markets with token IDs, waiting...");
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return;
        }

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(_options.WebSocketUrl), ct);
        _logger.LogInformation("Connected to Polymarket WebSocket");

        // Subscribe to market asset channels
        foreach (var tokenId in markets.Keys)
        {
            var subscribeMsg = JsonSerializer.Serialize(new
            {
                type = "subscribe",
                channel = "market",
                assets_id = tokenId
            });
            var bytes = Encoding.UTF8.GetBytes(subscribeMsg);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }

        _logger.LogInformation("Subscribed to {Count} market channels", markets.Count);

        // Listen for messages
        var buffer = new byte[8192];
        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation("WebSocket closed by server");
                break;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            try
            {
                var message = JsonSerializer.Deserialize<PolymarketWebSocketMessage>(json);
                if (message?.Trades is { Count: > 0 })
                {
                    foreach (var trade in message.Trades)
                    {
                        if (markets.TryGetValue(trade.AssetId, out var marketInfo))
                        {
                            await _tradeChannel.Writer.WriteAsync((trade, marketInfo.Id, marketInfo.ConditionId), ct);

                            // Broadcast immediately via SignalR
                            if (decimal.TryParse(trade.Price, out var price) && decimal.TryParse(trade.Size, out var size))
                            {
                                var dto = new TradeDto(0, marketInfo.Id, trade.Side, trade.Outcome, price, size, DateTime.UtcNow);
                                await _hubContext.Clients.Group(marketInfo.ConditionId).SendAsync("ReceiveTrade", dto, ct);
                                await _hubContext.Clients.All.SendAsync("ReceiveGlobalTrade", dto, ct);
                            }
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Failed to parse WebSocket message");
            }
        }
    }

    private async Task PersistTradesAsync(CancellationToken ct)
    {
        await foreach (var (trade, marketId, _) in _tradeChannel.Reader.ReadAllAsync(ct))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (!decimal.TryParse(trade.Price, out var price) ||
                    !decimal.TryParse(trade.Size, out var size))
                    continue;

                var exists = await db.Trades.AnyAsync(t => t.PolymarketTradeId == trade.Id, ct);
                if (exists) continue;

                db.Trades.Add(new Trade
                {
                    MarketId = marketId,
                    PolymarketTradeId = trade.Id,
                    Side = trade.Side,
                    Outcome = trade.Outcome,
                    Price = price,
                    Size = size,
                    Timestamp = DateTime.UtcNow
                });

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to persist trade {TradeId}", trade.Id);
            }
        }
    }
}
