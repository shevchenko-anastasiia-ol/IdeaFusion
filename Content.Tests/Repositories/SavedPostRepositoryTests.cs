using ContentDAL.Repository;
using ContentDomain.Entity;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Content.Tests.Repositories;

public class SavedPostRepositoryTests : IDisposable
{
    private readonly SqliteConnection _db = DbHelper.OpenDb();

    public void Dispose() => _db.Dispose();

    private SavedPostRepository Repo() => new(_db);

    private SavedPost Make(int postId = 1, int userId = 1) => new()
    {
        PostId  = postId,
        UserId  = userId,
        SavedAt = DateTime.UtcNow
    };

    private async Task SeedPostAsync(int postId, string title = "Post", int postAuthorId = 1, bool isDeleted = false)
    {
        // PostAuthorId must be non-null — Dapper treats Post as null when the splitOn column is null
        await _db.ExecuteAsync(
            "INSERT INTO Posts (PostId, PostAuthorId, Title, Status, CreatedAt, IsDeleted) VALUES (@Id, @AuthorId, @Title, 'Published', @Now, @Del)",
            new { Id = postId, AuthorId = postAuthorId, Title = title, Now = DateTime.UtcNow.ToString("o"), Del = isDeleted ? 1 : 0 });
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_SetsSavedPostId()
    {
        var sp = Make();
        await Repo().AddAsync(sp);
        sp.SavedPostId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_PersistsInDatabase()
    {
        var sp = Make(postId: 5, userId: 10);
        await Repo().AddAsync(sp);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM SavedPosts WHERE PostId = 5 AND UserId = 10");
        count.Should().Be(1);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesSavedPost()
    {
        var sp = Make(postId: 2, userId: 3);
        await Repo().AddAsync(sp);

        await Repo().DeleteAsync(postId: 2, userId: 3);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM SavedPosts WHERE PostId = 2 AND UserId = 3");
        count.Should().Be(0);
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        var sp = Make(postId: 7, userId: 4);
        await Repo().AddAsync(sp);

        var exists = await Repo().ExistsAsync(postId: 7, userId: 4);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        var exists = await Repo().ExistsAsync(postId: 9999, userId: 9999);
        exists.Should().BeFalse();
    }

    // ── CountByPostAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CountByPostAsync_ReturnsCorrectCount()
    {
        await Repo().AddAsync(Make(postId: 10, userId: 1));
        await Repo().AddAsync(Make(postId: 10, userId: 2));
        await Repo().AddAsync(Make(postId: 11, userId: 1));

        var count = await Repo().CountByPostAsync(postId: 10);
        count.Should().Be(2);
    }

    // ── GetByUserIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_ReturnsSavedPostsWithPost()
    {
        await SeedPostAsync(postId: 50, title: "Hello World");
        var sp = Make(postId: 50, userId: 20);
        await Repo().AddAsync(sp);

        var result = (await Repo().GetByUserIdAsync(userId: 20)).ToList();

        result.Should().HaveCount(1);
        result[0].PostId.Should().Be(50);
        result[0].Post.Should().NotBeNull();
        result[0].Post.Title.Should().Be("Hello World");
    }

    [Fact]
    public async Task GetByUserIdAsync_DoesNotReturnDeletedPosts()
    {
        await SeedPostAsync(postId: 60, title: "Deleted", isDeleted: true);
        await Repo().AddAsync(Make(postId: 60, userId: 30));

        var result = await Repo().GetByUserIdAsync(userId: 30);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNoSavedPosts_ReturnsEmpty()
    {
        var result = await Repo().GetByUserIdAsync(userId: 9999);
        result.Should().BeEmpty();
    }
}
