using AsvsSecurityAuditor.Models.Entities;
using AsvsSecurityAuditor.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AsvsSecurityAuditor.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AsvsRequirementEntity> Requirements => Set<AsvsRequirementEntity>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentEntry> AssessmentEntries => Set<AssessmentEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AsvsRequirementEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RequirementRef).IsUnique();
            e.Property(x => x.RequirementRef).HasMaxLength(32);
            e.Property(x => x.Chapter).HasMaxLength(128);
            e.Property(x => x.Area).HasMaxLength(256);
            e.Property(x => x.LevelRaw).HasMaxLength(64);
            e.Property(x => x.Cwe).HasMaxLength(64);
            e.Property(x => x.Nist).HasMaxLength(128);
            e.Property(x => x.VerificationRequirement).HasMaxLength(4000);
        });

        builder.Entity<Assessment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.Title });
            e.Property(x => x.Title).HasMaxLength(256);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AssessmentEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AssessmentId, x.RequirementId }).IsUnique();
            e.HasOne(x => x.Assessment)
                .WithMany(a => a.Entries!)
                .HasForeignKey(x => x.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Requirement)
                .WithMany()
                .HasForeignKey(x => x.RequirementId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.SourceCodeReference).HasMaxLength(512);
            e.Property(x => x.Comment).HasMaxLength(2000);
            e.Property(x => x.ToolUsed).HasMaxLength(256);
        });
    }
}
