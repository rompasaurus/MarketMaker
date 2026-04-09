using Microsoft.AspNetCore.SignalR;

namespace PolymarketTracker.Api.Hubs;

public class MarketHub : Hub
{
    private readonly ILogger<MarketHub> _logger;

    public MarketHub(ILogger<MarketHub> logger) => _logger = logger;

    public async Task JoinMarket(string conditionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conditionId);
        _logger.LogDebug("Client {ConnectionId} joined market {ConditionId}", Context.ConnectionId, conditionId);
    }

    public async Task LeaveMarket(string conditionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conditionId);
        _logger.LogDebug("Client {ConnectionId} left market {ConditionId}", Context.ConnectionId, conditionId);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
