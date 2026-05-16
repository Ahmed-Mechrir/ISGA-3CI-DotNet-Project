using AsvsSecurityAuditor.Models.Identity;

namespace AsvsSecurityAuditor.Models.Entities;

/// <summary>Named assessment workbook per user.</summary>
public class Assessment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }

    public ICollection<AssessmentEntry>? Entries { get; set; }
}
