using AsvsSecurityAuditor.Models.Entities;

namespace AsvsSecurityAuditor.Repositories.Interfaces;

public interface IAssessmentRepository
{
    Task<IReadOnlyList<Assessment>> ListForUserAsync(string userId, CancellationToken ct = default);
    Task<Assessment?> GetWithEntriesAsync(int assessmentId, string userId, CancellationToken ct = default);
    Task<Assessment?> GetWithEntriesAndRequirementsAsync(int assessmentId, string userId, CancellationToken ct = default);
    Task AddAsync(Assessment assessment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
