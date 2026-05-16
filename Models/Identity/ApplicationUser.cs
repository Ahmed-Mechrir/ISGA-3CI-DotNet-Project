using Microsoft.AspNetCore.Identity;

namespace AsvsSecurityAuditor.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
