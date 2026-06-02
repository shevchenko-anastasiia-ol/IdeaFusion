using ContentDAL.Repository;
using ContentDomain.Entity;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Content.Tests.Repositories;

public class LikeRepositoryTests : IDisposable
{
    private readonly SqliteConnection _db = DbHelper.OpenDb();

    public void Dispose() => _db.Dispose();

    private LikeRepository Repo() => new(_db);

    private Like MakeLike(int postId = 1, int userId = 1) => new()
    {
        PostId    = postId,
        UserId    = userId,
        CreatedAt = DateTime.UtcNow
    };

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_SetsLikeId()
    {
        var like = MakeLike();
        await Repo().AddAsync(like);
        like.LikeId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_PersistsLikeInDatabase()
    {
        var like = MakeLike(postId: 5, userId: 10);
        await Repo().AddAsync(like);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Likes WHERE PostId = 5 AND UserId = 10");
        count.Should().Be(1);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesLike()
    {
        var like = MakeLike(postId: 2, userId: 3);
        await Repo().AddAsync(like);

        await Repo().DeleteAsync(postId: 2, userId: 3);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Likes WHERE PostId = 2 AND UserId = 3");
        count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentLike_DoesNotThrow()
    {
        Func<Task> act = () => Repo().DeleteAsync(postId: 9999, userId: 9999);
        await act.Should().NotThrowAsync();
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_WhenLikeExists_ReturnsTrue()
    {
        var like = MakeLike(postId: 7, userId: 4);
        await Repo().AddAsync(like);

        var exists = await Repo().ExistsAsync(postId: 7, userId: 4);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenLikeNotExists_ReturnsFalse()
    {
        var exists = await Repo().ExistsAsync(postId: 9999, userId: 9999);
        exists.Should().BeFalse();
    }

    // ── CountByPostAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CountByPostAsync_ReturnsCorrectCount()
    {
        await Repo().AddAsync(MakeLike(postId: 10, userId: 1));
        await Repo().AddAsync(MakeLike(postId: 10, userId: 2));
        await Repo().AddAsync(MakeLike(postId: 99, userId: 1));

        var count = await Repo().CountByPostAsync(postId: 10);
        count.Should().Be(2);
    }

    [Fact]
    public async Task CountByPostAsync_WhenNoLikes_ReturnsZero()
    {
        var count = await Repo().CountByPostAsync(postId: 9999);
        count.Should().Be(0);
    }

    // ── GetByPostIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPostIdAsync_ReturnsLikesForPost()
    {
        await Repo().AddAsync(MakeLike(postId: 20, userId: 1));
        await Repo().AddAsync(MakeLike(postId: 20, userId: 2));
        await Repo().AddAsync(MakeLike(postId: 21, userId: 1));

        var result = (await Repo().GetByPostIdAsync(postId: 20)).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(l => l.PostId.Should().Be(20));
    }
}
