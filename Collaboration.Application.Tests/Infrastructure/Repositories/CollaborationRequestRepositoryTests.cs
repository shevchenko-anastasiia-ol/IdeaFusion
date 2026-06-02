using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using Collaboration.Infrastructure.Repositories;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Repositories;

public class CollaborationRequestRepositoryTests
{
    private readonly Mock<IMongoDatabase>                          _dbMock  = new();
    private readonly Mock<IMongoCollection<CollaborationRequest>>  _colMock = new();

    public CollaborationRequestRepositoryTests()
    {
        _dbMock
            .Setup(d => d.GetCollection<CollaborationRequest>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(_colMock.Object);
    }

    private CollaborationRequestRepository CreateRepo() => new(_dbMock.Object);

    private static CollaborationRequest CreateRequest(string teamId = "team-1", string fromUserId = "user-1") =>
        new(teamId, fromUserId, "Developer", "Hello", null);

    private void SetupFindAsync(List<CollaborationRequest> items)
    {
        var cursorMock = new Mock<IAsyncCursor<CollaborationRequest>>();
        cursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.Count > 0)
            .ReturnsAsync(false);
        cursorMock.Setup(c => c.Current).Returns(items);

        _colMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<CollaborationRequest>>(),
                It.IsAny<FindOptions<CollaborationRequest, CollaborationRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);
    }

    private void SetupPagedFindAsync(List<CollaborationRequest> items, long totalCount)
    {
        SetupFindAsync(items);
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<CollaborationRequest>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalCount);
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
            It.IsAny<FilterDefinition<CollaborationRequest>>(),
            It.IsAny<FindOptions<CollaborationRequest, CollaborationRequest>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByTeamIdAsync_WithValidTeamId_ReturnsRequests()
    {
        var request = CreateRequest("team-1");
        SetupFindAsync([request]);

        var result = await CreateRepo().GetByTeamIdAsync("team-1");

        result.Should().ContainSingle();
    }

    // ── GetByFromUserIdAsync ──────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByFromUserIdAsync_WithNullOrEmptyUserId_ReturnsEmptyWithoutDbCall(string? userId)
    {
        var result = await CreateRepo().GetByFromUserIdAsync(userId!);

        result.Should().BeEmpty();
        _colMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<CollaborationRequest>>(),
            It.IsAny<FindOptions<CollaborationRequest, CollaborationRequest>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByFromUserIdAsync_WithValidUserId_ReturnsRequests()
    {
        var request = CreateRequest(fromUserId: "user-42");
        SetupFindAsync([request]);

        var result = await CreateRepo().GetByFromUserIdAsync("user-42");

        result.Should().ContainSingle();
    }

    // ── GetByStatusAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByStatusAsync_ReturnsRequestsMatchingStatus()
    {
        var request = CreateRequest();
        SetupFindAsync([request]);

        var result = await CreateRepo().GetByStatusAsync("team-1", CollaborationRequestStatus.Pending);

        result.Should().ContainSingle();
    }

    // ── HasPendingRequestAsync ────────────────────────────────────────────────

    [Fact]
    public async Task HasPendingRequestAsync_WhenCountIsZero_ReturnsFalse()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<CollaborationRequest>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var result = await CreateRepo().HasPendingRequestAsync("team-1", "user-1", "Developer");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPendingRequestAsync_WhenCountIsPositive_ReturnsTrue()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<CollaborationRequest>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var result = await CreateRepo().HasPendingRequestAsync("team-1", "user-1", "Developer");
        result.Should().BeTrue();
    }

    // ── GetByCursorAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCursorAsync_WithNullCursor_QueriesDatabaseAndReturnsPagedList()
    {
        var request = CreateRequest();
        SetupFindAsync([request]);

        var result = await CreateRepo().GetByCursorAsync(null, 10);

        result.Should().NotBeNull();
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByCursorAsync_WithCursor_CallsDatabaseAndReturnsPagedList()
    {
        SetupFindAsync([]);

        var result = await CreateRepo().GetByCursorAsync("some-cursor-id", 5);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCursorAsync_WhenItemsEqualPageSize_SetsNextCursor()
    {
        var requests = Enumerable.Range(1, 3)
            .Select(i => CreateRequest($"team-{i}", $"user-{i}"))
            .ToList();
        SetupFindAsync(requests);

        var result = await CreateRepo().GetByCursorAsync(null, 3);

        result.Should().HaveCount(3);
        result.NextCursor.Should().NotBeNull();
    }

    // ── GetPagedFilteredAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedFilteredAsync_WithAllNullFilters_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            null, null, null, null, null, 1, 10);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
    }
}