using AsvsSecurityAuditor.Models.Enums;

namespace AsvsSecurityAuditor.DTOs;

public class ExplainRequestDto
{
    public string? Model { get; set; }
    public string? ApiKey { get; set; }
    public string? Technology { get; set; }
}

public class ExplainResponseDto
{
    public required string Explanation { get; set; }
    public required string Model { get; set; }
    public required string RequirementRef { get; set; }
}

public class AssessmentEntryPatchDto
{
    public AssessmentStatus Status { get; set; }
    public string? SourceCodeReference { get; set; }
    public string? Comment { get; set; }
    public string? ToolUsed { get; set; }
}

public class DashboardStatsDto
{
    public int ApplicableTotal { get; set; }
    public int Valid { get; set; }
    public int NotValid { get; set; }
    public int Pending { get; set; }
    public int UserNotApplicable { get; set; }
    public double CompliancePct { get; set; }
    public int SecurityScore { get; set; }
    public IReadOnlyList<ChapterComplianceDto> Chapters { get; set; } = [];
}

public class ChapterComplianceDto
{
    public string Chapter { get; set; } = string.Empty;
    public int Applicable { get; set; }
    public int Valid { get; set; }
    public double CompliancePct { get; set; }
}

public class ImportResultDto
{
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];
    public bool Success => Errors.Count == 0;
}
