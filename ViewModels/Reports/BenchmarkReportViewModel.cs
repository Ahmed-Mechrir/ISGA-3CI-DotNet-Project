using AsvsSecurityAuditor.Models.Enums;

namespace AsvsSecurityAuditor.ViewModels.Reports;

public class ChapterComplianceViewModel
{
    public string Chapter { get; set; } = string.Empty;
    public int Applicable { get; set; }
    public int Valid { get; set; }
    public int NotValid { get; set; }
    public int Pending { get; set; }
    public double CompliancePct { get; set; }
}

public class WeakAreaViewModel
{
    public string Chapter { get; set; } = string.Empty;
    public double CompliancePct { get; set; }
    public string Rationale { get; set; } = string.Empty;
}

public class BenchmarkReportViewModel
{
    public int AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string UserDisplay { get; set; } = string.Empty;
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

    public int ApplicableTotal { get; set; }
    public int Valid { get; set; }
    public int NotValid { get; set; }
    public int Pending { get; set; }
    public int GlobalNotApplicable { get; set; }
    public int UserMarkedNotApplicable { get; set; }

    public double CompliancePct { get; set; }
    public int SecurityScore { get; set; }
    public RiskLevel RiskLevel { get; set; }

    public IReadOnlyList<ChapterComplianceViewModel> Chapters { get; set; } = [];
    public IReadOnlyList<WeakAreaViewModel> WeakAreas { get; set; } = [];
}
