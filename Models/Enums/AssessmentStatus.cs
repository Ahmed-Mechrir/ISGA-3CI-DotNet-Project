namespace AsvsSecurityAuditor.Models.Enums;

/// <summary>User assessment status aligned with OWASP ASVS auditing vocabulary.</summary>
public enum AssessmentStatus
{
    Pending = 0,
    Valid = 1,
    NotValid = 2,
    NotApplicable = 3
}
