using ContentDAL.Data;
using ContentDomain.Entity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Content.Tests.DAL;

public class ContentDbContextTests : IDisposable
{
    private readonly ContentDbContext _db;

    public ContentDbContextTests()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ContentDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    // ── DbSets exist ─────────────────────────────────────────────────────────

    [Fact] public void Posts_DbSet_IsNotNull()              => _db.Posts.Should().NotBeNull();
    [Fact] public void PostAuthors_DbSet_IsNotNull()        => _db.PostAuthors.Should().NotBeNull();
    [Fact] public void CollaborationSnapshots_DbSet_IsNotNull() => _db.CollaborationSnapshots.Should().NotBeNull();
    [Fact] public void Tags_DbSet_IsNotNull()               => _db.Tags.Should().NotBeNull();
    [Fact] public void PostTags_DbSet_IsNotNull()           => _db.PostTags.Should().NotBeNull();
    [Fact] public void Comments_DbSet_IsNotNull()           => _db.Comments.Should().NotBeNull();
    [Fact] public void Likes_DbSet_IsNotNull()              => _db.Likes.Should().NotBeNull();
    [Fact] public void PostViews_DbSet_IsNotNull()          => _db.PostViews.Should().NotBeNull();
    [Fact] public void SavedPosts_DbSet_IsNotNull()         => _db.SavedPosts.Should().NotBeNull();
    [Fact] public void PostMedia_DbSet_IsNotNull()          => _db.PostMedia.Should().NotBeNull();

    // ── CRUD ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CanSave_And_Retrieve_Tag()
    {
        _db.Tags.Add(new Tag { Name = "dotnet" });
        await _db.SaveChangesAsync();

        var tag = await _db.Tags.FirstAsync(t => t.Name == "dotnet");
        tag.Should().NotBeNull();
        tag.Name.Should().Be("dotnet");
    }

    [Fact]
    public async Task CanSave_And_Retrieve_PostAuthor()
    {
        _db.PostAuthors.Add(new PostAuthor { UserId = 1, UserName = "alice", SyncedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var author = await _db.PostAuthors.FirstAsync();
        author.UserName.Should().Be("alice");
    }

    [Fact]
    public async Task CanSave_And_Retrieve_CollaborationSnapshot()
    {
        _db.CollaborationSnapshots.Add(new CollaborationSnapshot
        {
            CollaborationId = 1,
            Name            = "Team Alpha",
            SyncedAt        = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var snap = await _db.CollaborationSnapshots.FirstAsync();
        snap.Name.Should().Be("Team Alpha");
    }

    [Fact]
    public async Task CanSave_And_Retrieve_Post_WithAuthor()
    {
        var author = new PostAuthor { UserId = 2, UserName = "bob", SyncedAt = DateTime.UtcNow };
        _db.PostAuthors.Add(author);
        await _db.SaveChangesAsync();

        _db.Posts.Add(new Post
        {
            PostAuthorId = author.PostAuthorId,
            Title        = "Hello World",
            CreatedAt    = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var post = await _db.Posts.FirstAsync();
        post.Title.Should().Be("Hello World");
    }

    // ── ApplyLowercaseNaming ──────────────────────────────────────────────────

    [Fact]
    public void Model_Post_TableName_IsLowercase()
    {
        var entity = _db.Model.FindEntityType(typeof(Post))!;
        var name = entity.GetTableName()!;
        name.Should().Be(name.ToLowerInvariant());
    }

    [Fact]
    public void Model_PostAuthor_TableName_IsLowercase()
    {
        var entity = _db.Model.FindEntityType(typeof(PostAuthor))!;
        var name = entity.GetTableName()!;
        name.Should().Be(name.ToLowerInvariant());
    }

    [Fact]
    public void Model_Tag_TableName_IsLowercase()
    {
        var entity = _db.Model.FindEntityType(typeof(Tag))!;
        var name = entity.GetTableName()!;
        name.Should().Be(name.ToLowerInvariant());
    }

    [Fact]
    public void Model_CollaborationSnapshot_TableName_IsLowercase()
    {
        var entity = _db.Model.FindEntityType(typeof(CollaborationSnapshot))!;
        var name = entity.GetTableName()!;
        name.Should().Be(name.ToLowerInvariant());
    }

    [Fact]
    public void Model_Comment_TableName_IsLowercase()
    {
        var entity = _db.Model.FindEntityType(typeof(Comment))!;
        var name = entity.GetTableName()!;
        name.Should().Be(name.ToLowerInvariant());
    }
}

public class LowercaseNamingExtensionTests : IDisposable
{
    private readonly ContentDbContext _db;

    public LowercaseNamingExtensionTests()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ContentDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    // ── Table names ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(typeof(Post))]
    [InlineData(typeof(PostAuthor))]
    [InlineData(typeof(Tag))]
    [InlineData(typeof(CollaborationSnapshot))]
    [InlineData(typeof(Comment))]
    [InlineData(typeof(Like))]
    [InlineData(typeof(PostView))]
    [InlineData(typeof(SavedPost))]
    [InlineData(typeof(PostMedia))]
    public void AllEntityTypes_TableNamesAreLowercase(Type entityType)
    {
        var entity = _db.Model.FindEntityType(entityType)!;
        var name   = entity.GetTableName()!;
        name.Should().Be(name.ToLowerInvariant());
    }

    // ── Column names ──────────────────────────────────────────────────────────

    [Fact]
    public void Post_ColumnNames_AreLowercase()
    {
        var entity  = _db.Model.FindEntityType(typeof(Post))!;
        var columns = entity.GetProperties().Select(p => p.GetColumnName()).ToList();

        columns.Should().AllSatisfy(c => c.Should().Be(c.ToLowerInvariant()));
    }

    [Fact]
    public void Tag_ColumnNames_AreLowercase()
    {
        var entity  = _db.Model.FindEntityType(typeof(Tag))!;
        var columns = entity.GetProperties().Select(p => p.GetColumnName()).ToList();

        columns.Should().AllSatisfy(c => c.Should().Be(c.ToLowerInvariant()));
    }

    [Fact]
    public void PostAuthor_ColumnNames_AreLowercase()
    {
        var entity  = _db.Model.FindEntityType(typeof(PostAuthor))!;
        var columns = entity.GetProperties().Select(p => p.GetColumnName()).ToList();

        columns.Should().AllSatisfy(c => c.Should().Be(c.ToLowerInvariant()));
    }

    // ── Key names ─────────────────────────────────────────────────────────────

    [Fact]
    public void Post_PrimaryKey_NameIsLowercase()
    {
        var entity = _db.Model.FindEntityType(typeof(Post))!;
        var pk     = entity.FindPrimaryKey()!;

        pk.GetName().Should().Be(pk.GetName()!.ToLowerInvariant());
    }
}
