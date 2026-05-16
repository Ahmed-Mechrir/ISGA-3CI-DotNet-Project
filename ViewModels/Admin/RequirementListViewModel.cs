namespace AsvsSecurityAuditor.ViewModels.Admin;

public class RequirementListViewModel
{
    public int Id { get; set; }
    public string RequirementRef { get; set; } = string.Empty;
    public string Chapter { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool PreMarkedNotApplicable { get; set; }
}
