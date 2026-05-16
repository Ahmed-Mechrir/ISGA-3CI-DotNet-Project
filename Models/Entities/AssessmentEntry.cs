using AsvsSecurityAuditor.Models.Enums;

namespace AsvsSecurityAuditor.Models.Entities;

public class AssessmentEntry
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }

    public int RequirementId { get; set; }
    public AsvsRequirementEntity? Requirement { get; set; }

    public AssessmentStatus Status { get; set; } = AssessmentStatus.Pending;

    public string? SourceCodeReference { get; set; }
    public string? Comment { get; set; }
    public string? ToolUsed { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}
