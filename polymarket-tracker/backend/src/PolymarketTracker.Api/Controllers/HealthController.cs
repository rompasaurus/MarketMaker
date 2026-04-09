using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolymarketTracker.Api.Data;

namespace PolymarketTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var checks = new Dictionary<string, string>();

        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1");
            checks["database"] = "healthy";
        }
        catch
        {
            checks["database"] = "unhealthy";
        }

        var allHealthy = checks.Values.All(v => v == "healthy");
        return allHealthy
            ? Ok(new { status = "healthy", checks })
            : StatusCode(503, new { status = "unhealthy", checks });
    }
}
