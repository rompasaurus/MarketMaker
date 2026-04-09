namespace PolymarketTracker.Api.Models.Dto;

public record IssueDto(
    int Id,
    int IssueNumber,
    string Title,
    string? Description,
    string Status,
    string? GitBranch,
    string? AssignedTo,
    string? Labels,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt);

public record CreateIssueDto(
    string Title,
    string? Description,
    string? Labels);

public record UpdateIssueDto(
    string? Title,
    string? Description,
    string? Status,
    string? AssignedTo,
    string? Labels);
