
using System.ComponentModel.DataAnnotations;

namespace AsvsSecurityAuditor.ViewModels.Assessment;

public class AssessmentListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}

public class AssessmentIndexViewModel
{
    public IReadOnlyList<AssessmentListItemViewModel> Assessments { get; set; } = [];
}

public class AssessmentCreateInputModel
{
    [Required, StringLength(256)]
    public string Title { get; set; } = string.Empty;
}
