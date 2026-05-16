using AsvsSecurityAuditor.Data;
using AsvsSecurityAuditor.Models.Entities;
using AsvsSecurityAuditor.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AsvsSecurityAuditor.Repositories;

public class AssessmentRepository : IAssessmentRepository
{
    private readonly ApplicationDbContext _db;

    public AssessmentRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Assessment>> ListForUserAsync(string userId, CancellationToken ct = default) =>
        await _db.Assessments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.UpdatedUtc ?? a.CreatedUtc)
            .ToListAsync(ct);

    public async Task<Assessment?> GetWithEntriesAsync(int assessmentId, string userId, CancellationToken ct = default) =>
        await _db.Assessments
            .Include(a => a.Entries!)
            .ThenInclude(e => e.Requirement)
            .FirstOrDefaultAsync(a => a.Id == assessmentId && a.UserId == userId, ct);

    public async Task<Assessment?> GetWithEntriesAndRequirementsAsync(int assessmentId, string userId, CancellationToken ct = default) =>
        await GetWithEntriesAsync(assessmentId, userId, ct);

    public async Task AddAsync(Assessment assessment, CancellationToken ct = default) =>
        await _db.Assessments.AddAsync(assessment, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
