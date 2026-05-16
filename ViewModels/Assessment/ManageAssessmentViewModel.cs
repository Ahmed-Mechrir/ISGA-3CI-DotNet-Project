using AsvsSecurityAuditor.Models.Enums;

namespace AsvsSecurityAuditor.ViewModels.Assessment;

public class ManageAssessmentRowViewModel
{
    public int RequirementEntityId { get; set; }
    public string RequirementRef { get; set; } = string.Empty;
    public string Chapter { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int Level { get; set; }
    public string RequirementText { get; set; } = string.Empty;
    public bool SourceNotApplicable { get; set; }
    public AssessmentStatus Status { get; set; }
    public string? SourceCodeReference { get; set; }
    public string? Comment { get; set; }
    public string? ToolUsed { get; set; }
}

public class ManageAssessmentPageViewModel
{
    public int AssessmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<ManageAssessmentRowViewModel> Rows { get; set; } = [];
}
