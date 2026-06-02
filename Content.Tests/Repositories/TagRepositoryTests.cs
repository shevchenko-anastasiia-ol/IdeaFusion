using ContentDAL.Repository;
using ContentDomain.Entity;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Content.Tests.Repositories;

public class TagRepositoryTests : IDisposable
{
    private readonly SqliteConnection _db = DbHelper.OpenDb();

    public void Dispose() => _db.Dispose();

    private TagRepository Repo() => new(_db);

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_SetsTagId()
    {
        var tag = new Tag { Name = "dotnet" };
        await Repo().AddAsync(tag);
        tag.TagId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_PersistsTagInDatabase()
    {
        var tag = new Tag { Name = "music" };
        await Repo().AddAsync(tag);

        var name = await _db.ExecuteScalarAsync<string>(
            "SELECT Name FROM Tags WHERE TagId = @Id", new { Id = tag.TagId });
        name.Should().Be("music");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesTag()
    {
        var tag = new Tag { Name = "to-delete" };
        await Repo().AddAsync(tag);

        await Repo().DeleteAsync(tag.TagId);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Tags WHERE TagId = @Id", new { Id = tag.TagId });
        count.Should().Be(0);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsTag()
    {
        var tag = new Tag { Name = "photography" };
        await Repo().AddAsync(tag);

        var result = await Repo().GetByIdAsync(tag.TagId);

        result.Should().NotBeNull();
        result!.Name.Should().Be("photography");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await Repo().GetByIdAsync(9999);
        result.Should().BeNull();
    }

    // ── GetByNameAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByNameAsync_WhenExists_ReturnsTag()
    {
        var tag = new Tag { Name = "travel" };
        await Repo().AddAsync(tag);

        var result = await Repo().GetByNameAsync("travel");

        result.Should().NotBeNull();
        result!.TagId.Should().Be(tag.TagId);
    }

    [Fact]
    public async Task GetByNameAsync_WhenNotExists_ReturnsNull()
    {
        var result = await Repo().GetByNameAsync("nonexistent");
        result.Should().BeNull();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllTags()
    {
        await Repo().AddAsync(new Tag { Name = "alpha" });
        await Repo().AddAsync(new Tag { Name = "beta" });
        await Repo().AddAsync(new Tag { Name = "gamma" });

        var result = (await Repo().GetAllAsync()).ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var result = await Repo().GetAllAsync();
        result.Should().BeEmpty();
    }

    // ── GetByPostIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPostIdAsync_ReturnsTagsForPost()
    {
        var t1 = new Tag { Name = "rock" };
        var t2 = new Tag { Name = "pop" };
        var t3 = new Tag { Name = "jazz" };
        await Repo().AddAsync(t1);
        await Repo().AddAsync(t2);
        await Repo().AddAsync(t3);

        await Repo().AddPostTagAsync(postId: 1, tagId: t1.TagId);
        await Repo().AddPostTagAsync(postId: 1, tagId: t2.TagId);
        await Repo().AddPostTagAsync(postId: 2, tagId: t3.TagId);

        var result = (await Repo().GetByPostIdAsync(postId: 1)).ToList();

        result.Should().HaveCount(2);
        result.Select(t => t.Name).Should().Contain("rock").And.Contain("pop");
    }

    // ── AddPostTagAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddPostTagAsync_InsertsPostTag()
    {
        var tag = new Tag { Name = "science" };
        await Repo().AddAsync(tag);

        await Repo().AddPostTagAsync(postId: 5, tagId: tag.TagId);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PostTags WHERE PostId = 5 AND TagId = @TagId",
            new { TagId = tag.TagId });
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddPostTagAsync_CalledTwice_IsIdempotent()
    {
        var tag = new Tag { Name = "art" };
        await Repo().AddAsync(tag);

        await Repo().AddPostTagAsync(postId: 6, tagId: tag.TagId);
        await Repo().AddPostTagAsync(postId: 6, tagId: tag.TagId); // duplicate — ON CONFLICT DO NOTHING

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PostTags WHERE PostId = 6 AND TagId = @TagId",
            new { TagId = tag.TagId });
        count.Should().Be(1);
    }

    // ── DeletePostTagsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeletePostTagsAsync_RemovesAllTagsForPost()
    {
        var t1 = new Tag { Name = "x" };
        var t2 = new Tag { Name = "y" };
        await Repo().AddAsync(t1);
        await Repo().AddAsync(t2);
        await Repo().AddPostTagAsync(postId: 7, tagId: t1.TagId);
        await Repo().AddPostTagAsync(postId: 7, tagId: t2.TagId);

        await Repo().DeletePostTagsAsync(postId: 7);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PostTags WHERE PostId = 7");
        count.Should().Be(0);
    }
}
