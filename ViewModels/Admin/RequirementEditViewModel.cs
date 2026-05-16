using System.ComponentModel.DataAnnotations;

namespace AsvsSecurityAuditor.ViewModels.Admin;

public class RequirementEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(32)]
    public string RequirementRef { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Chapter { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string Area { get; set; } = string.Empty;

    public int Level { get; set; }

    [StringLength(64)]
    public string LevelRaw { get; set; } = string.Empty;

    [StringLength(64)]
    public string Cwe { get; set; } = string.Empty;

    [StringLength(128)]
    public string Nist { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string VerificationRequirement { get; set; } = string.Empty;

    [Display(Name = "Marked N/A in OWASP source export")]
    public bool PreMarkedNotApplicable { get; set; }
}
