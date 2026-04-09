using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Models.Dto;

namespace PolymarketTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NewsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<NewsItemDto>>> GetByMarket(
        [FromQuery] int? marketId = null,
        [FromQuery] int limit = 20)
    {
        limit = Math.Min(limit, 100);

        var query = _db.NewsItems.AsQueryable();
        if (marketId.HasValue)
            query = query.Where(n => n.MarketId == marketId.Value);

        var items = await query
            .OrderByDescending(n => n.PublishedAt)
            .Take(limit)
            .Select(n => new NewsItemDto(
                n.Id, n.MarketId, n.Title, n.Url,
                n.Source, n.Tone, n.ImageUrl, n.PublishedAt))
            .ToListAsync();

        return Ok(items);
    }
}
