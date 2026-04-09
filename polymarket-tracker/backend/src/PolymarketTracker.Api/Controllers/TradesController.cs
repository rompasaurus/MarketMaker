using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Models.Dto;

namespace PolymarketTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradesController : ControllerBase
{
    private readonly AppDbContext _db;

    public TradesController(AppDbContext db) => _db = db;

    [HttpGet("{marketId:int}")]
    public async Task<ActionResult<List<TradeDto>>> GetByMarket(int marketId, [FromQuery] int limit = 50)
    {
        limit = Math.Min(limit, 200);

        var trades = await _db.Trades
            .Where(t => t.MarketId == marketId)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .Select(t => new TradeDto(
                t.Id, t.MarketId, t.Side, t.Outcome,
                t.Price, t.Size, t.Timestamp))
            .ToListAsync();

        return Ok(trades);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<List<TradeDto>>> GetRecent([FromQuery] int limit = 50)
    {
        limit = Math.Min(limit, 200);

        var trades = await _db.Trades
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .Select(t => new TradeDto(
                t.Id, t.MarketId, t.Side, t.Outcome,
                t.Price, t.Size, t.Timestamp))
            .ToListAsync();

        return Ok(trades);
    }
}
