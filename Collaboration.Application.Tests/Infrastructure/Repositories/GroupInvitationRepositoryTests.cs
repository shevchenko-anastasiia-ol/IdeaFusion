using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Infrastructure.Repositories;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Repositories;

public class GroupInvitationRepositoryTests
{
    private readonly Mock<IMongoDatabase>                    _dbMock  = new();
    private readonly Mock<IMongoCollection<GroupInvitation>> _colMock = new();

    public GroupInvitationRepositoryTests()
    {
        _dbMock
            .Setup(d => d.GetCollection<GroupInvitation>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(_colMock.Object);
    }

    private GroupInvitationRepository CreateRepo() => new(_dbMock.Object);

    private static GroupInvitation CreateInvitation(
        string teamId = "team-1", string invitedUserId = "user-1", string invitedByUserId = "system") =>
        new(teamId, invitedUserId, invitedByUserId, "Developer", "Join us!", 7);

    private void SetupFindAsync(List<GroupInvitation> items)
    {
        var cursorMock = new Mock<IAsyncCursor<GroupInvitation>>();
        cursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.Count > 0)
            .ReturnsAsync(false);
        cursorMock.Setup(c => c.Current).Returns(items);

        _colMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<GroupInvitation>>(),
                It.IsAny<FindOptions<GroupInvitation, GroupInvitation>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);
    }

    private void SetupPagedFindAsync(List<GroupInvitation> items, long totalCount)
    {
        SetupFindAsync(items);
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<GroupInvitation>>(),
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
            It.IsAny<FilterDefinition<GroupInvitation>>(),
            It.IsAny<FindOptions<GroupInvitation, GroupInvitation>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByTeamIdAsync_WithValidTeamId_ReturnsInvitations()
    {
        var invitation = CreateInvitation("team-7");
        SetupFindAsync([invitation]);

        var result = await CreateRepo().GetByTeamIdAsync("team-7");

        result.Should().ContainSingle();
    }

    // ── GetByInvitedUserIdAsync ───────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByInvitedUserIdAsync_WithNullOrEmptyUserId_ReturnsEmptyWithoutDbCall(string? userId)
    {
        var result = await CreateRepo().GetByInvitedUserIdAsync(userId!);

        result.Should().BeEmpty();
        _colMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<GroupInvitation>>(),
            It.IsAny<FindOptions<GroupInvitation, GroupInvitation>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByInvitedUserIdAsync_WithValidUserId_ReturnsInvitations()
    {
        var invitation = CreateInvitation(invitedUserId: "user-42");
        SetupFindAsync([invitation]);

        var result = await CreateRepo().GetByInvitedUserIdAsync("user-42");

        result.Should().ContainSingle();
    }

    // ── GetByStatusAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByStatusAsync_ReturnsInvitationsMatchingStatus()
    {
        var invitation = CreateInvitation();
        SetupFindAsync([invitation]);

        var result = await CreateRepo().GetByStatusAsync("team-1", InvitationStatus.Pending);

        result.Should().ContainSingle();
    }

    // ── HasPendingInvitationAsync ─────────────────────────────────────────────

    [Fact]
    public async Task HasPendingInvitationAsync_WhenCountIsZero_ReturnsFalse()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<GroupInvitation>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var result = await CreateRepo().HasPendingInvitationAsync("team-1", "user-1");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPendingInvitationAsync_WhenCountIsPositive_ReturnsTrue()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<GroupInvitation>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var result = await CreateRepo().HasPendingInvitationAsync("team-1", "user-1");
        result.Should().BeTrue();
    }

    // ── GetExpiredAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetExpiredAsync_ReturnsExpiredPendingInvitations()
    {
        var invitation = CreateInvitation();
        SetupFindAsync([invitation]);

        var result = await CreateRepo().GetExpiredAsync();

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetExpiredAsync_WhenNoneExpired_ReturnsEmpty()
    {
        SetupFindAsync([]);

        var result = await CreateRepo().GetExpiredAsync();

        result.Should().BeEmpty();
    }

    // ── GetByCursorAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCursorAsync_WithNullCursor_ReturnsPagedList()
    {
        var invitation = CreateInvitation();
        SetupFindAsync([invitation]);

        var result = await CreateRepo().GetByCursorAsync(null, 10);

        result.Should().NotBeNull();
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByCursorAsync_WithCursor_CallsDatabaseAndReturnsPagedList()
    {
        SetupFindAsync([]);

        var result = await CreateRepo().GetByCursorAsync("cursor-id", 5);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCursorAsync_WhenItemsEqualPageSize_SetsNextCursor()
    {
        var invitations = Enumerable.Range(1, 2)
            .Select(i => CreateInvitation($"team-{i}", $"user-{i}"))
            .ToList();
        SetupFindAsync(invitations);

        var result = await CreateRepo().GetByCursorAsync(null, 2);

        result.Should().HaveCount(2);
        result.NextCursor.Should().NotBeNull();
    }

    // ── BuildFilter (unit tests for filter-building logic) ────────────────────

    [Fact]
    public void BuildFilter_WithNoArguments_ReturnsEmptyFilter()
    {
        var filter = GroupInvitationRepository.BuildFilter(null, null, null, null, null, null);

        filter.Should().NotBeNull();
        filter.Should().Be(Builders<GroupInvitation>.Filter.Empty);
    }

    [Fact]
    public void BuildFilter_WithTeamId_ReturnsNonEmptyFilter()
    {
        var filter = GroupInvitationRepository.BuildFilter("team-1", null, null, null, null, null);

        filter.Should().NotBeNull();
        filter.Should().NotBe(Builders<GroupInvitation>.Filter.Empty);
    }

    [Fact]
    public void BuildFilter_WithInvitedUserId_ReturnsNonEmptyFilter()
    {
        var filter = GroupInvitationRepository.BuildFilter(null, "user-1", null, null, null, null);

        filter.Should().NotBeNull();
        filter.Should().NotBe(Builders<GroupInvitation>.Filter.Empty);
    }

    [Fact]
    public void BuildFilter_WithInvitedByUserId_ReturnsNonEmptyFilter()
    {
        var filter = GroupInvitationRepository.BuildFilter(null, null, "admin-1", null, null, null);

        filter.Should().NotBeNull();
        filter.Should().NotBe(Builders<GroupInvitation>.Filter.Empty);
    }

    [Fact]
    public void BuildFilter_WithRole_ReturnsNonEmptyFilter()
    {
        var filter = GroupInvitationRepository.BuildFilter(null, null, null, "Developer", null, null);

        filter.Should().NotBeNull();
        filter.Should().NotBe(Builders<GroupInvitation>.Filter.Empty);
    }

    [Fact]
    public void BuildFilter_WithStatus_ReturnsNonEmptyFilter()
    {
        var filter = GroupInvitationRepository.BuildFilter(null, null, null, null, InvitationStatus.Pending, null);

        filter.Should().NotBeNull();
        filter.Should().NotBe(Builders<GroupInvitation>.Filter.Empty);
    }

    [Fact]
    public void BuildFilter_WithIsExpiredTrue_ReturnsNonEmptyFilter()
    {
        var filter = GroupInvitationRepository.BuildFilter(null, null, null, null, null, true);

        filter.Should().NotBeNull();
        filter.Should().NotBe(Builders<GroupInvitation>.Filter.Empty);
    }

    [Fact]
    public void BuildFilter_WithIsExpiredFalse_ReturnsNonEmptyFilter()
    {
        var filter = GroupInvitationRepository.BuildFilter(null, null, null, null, null, false);

        filter.Should().NotBeNull();
        filter.Should().NotBe(Builders<GroupInvitation>.Filter.Empty);
    }

    [Fact]
    public void BuildFilter_WithAllFilters_ReturnsNonEmptyFilter()
    {
        var filter = GroupInvitationRepository.BuildFilter(
            "team-1", "user-1", "admin-1", "Developer", InvitationStatus.Accepted, true);

        filter.Should().NotBeNull();
        filter.Should().NotBe(Builders<GroupInvitation>.Filter.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildFilter_WithWhitespaceTeamId_ReturnsEmptyFilter(string? teamId)
    {
        var filter = GroupInvitationRepository.BuildFilter(teamId, null, null, null, null, null);

        filter.Should().Be(Builders<GroupInvitation>.Filter.Empty);
    }

    // ── GetPagedFilteredAsync (integration via mocked collection) ─────────────

    [Fact]
    public async Task GetPagedFilteredAsync_WithNoFilters_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            null, null, null, null, null, null, 1, 10);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithTeamIdFilter_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            "team-1", null, null, null, null, null, 1, 10);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithStatusFilter_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            null, null, null, null, InvitationStatus.Accepted, null, 1, 10);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithIsExpiredTrue_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            null, null, null, null, null, true, 1, 10);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithIsExpiredFalse_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            null, null, null, null, null, false, 1, 10);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithAscendingSort_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            null, null, null, null, null, null, 1, 10, ascending: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithAllFilters_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            "team-1", "user-1", "admin-1", "Developer",
            InvitationStatus.Pending, true, 1, 10, ascending: false);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithItems_ReturnsTotalCount()
    {
        var invitations = Enumerable.Range(1, 3)
            .Select(i => CreateInvitation($"team-{i}", $"user-{i}"))
            .ToList();
        SetupPagedFindAsync(invitations, 3L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            null, null, null, null, null, null, 1, 10);

        result.TotalCount.Should().Be(3);
    }
}
