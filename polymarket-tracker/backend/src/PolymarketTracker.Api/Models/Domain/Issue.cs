using System.ComponentModel.DataAnnotations;

namespace PolymarketTracker.Api.Models.Domain;

public class Issue
{
    public int Id { get; set; }

    public int IssueNumber { get; set; }

    [MaxLength(512)]
    public required string Title { get; set; }

    public string? Description { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "open";

    [MaxLength(256)]
    public string? GitBranch { get; set; }

    [MaxLength(128)]
    public string? AssignedTo { get; set; }

    [MaxLength(512)]
    public string? Labels { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }
}
