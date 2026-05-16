using AsvsSecurityAuditor.Data;
using AsvsSecurityAuditor.Models.Entities;
using AsvsSecurityAuditor.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AsvsSecurityAuditor.Repositories;

public class RequirementRepository : IRequirementRepository
{
    private readonly ApplicationDbContext _db;

    public RequirementRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AsvsRequirementEntity>> GetAllOrderedAsync(CancellationToken ct = default)
    {
        return await _db.Requirements
            .AsNoTracking()
            .OrderBy(r => r.Chapter).ThenBy(r => r.RequirementRef)
            .ToListAsync(ct);
    }

    public async Task<AsvsRequirementEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.Requirements.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<AsvsRequirementEntity?> FindByRequirementRefAsync(string requirementRef, CancellationToken ct = default) =>
        await _db.Requirements.FirstOrDefaultAsync(r => r.RequirementRef == requirementRef, ct);

    public async Task AddAsync(AsvsRequirementEntity entity, CancellationToken ct = default)
    {
        await _db.Requirements.AddAsync(entity, ct);
    }

    public void Update(AsvsRequirementEntity entity) => _db.Requirements.Update(entity);

    public async Task<bool> RequirementHasAssessmentEntriesAsync(int id, CancellationToken ct = default) =>
        await _db.AssessmentEntries.AnyAsync(e => e.RequirementId == id, ct);

    public async Task<bool> DeleteIfUnusedAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity == null) return false;
        if (await RequirementHasAssessmentEntriesAsync(id, ct))
            return false;
        _db.Requirements.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
