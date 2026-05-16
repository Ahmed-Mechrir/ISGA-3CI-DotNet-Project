using AsvsSecurityAuditor.Data;
using AsvsSecurityAuditor.DTOs;
using AsvsSecurityAuditor.Models.Entities;
using AsvsSecurityAuditor.Models.Enums;
using AsvsSecurityAuditor.Services.Interfaces;
using AsvsSecurityAuditor.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace AsvsSecurityAuditor.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db) => _db = db;

    public async Task<BenchmarkReportViewModel> BuildBenchmarkAsync(int assessmentId, string userId, CancellationToken ct = default)
    {
        var a = await _db.Assessments.AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == assessmentId && x.UserId == userId, ct);

        if (a == null)
            throw new InvalidOperationException("Assessment was not found or access denied.");

        var entries = await _db.AssessmentEntries.AsNoTracking()
            .Include(e => e.Requirement)
            .Where(e => e.AssessmentId == assessmentId && e.Requirement != null)
            .ToListAsync(ct);

        var display = string.IsNullOrWhiteSpace(a.User?.DisplayName) ? (a.User?.UserName ?? userId) : a.User.DisplayName!;
        return Compute(entries, assessmentId, a.Title, display);
    }

    public async Task<DashboardStatsDto> BuildDashboardStatsAsync(int assessmentId, string userId, CancellationToken ct = default)
    {
        var exists = await _db.Assessments.AnyAsync(a => a.Id == assessmentId && a.UserId == userId, ct);
        if (!exists)
            throw new InvalidOperationException("Assessment was not found or access denied.");

        var entries = await _db.AssessmentEntries.AsNoTracking()
            .Include(e => e.Requirement)
            .Where(e => e.AssessmentId == assessmentId && e.Requirement != null)
            .ToListAsync(ct);

        var bm = Compute(entries, assessmentId, "", "");

        var chapterDtos = bm.Chapters.Select(c => new ChapterComplianceDto
        {
            Chapter = c.Chapter,
            Applicable = c.Applicable,
            Valid = c.Valid,
            CompliancePct = c.CompliancePct
        }).ToList();

        return new DashboardStatsDto
        {
            ApplicableTotal = bm.ApplicableTotal,
            Valid = bm.Valid,
            NotValid = bm.NotValid,
            Pending = bm.Pending,
            UserNotApplicable = bm.UserMarkedNotApplicable,
            CompliancePct = bm.CompliancePct,
            SecurityScore = bm.SecurityScore,
            Chapters = chapterDtos
        };
    }

    private static BenchmarkReportViewModel Compute(
        List<AssessmentEntry> entries,
        int assessmentId,
        string assessmentTitle,
        string displayUser)
    {
        var globalNa = entries.Count(r => r.Requirement!.PreMarkedNotApplicable);

        static bool ApplicableFlag(AssessmentStatus status, bool srcNa) =>
            !srcNa && status != AssessmentStatus.NotApplicable;

        var applicable = entries.Where(p => ApplicableFlag(p.Status, p.Requirement!.PreMarkedNotApplicable)).ToList();

        var valid = applicable.Count(p => p.Status == AssessmentStatus.Valid);
        var notValid = applicable.Count(p => p.Status == AssessmentStatus.NotValid);
        var pending = applicable.Count(p => p.Status == AssessmentStatus.Pending);
        var userNa = entries.Count(p => !p.Requirement!.PreMarkedNotApplicable && p.Status == AssessmentStatus.NotApplicable);

        var applicableTotal = applicable.Count;
        var compliancePct = applicableTotal == 0 ? 0d : Math.Round(100d * valid / applicableTotal, 1);
        var securityScore = applicableTotal == 0 ? 0 : (int)Math.Round(100d * valid / applicableTotal);

        var chapterGroups = applicable
            .GroupBy(p => p.Requirement!.Chapter)
            .Select(g =>
            {
                var app = g.Count();
                var v = g.Count(x => x.Status == AssessmentStatus.Valid);
                var nv = g.Count(x => x.Status == AssessmentStatus.NotValid);
                var pen = g.Count(x => x.Status == AssessmentStatus.Pending);
                var pct = app == 0 ? 0 : Math.Round(100d * v / app, 1);
                return new ChapterComplianceViewModel
                {
                    Chapter = g.Key,
                    Applicable = app,
                    Valid = v,
                    NotValid = nv,
                    Pending = pen,
                    CompliancePct = pct
                };
            })
            .OrderBy(c => c.Chapter)
            .ToList();

        var weak = chapterGroups
            .Where(c => c.Applicable > 0)
            .OrderBy(c => c.CompliancePct)
            .Take(5)
            .Select(c => new WeakAreaViewModel
            {
                Chapter = c.Chapter,
                CompliancePct = c.CompliancePct,
                Rationale = $"Low conformance in \"{c.Chapter}\" ({c.NotValid} failed, {c.Pending} pending)."
            })
            .ToList();

        var risk = ComputeRisk(compliancePct, applicable);

        return new BenchmarkReportViewModel
        {
            AssessmentId = assessmentId,
            AssessmentTitle = assessmentTitle,
            UserDisplay = displayUser,
            GeneratedUtc = DateTime.UtcNow,
            ApplicableTotal = applicableTotal,
            Valid = valid,
            NotValid = notValid,
            Pending = pending,
            GlobalNotApplicable = globalNa,
            UserMarkedNotApplicable = userNa,
            CompliancePct = compliancePct,
            SecurityScore = securityScore,
            RiskLevel = risk,
            Chapters = chapterGroups,
            WeakAreas = weak
        };
    }

    private static RiskLevel ComputeRisk(double compliancePct, List<AssessmentEntry> applicable)
    {
        var level3Failed = applicable.Count(p => p.Requirement!.Level >= 3 && p.Status == AssessmentStatus.NotValid);
        var level2Failed = applicable.Count(p => p.Requirement!.Level == 2 && p.Status == AssessmentStatus.NotValid);

        if (compliancePct < 40 || level3Failed >= 3)
            return RiskLevel.High;
        if (compliancePct < 70 || level2Failed >= 10 || level3Failed >= 1)
            return RiskLevel.Medium;
        return RiskLevel.Low;
    }
}
