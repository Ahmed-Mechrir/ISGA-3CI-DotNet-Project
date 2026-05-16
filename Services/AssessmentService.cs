using AsvsSecurityAuditor.Data;
using AsvsSecurityAuditor.DTOs;
using AsvsSecurityAuditor.Models.Entities;
using AsvsSecurityAuditor.Models.Enums;
using AsvsSecurityAuditor.Services.Interfaces;
using AsvsSecurityAuditor.ViewModels.Assessment;
using Microsoft.EntityFrameworkCore;

namespace AsvsSecurityAuditor.Services;

public class AssessmentService : IAssessmentService
{
    private readonly ApplicationDbContext _db;

    public AssessmentService(ApplicationDbContext db) => _db = db;

    public async Task<int> CreateAssessmentAsync(string userId, string title, CancellationToken ct = default)
    {
        var requirementIds = await _db.Requirements.AsNoTracking().Select(r => r.Id).ToListAsync(ct);
        if (requirementIds.Count == 0)
            throw new InvalidOperationException(
                "No ASVS requirements in the database yet. Ask an administrator to import the checklist CSV.");

        var assessment = new Assessment
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? $"Assessment {DateTime.UtcNow:yyyy-MM-dd}" : title.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        await _db.Assessments.AddAsync(assessment, ct);
        await _db.SaveChangesAsync(ct); // assigns Id

        var entries = requirementIds.Select(rid => new AssessmentEntry
        {
            AssessmentId = assessment.Id,
            RequirementId = rid,
            Status = AssessmentStatus.Pending
        });

        await _db.AssessmentEntries.AddRangeAsync(entries, ct);
        await _db.SaveChangesAsync(ct);
        return assessment.Id;
    }

    public async Task UpdateEntryAsync(
        int assessmentId,
        string userId,
        int requirementEntityId,
        AssessmentEntryPatchDto dto,
        CancellationToken ct = default)
    {
        var entry = await _db.AssessmentEntries
            .Include(e => e.Requirement)
            .Include(e => e.Assessment)
            .FirstOrDefaultAsync(e =>
                    e.AssessmentId == assessmentId
                    && e.RequirementId == requirementEntityId
                    && e.Assessment != null
                    && e.Assessment.UserId == userId,
                ct);

        if (entry == null)
            throw new InvalidOperationException("Assessment entry was not found or access denied.");

        if (entry.Requirement?.PreMarkedNotApplicable == true && dto.Status != AssessmentStatus.NotApplicable)
            throw new InvalidOperationException("This requirement is marked N/A in the source checklist.");

        entry.Status = dto.Status;
        entry.SourceCodeReference = dto.SourceCodeReference ?? "";
        entry.Comment = dto.Comment ?? "";
        entry.ToolUsed = dto.ToolUsed ?? "";
        entry.UpdatedUtc = DateTime.UtcNow;

        var ass = await _db.Assessments.FirstAsync(a => a.Id == assessmentId, ct);
        ass.UpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ManageAssessmentPageViewModel?> GetManageModelAsync(int assessmentId, string userId, CancellationToken ct = default)
    {
        var assessment = await _db.Assessments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assessmentId && a.UserId == userId, ct);

        if (assessment == null)
            return null;

        var rows = await _db.AssessmentEntries.AsNoTracking()
            .Include(e => e.Requirement)
            .Where(e => e.AssessmentId == assessmentId)
            .OrderBy(e => e.Requirement!.Chapter)
            .ThenBy(e => e.Requirement!.RequirementRef)
            .Select(e => new ManageAssessmentRowViewModel
            {
                RequirementEntityId = e.RequirementId,
                RequirementRef = e.Requirement!.RequirementRef,
                Chapter = e.Requirement.Chapter,
                Area = e.Requirement.Area,
                Level = e.Requirement.Level,
                RequirementText = e.Requirement.VerificationRequirement,
                SourceNotApplicable = e.Requirement.PreMarkedNotApplicable,
                Status = e.Status,
                SourceCodeReference = e.SourceCodeReference,
                Comment = e.Comment,
                ToolUsed = e.ToolUsed
            }).ToListAsync(ct);

        return new ManageAssessmentPageViewModel
        {
            AssessmentId = assessment.Id,
            Title = assessment.Title,
            Rows = rows
        };
    }
}
