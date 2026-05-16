using AsvsSecurityAuditor.Data;
using AsvsSecurityAuditor.Models.Identity;
using AsvsSecurityAuditor.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AsvsSecurityAuditor.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        foreach (var role in new[] { Roles.Admin, Roles.Auditor })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var email = config["AdminSeed:Email"];
        var password = config["AdminSeed:Password"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            var admin = await userManager.FindByEmailAsync(email);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = "Administrator"
                };
                var result = await userManager.CreateAsync(admin, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, Roles.Admin);
                    await userManager.AddToRoleAsync(admin, Roles.Auditor);
                    logger.LogInformation("Seeded admin account {Email}", email);
                }
                else
                    logger.LogWarning("Admin seed failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
