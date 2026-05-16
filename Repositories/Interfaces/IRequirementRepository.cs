using AsvsSecurityAuditor.Models.Entities;

namespace AsvsSecurityAuditor.Repositories.Interfaces;

public interface IRequirementRepository
{
    Task<IReadOnlyList<AsvsRequirementEntity>> GetAllOrderedAsync(CancellationToken ct = default);
    Task<AsvsRequirementEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AsvsRequirementEntity?> FindByRequirementRefAsync(string requirementRef, CancellationToken ct = default);
    Task AddAsync(AsvsRequirementEntity entity, CancellationToken ct = default);
    void Update(AsvsRequirementEntity entity);
    Task<bool> DeleteIfUnusedAsync(int id, CancellationToken ct = default);
    Task<bool> RequirementHasAssessmentEntriesAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
