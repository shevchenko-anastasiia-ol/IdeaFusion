using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace IdentityDAL.Data;

public class IdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public DbSet<AuditLogEntry> AuditLogEntries { get; set; }
    public DbSet<SearchPreference> SearchPreferences { get; set; }

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SearchPreference>(entity =>
        {
            entity.ToTable("SearchPreferences");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.OwnsMany(e => e.SavedFilters);

            var valueComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            entity.Property(e => e.SearchHistory).HasConversion(
                v => string.Join(',', (IEnumerable<string>)v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(valueComparer);
        });

        builder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("AuditLogEntries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ActorId)
                .HasColumnName("ActorId");

            entity.Property(e => e.Action)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.TargetType)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(e => e.TargetId)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.ActorName)
                .HasMaxLength(256);

            entity.Property(e => e.TargetName)
                .HasMaxLength(256);

            entity.Property(e => e.Details)
                .HasMaxLength(4000);

            // The audit trail is append-only, so stored as a string rather than an
            // int: rows stay readable in psql and a reordering can't corrupt history.
            entity.Property(e => e.Category)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            // Audit reads are almost always "newest first, optionally per actor".
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ActorId);
            entity.HasIndex(e => e.Category);
        });
    }
}
