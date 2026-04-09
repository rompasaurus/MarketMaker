using PolymarketTracker.Api.Models.Dto;

namespace PolymarketTracker.Api.Services.Interfaces;

public interface IIssueService
{
    Task<List<IssueDto>> GetAllAsync(string? status = null, CancellationToken ct = default);
    Task<IssueDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IssueDto> CreateAsync(CreateIssueDto dto, CancellationToken ct = default);
    Task<IssueDto?> UpdateAsync(int id, UpdateIssueDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
