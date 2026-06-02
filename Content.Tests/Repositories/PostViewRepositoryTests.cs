using ContentDAL.Repository;
using ContentDomain.Entity;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Content.Tests.Repositories;

public class PostViewRepositoryTests : IDisposable
{
    private readonly SqliteConnection _db = DbHelper.OpenDb();

    public void Dispose() => _db.Dispose();

    private PostViewRepository Repo() => new(_db);

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_SetsPostViewId()
    {
        var view = new PostView { PostId = 1, UserId = 10, ViewedAt = DateTime.UtcNow };

        await Repo().AddAsync(view);

        view.PostViewId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_WithNullUserId_DoesNotThrow()
    {
        var view = new PostView { PostId = 2, UserId = null, ViewedAt = DateTime.UtcNow };

        Func<Task> act = () => Repo().AddAsync(view);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsViewInDatabase()
    {
        var view = new PostView { PostId = 5, UserId = 7, ViewedAt = DateTime.UtcNow };
        await Repo().AddAsync(view);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PostViews WHERE PostId = 5 AND UserId = 7");
        count.Should().Be(1);
    }

    // ── CountByPostAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CountByPostAsync_ReturnsCorrectCount()
    {
        await Repo().AddAsync(new PostView { PostId = 30, ViewedAt = DateTime.UtcNow });
        await Repo().AddAsync(new PostView { PostId = 30, ViewedAt = DateTime.UtcNow });
        await Repo().AddAsync(new PostView { PostId = 31, ViewedAt = DateTime.UtcNow });

        var count = await Repo().CountByPostAsync(postId: 30);
        count.Should().Be(2);
    }

    [Fact]
    public async Task CountByPostAsync_WhenNoViews_ReturnsZero()
    {
        var count = await Repo().CountByPostAsync(postId: 9999);
        count.Should().Be(0);
    }
}
