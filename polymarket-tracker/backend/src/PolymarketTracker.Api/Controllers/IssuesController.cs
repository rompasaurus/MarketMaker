using Microsoft.AspNetCore.Mvc;
using PolymarketTracker.Api.Models.Dto;
using PolymarketTracker.Api.Services.Interfaces;

namespace PolymarketTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _issueService;

    public IssuesController(IIssueService issueService) => _issueService = issueService;

    [HttpGet]
    public async Task<ActionResult<List<IssueDto>>> GetAll([FromQuery] string? status = null)
        => Ok(await _issueService.GetAllAsync(status));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IssueDto>> GetById(int id)
    {
        var issue = await _issueService.GetByIdAsync(id);
        return issue is null ? NotFound() : Ok(issue);
    }

    [HttpPost]
    public async Task<ActionResult<IssueDto>> Create([FromBody] CreateIssueDto dto)
    {
        var issue = await _issueService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = issue.Id }, issue);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<IssueDto>> Update(int id, [FromBody] UpdateIssueDto dto)
    {
        var issue = await _issueService.UpdateAsync(id, dto);
        return issue is null ? NotFound() : Ok(issue);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _issueService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
