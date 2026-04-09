using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PolymarketTracker.Api.Data;
using PolymarketTracker.Api.Models.Domain;
using PolymarketTracker.Api.Models.Dto;
using PolymarketTracker.Api.Services.Interfaces;

namespace PolymarketTracker.Api.Services;

public partial class IssueService : IIssueService
{
    private readonly AppDbContext _db;
    private readonly ILogger<IssueService> _logger;

    public IssueService(AppDbContext db, ILogger<IssueService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<IssueDto>> GetAllAsync(string? status = null, CancellationToken ct = default)
    {
        var query = _db.Issues.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.Status == status);

        return await query.OrderByDescending(i => i.CreatedAt)
            .Select(i => ToDto(i))
            .ToListAsync(ct);
    }

    public async Task<IssueDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var issue = await _db.Issues.FindAsync([id], ct);
        return issue is null ? null : ToDto(issue);
    }

    public async Task<IssueDto> CreateAsync(CreateIssueDto dto, CancellationToken ct = default)
    {
        var issue = new Issue
        {
            Title = dto.Title,
            Description = dto.Description,
            Labels = dto.Labels
        };

        _db.Issues.Add(issue);
        await _db.SaveChangesAsync(ct);

        // Generate branch name from issue number + title
        var slug = Slugify(dto.Title);
        issue.GitBranch = $"issue/{issue.IssueNumber}-{slug}";
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created issue #{IssueNumber}: {Title}, branch: {Branch}",
            issue.IssueNumber, issue.Title, issue.GitBranch);

        return ToDto(issue);
    }

    public async Task<IssueDto?> UpdateAsync(int id, UpdateIssueDto dto, CancellationToken ct = default)
    {
        var issue = await _db.Issues.FindAsync([id], ct);
        if (issue is null) return null;

        if (dto.Title is not null) issue.Title = dto.Title;
        if (dto.Description is not null) issue.Description = dto.Description;
        if (dto.AssignedTo is not null) issue.AssignedTo = dto.AssignedTo;
        if (dto.Labels is not null) issue.Labels = dto.Labels;
        if (dto.Status is not null)
        {
            issue.Status = dto.Status;
            if (dto.Status == "closed") issue.ClosedAt = DateTime.UtcNow;
        }

        issue.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ToDto(issue);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var issue = await _db.Issues.FindAsync([id], ct);
        if (issue is null) return false;

        issue.Status = "deleted";
        issue.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return true;
    }

    private static IssueDto ToDto(Issue i) => new(
        i.Id, i.IssueNumber, i.Title, i.Description, i.Status,
        i.GitBranch, i.AssignedTo, i.Labels,
        i.CreatedAt, i.UpdatedAt, i.ClosedAt);

    private static string Slugify(string text)
    {
        var slug = text.ToLowerInvariant();
        slug = SlugRegex().Replace(slug, "-");
        slug = DashRegex().Replace(slug, "-");
        return slug.Trim('-')[..Math.Min(slug.Length, 50)].TrimEnd('-');
    }

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex DashRegex();
}
