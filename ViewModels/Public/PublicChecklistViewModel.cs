namespace AsvsSecurityAuditor.ViewModels.Public;

public class PublicChecklistRowViewModel
{
    public string RequirementRef { get; set; } = string.Empty;
    public string Chapter { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int Level { get; set; }
    public string VerificationRequirement { get; set; } = string.Empty;
    public bool PreMarkedNotApplicable { get; set; }
}

public class LevelCountSummary
{
    public int Level { get; set; }
    public int Count { get; set; }
}

public class PublicChecklistPageViewModel
{
    public IReadOnlyList<PublicChecklistRowViewModel> Rows { get; set; } = [];
    public int TotalCount => Rows.Count;

    /// <summary>Distinct non-empty chapters (for sidebar + filter).</summary>
    public int DistinctChapterCount { get; set; }

    public IReadOnlyList<string> ChapterNamesSorted { get; set; } = [];

    public IReadOnlyList<LevelCountSummary> RequirementsPerLevel { get; set; } = [];
}
