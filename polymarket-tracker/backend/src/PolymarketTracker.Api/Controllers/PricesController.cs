using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Models.Dto;

namespace PolymarketTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricesController : ControllerBase
{
    private readonly AppDbContext _db;

    public PricesController(AppDbContext db) => _db = db;

    [HttpGet("history/{marketId:int}")]
    public async Task<ActionResult<PriceHistoryResponse>> GetHistory(
        int marketId,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null)
    {
        var market = await _db.Markets.FindAsync(marketId);
        if (market is null) return NotFound();

        var query = _db.PriceSnapshots
            .Where(p => p.MarketId == marketId);

        if (DateTime.TryParse(from, out var fromDate))
            query = query.Where(p => p.Timestamp >= fromDate);
        if (DateTime.TryParse(to, out var toDate))
            query = query.Where(p => p.Timestamp <= toDate);

        var history = await query
            .OrderBy(p => p.Timestamp)
            .Select(p => new PriceHistoryDto(p.Price, p.Timestamp))
            .ToListAsync();

        return Ok(new PriceHistoryResponse(market.ConditionId, history));
    }
}
