using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Models.Dto;

namespace PolymarketTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MarketsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MarketListDto>>> GetAll(
        [FromQuery] string? category = null,
        [FromQuery] bool activeOnly = true,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.Markets.AsQueryable();

        if (activeOnly) query = query.Where(m => m.Active);
        if (!string.IsNullOrEmpty(category)) query = query.Where(m => m.Category == category);
        if (!string.IsNullOrEmpty(search)) query = query.Where(m => m.Question.Contains(search));

        var markets = await query
            .OrderByDescending(m => m.Volume)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MarketListDto(
                m.Id, m.ConditionId, m.Question, m.Category,
                m.Volume, m.CurrentPrice, m.ImageUrl, m.Active))
            .ToListAsync();

        return Ok(markets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MarketDto>> GetById(int id)
    {
        var market = await _db.Markets.FindAsync(id);
        if (market is null) return NotFound();

        return Ok(new MarketDto(
            market.Id, market.ConditionId, market.Question, market.Description,
            market.Category, market.EndDate, market.Active, market.Volume,
            market.Liquidity, market.CurrentPrice, market.ImageUrl, market.UpdatedAt));
    }
}
