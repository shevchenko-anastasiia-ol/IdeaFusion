namespace ContentDAL.Data;

using ContentDomain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

public class ContentDbContext(DbContextOptions<ContentDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostAuthor> PostAuthors { get; set; }
    public DbSet<CollaborationSnapshot> CollaborationSnapshots { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<PostTag> PostTags { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<PostView> PostViews { get; set; }
    public DbSet<SavedPost> SavedPosts { get; set; }
    public DbSet<PostMedia> PostMedia { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
 
        // ── CollaborationSnapshot (денормалізований кеш з CollaborationService) ──
        modelBuilder.Entity<CollaborationSnapshot>(entity =>
        {
            entity.HasKey(c => c.CollaborationSnapshotId);
 
            entity.HasIndex(c => c.CollaborationId).IsUnique();
 
            entity.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();
 
            entity.Property(c => c.AvatarUrl)
                .HasMaxLength(500);

            entity.Property(c => c.ExternalId)
                .HasMaxLength(100);

            entity.Property(c => c.SyncedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        });
 
        // ── PostAuthor (денормалізований кеш з UserService) ─────────────────
        modelBuilder.Entity<PostAuthor>(entity =>
        {
            entity.HasKey(a => a.PostAuthorId);
 
            entity.HasIndex(a => a.UserId).IsUnique();
 
            entity.Property(a => a.UserName)
                .HasMaxLength(100)
                .IsRequired();
 
            entity.Property(a => a.AvatarUrl)
                .HasMaxLength(500);
 
            entity.Property(a => a.SyncedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        });
 
        // ── Post ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(p => p.PostId);
 
            entity.Property(p => p.PostAuthorId)
                .IsRequired(false);
 
            entity.Property(p => p.CollaborationSnapshotId)
                .IsRequired(false);
 
            entity.Property(p => p.Title)
                .HasMaxLength(200)
                .IsRequired();
 
            entity.Property(p => p.Description)
                .HasMaxLength(2000);
 
            entity.Property(p => p.ExternalLink)
                .HasMaxLength(500);
 
            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValueSql("'Published'");
 
            entity.Property(p => p.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
 
            entity.Property(p => p.CreatedBy)
                .HasMaxLength(100);
 
            entity.Property(p => p.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);
 
            entity.Property(p => p.UpdatedBy)
                .HasMaxLength(100);
 
            entity.Property(p => p.IsDeleted)
                .HasDefaultValueSql("false");
            
 
            // один пост не може бути і особистим, і від колаборації одночасно
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_post_authororcollaboration",
                "(postauthorid IS NOT NULL AND collaborationsnapshotid IS NULL) OR " +
                "(postauthorid IS NULL AND collaborationsnapshotid IS NOT NULL)"));
 
            entity.HasOne(p => p.Author)
                .WithMany(a => a.Posts)
                .HasForeignKey(p => p.PostAuthorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
 
            entity.HasOne(p => p.Collaboration)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CollaborationSnapshotId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });
 
        // ── Tag ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(t => t.TagId);
 
            entity.HasIndex(t => t.Name).IsUnique();
 
            entity.Property(t => t.Name)
                .HasMaxLength(50)
                .IsRequired();
        });
 
        // ── PostTag (composite PK) ───────────────────────────────────────────
        modelBuilder.Entity<PostTag>(entity =>
        {
            entity.HasKey(pt => new { pt.PostId, pt.TagId });
 
            entity.HasOne(pt => pt.Post)
                .WithMany(p => p.PostTags)
                .HasForeignKey(pt => pt.PostId);
 
            entity.HasOne(pt => pt.Tag)
                .WithMany(t => t.PostTags)
                .HasForeignKey(pt => pt.TagId);
        });
 
        // ── Comment ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.CommentId);
 
            entity.Property(c => c.Body)
                .HasMaxLength(2000)
                .IsRequired();
 
            entity.Property(c => c.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
 
            entity.Property(c => c.CreatedBy)
                .HasMaxLength(100);
 
            entity.Property(c => c.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired(false);
 
            entity.Property(c => c.UpdatedBy)
                .HasMaxLength(100);
 
            entity.Property(c => c.IsDeleted)
                .HasDefaultValueSql("false");
            
 
            entity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);
 
            entity.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.PostAuthorId)
                .OnDelete(DeleteBehavior.Restrict);
 
            // самопосилання: відповідь на коментар
            entity.HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
 
        // ── Like ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(l => l.LikeId);
 
            // один юзер — один лайк на пост
            entity.HasIndex(l => new { l.PostId, l.UserId }).IsUnique();
 
            entity.Property(l => l.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
 
            entity.HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });
 
        // ── PostView ──────────────────────────────────────────────────────────
        modelBuilder.Entity<PostView>(entity =>
        {
            entity.HasKey(v => v.PostViewId);
 
            entity.Property(v => v.UserId)
                .IsRequired(false);
 
            entity.Property(v => v.ViewedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
 
            entity.HasOne(v => v.Post)
                .WithMany(p => p.Views)
                .HasForeignKey(v => v.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });
 
        // ── SavedPost ─────────────────────────────────────────────────────────
        modelBuilder.Entity<SavedPost>(entity =>
        {
            entity.HasKey(s => s.SavedPostId);
 
            // юзер зберігає пост лише один раз
            entity.HasIndex(s => new { s.PostId, s.UserId }).IsUnique();
 
            entity.Property(s => s.SavedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
 
            entity.HasOne(s => s.Post)
                .WithMany(p => p.SavedPosts)
                .HasForeignKey(s => s.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        // ── PostMedia ─────────────────────────────────────────────────────────
        modelBuilder.Entity<PostMedia>(entity =>
        {
            entity.HasKey(m => m.Id);
        
            entity.Property(m => m.Bucket)
                .HasMaxLength(100)
                .IsRequired();
        
            entity.Property(m => m.ObjectName)
                .HasMaxLength(500)
                .IsRequired();
        
            entity.Property(m => m.ContentType)
                .HasMaxLength(100)
                .IsRequired();
        
            entity.HasOne(m => m.Post)
                .WithMany(p => p.Media)
                .HasForeignKey(m => m.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.ApplyLowercaseNaming();
    }
}

public static class LowercaseNamingExtension
{
    public static void ApplyLowercaseNaming(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // таблиця
            entity.SetTableName(entity.GetTableName()!.ToLowerInvariant());
 
            // колонки
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToLowerInvariant());
            }
 
            // ключі
            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()!.ToLowerInvariant());
            }
 
            // foreign keys
            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName(fk.GetConstraintName()!.ToLowerInvariant());
            }
 
            // індекси
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()!.ToLowerInvariant());
            }
        }
    }
}
