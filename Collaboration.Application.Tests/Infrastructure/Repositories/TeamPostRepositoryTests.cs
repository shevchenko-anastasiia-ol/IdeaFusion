using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using Collaboration.Infrastructure.Repositories;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Repositories;

public class TeamPostRepositoryTests
{
    private readonly Mock<IMongoDatabase>               _dbMock  = new();
    private readonly Mock<IMongoCollection<TeamPost>>   _colMock = new();

    public TeamPostRepositoryTests()
    {
        _dbMock
            .Setup(d => d.GetCollection<TeamPost>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(_colMock.Object);
    }

    private TeamPostRepository CreateRepo() => new(_dbMock.Object);

    private static UserSnapshot Author(string id = "author-1") => new(id, $"User{id}");

    private static TeamPost CreatePost(string postId = "post-1", string teamId = "team-1") =>
        new(postId, teamId, Author(), "Sample Title");

    private void SetupFindAsync(List<TeamPost> items)
    {
        var cursorMock = new Mock<IAsyncCursor<TeamPost>>();
        cursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.Count > 0)
            .ReturnsAsync(false);
        cursorMock.Setup(c => c.Current).Returns(items);

        _colMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TeamPost>>(),
                It.IsAny<FindOptions<TeamPost, TeamPost>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);
    }

    private void SetupPagedFindAsync(List<TeamPost> items, long totalCount)
    {
        SetupFindAsync(items);
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<TeamPost>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalCount);
    }

    // ── GetByPostIdAsync ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByPostIdAsync_WithNullOrEmptyId_ReturnsNullWithoutDbCall(string? postId)
    {
        var result = await CreateRepo().GetByPostIdAsync(postId!);

        result.Should().BeNull();
        _colMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<TeamPost>>(),
            It.IsAny<FindOptions<TeamPost, TeamPost>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByPostIdAsync_WithValidId_QueriesDatabase()
    {
        var post = CreatePost("post-42");
        SetupFindAsync([post]);

        await CreateRepo().GetByPostIdAsync("post-42");

        _colMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<TeamPost>>(),
            It.IsAny<FindOptions<TeamPost, TeamPost>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetByTeamIdAsync ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByTeamIdAsync_WithNullOrEmptyTeamId_ReturnsEmptyWithoutDbCall(string? teamId)
    {
        var result = await CreateRepo().GetByTeamIdAsync(teamId!);

        result.Should().BeEmpty();
        _colMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<TeamPost>>(),
            It.IsAny<FindOptions<TeamPost, TeamPost>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByTeamIdAsync_WithValidTeamId_ReturnsPosts()
    {
        var post = CreatePost(teamId: "team-99");
        SetupFindAsync([post]);

        var result = await CreateRepo().GetByTeamIdAsync("team-99");

        result.Should().ContainSingle();
    }

    // ── GetByAuthorIdAsync ────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByAuthorIdAsync_WithNullOrEmptyUserId_ReturnsEmptyWithoutDbCall(string? userId)
    {
        var result = await CreateRepo().GetByAuthorIdAsync(userId!);

        result.Should().BeEmpty();
        _colMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<TeamPost>>(),
            It.IsAny<FindOptions<TeamPost, TeamPost>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByAuthorIdAsync_WithValidUserId_ReturnsPosts()
    {
        var post = CreatePost();
        SetupFindAsync([post]);

        var result = await CreateRepo().GetByAuthorIdAsync("author-1");

        result.Should().ContainSingle();
    }

    // ── ExistsByPostIdAsync ───────────────────────────────────────────────────

    [Theory]
    [InlineData(null,    "team-1")]
    [InlineData("",      "team-1")]
    [InlineData("post-1", null)]
    [InlineData("post-1", "")]
    public async Task ExistsByPostIdAsync_WithNullOrEmptyInput_ReturnsFalseWithoutDbCall(string? postId, string? teamId)
    {
        var result = await CreateRepo().ExistsByPostIdAsync(postId!, teamId!);

        result.Should().BeFalse();
        _colMock.Verify(
            c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<TeamPost>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExistsByPostIdAsync_WhenCountIsPositive_ReturnsTrue()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<TeamPost>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var result = await CreateRepo().ExistsByPostIdAsync("post-1", "team-1");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByPostIdAsync_WhenCountIsZero_ReturnsFalse()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<TeamPost>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var result = await CreateRepo().ExistsByPostIdAsync("post-1", "team-1");
        result.Should().BeFalse();
    }

    // ── UpdateAuthorSnapshotAsync ─────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAuthorSnapshotAsync_WithNullOrEmptyUserId_ReturnsWithoutDbCall(string? userId)
    {
        await CreateRepo().UpdateAuthorSnapshotAsync(userId!, "newname", null);

        _colMock.Verify(
            c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<TeamPost>>(),
                It.IsAny<UpdateDefinition<TeamPost>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAuthorSnapshotAsync_WithValidUserId_CallsUpdateMany()
    {
        var updateResult = new Mock<UpdateResult>();
        updateResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        updateResult.SetupGet(r => r.ModifiedCount).Returns(2);
        _colMock
            .Setup(c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<TeamPost>>(),
                It.IsAny<UpdateDefinition<TeamPost>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult.Object);

        await CreateRepo().UpdateAuthorSnapshotAsync("author-1", "NewName", "https://avatar.url");

        _colMock.Verify(
            c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<TeamPost>>(),
                It.IsAny<UpdateDefinition<TeamPost>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetByCursorAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCursorAsync_WithNullCursor_ReturnsPagedList()
    {
        var post = CreatePost();
        SetupFindAsync([post]);

        var result = await CreateRepo().GetByCursorAsync(null, 10);

        result.Should().NotBeNull();
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByCursorAsync_WithCursor_ReturnsPagedList()
    {
        SetupFindAsync([]);

        var result = await CreateRepo().GetByCursorAsync("cursor-id", 5);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // ── GetByTeamPagedAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByTeamPagedAsync_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetByTeamPagedAsync("team-1", 1, 10);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
    }
}