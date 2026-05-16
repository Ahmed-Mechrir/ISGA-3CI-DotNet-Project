namespace AsvsSecurityAuditor.Models.Entities;

/// <summary>OWASP ASVS requirement row stored in SQL Server (imported from CSV).</summary>
public class AsvsRequirementEntity
{
    public int Id { get; set; }

    /// <summary>OWASP reference, e.g. V1.1.1 or 1.1.1</summary>
    public string RequirementRef { get; set; } = string.Empty;

    public string Chapter { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int Level { get; set; }
    public string LevelRaw { get; set; } = string.Empty;
    public string Cwe { get; set; } = string.Empty;
    public string Nist { get; set; } = string.Empty;
    public string VerificationRequirement { get; set; } = string.Empty;

    /// <summary>When the source checklist marks the row as N/A globally.</summary>
    public bool PreMarkedNotApplicable { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}
