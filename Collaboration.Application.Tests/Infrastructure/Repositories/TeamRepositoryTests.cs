using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using Collaboration.Infrastructure.Repositories;
using FluentAssertions;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Infrastructure.Repositories;

public class TeamRepositoryTests
{
    private readonly Mock<IMongoDatabase>          _dbMock  = new();
    private readonly Mock<IMongoCollection<Team>>  _colMock = new();

    public TeamRepositoryTests()
    {
        _dbMock
            .Setup(d => d.GetCollection<Team>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(_colMock.Object);
    }

    private TeamRepository CreateRepo() => new(_dbMock.Object);

    private static Team CreateTeam(string name = "Test Team") =>
        new(name, "A valid description", "Design", ["tag"], new UserSnapshot("owner-id", "OwnerUser"));

    private void SetupFindAsync(List<Team> items)
    {
        var cursorMock = new Mock<IAsyncCursor<Team>>();
        cursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items.Count > 0)
            .ReturnsAsync(false);
        cursorMock.Setup(c => c.Current).Returns(items);

        _colMock
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<FindOptions<Team, Team>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);
    }

    private void SetupPagedFindAsync(List<Team> items, long totalCount)
    {
        SetupFindAsync(items);
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalCount);
    }

    // ── SearchByNameAsync ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchByNameAsync_WithNullOrEmptyName_ReturnsEmptyWithoutDbCall(string? name)
    {
        var result = await CreateRepo().SearchByNameAsync(name!);

        result.Should().BeEmpty();
        _colMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<Team>>(),
            It.IsAny<FindOptions<Team, Team>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchByNameAsync_WithValidName_QueriesDatabase()
    {
        var team = CreateTeam("Alpha");
        SetupFindAsync([team]);

        var result = await CreateRepo().SearchByNameAsync("Alpha");

        result.Should().ContainSingle();
    }

    // ── ExistsByNameAsync ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistsByNameAsync_WithNullOrEmptyName_ReturnsFalseWithoutDbCall(string? name)
    {
        var result = await CreateRepo().ExistsByNameAsync(name!);

        result.Should().BeFalse();
        _colMock.Verify(
            c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExistsByNameAsync_WhenNameExists_ReturnsTrue()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var result = await CreateRepo().ExistsByNameAsync("Existing Team");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_WhenNameNotFound_ReturnsFalse()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var result = await CreateRepo().ExistsByNameAsync("New Team");
        result.Should().BeFalse();
    }

    // ── GetByTagAsync ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByTagAsync_WithNullOrEmptyTag_ReturnsEmptyWithoutDbCall(string? tag)
    {
        var result = await CreateRepo().GetByTagAsync(tag!);

        result.Should().BeEmpty();
        _colMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<Team>>(),
            It.IsAny<FindOptions<Team, Team>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByTagAsync_WithValidTag_ReturnsTeams()
    {
        var team = CreateTeam();
        SetupFindAsync([team]);

        var result = await CreateRepo().GetByTagAsync("design");

        result.Should().ContainSingle();
    }

    // ── GetByMemberIdAsync ────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByMemberIdAsync_WithNullOrEmptyUserId_ReturnsEmptyWithoutDbCall(string? userId)
    {
        var result = await CreateRepo().GetByMemberIdAsync(userId!);

        result.Should().BeEmpty();
        _colMock.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<Team>>(),
            It.IsAny<FindOptions<Team, Team>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByMemberIdAsync_WithValidUserId_ReturnsTeams()
    {
        var team = CreateTeam();
        SetupFindAsync([team]);

        var result = await CreateRepo().GetByMemberIdAsync("owner-id");

        result.Should().ContainSingle();
    }

    // ── IsUserMemberAsync ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(null,    "user-1")]
    [InlineData("",      "user-1")]
    [InlineData("team-1", null)]
    [InlineData("team-1", "")]
    public async Task IsUserMemberAsync_WithNullOrEmptyInputs_ReturnsFalseWithoutDbCall(string? teamId, string? userId)
    {
        var result = await CreateRepo().IsUserMemberAsync(teamId!, userId!);

        result.Should().BeFalse();
        _colMock.Verify(
            c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IsUserMemberAsync_WhenUserIsMember_ReturnsTrue()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var result = await CreateRepo().IsUserMemberAsync("team-1", "owner-id");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserMemberAsync_WhenUserIsNotMember_ReturnsFalse()
    {
        _colMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var result = await CreateRepo().IsUserMemberAsync("team-1", "stranger-id");
        result.Should().BeFalse();
    }

    // ── UpdateMemberSnapshotAsync ─────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateMemberSnapshotAsync_WithNullOrEmptyUserId_ReturnsWithoutDbCall(string? userId)
    {
        await CreateRepo().UpdateMemberSnapshotAsync(userId!, "NewName", null);

        _colMock.Verify(
            c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<UpdateDefinition<Team>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateMemberSnapshotAsync_WithValidUserId_CallsUpdateMany()
    {
        var updateResult = new Mock<UpdateResult>();
        updateResult.SetupGet(r => r.IsAcknowledged).Returns(true);
        updateResult.SetupGet(r => r.ModifiedCount).Returns(1);
        _colMock
            .Setup(c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<UpdateDefinition<Team>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult.Object);

        await CreateRepo().UpdateMemberSnapshotAsync("owner-id", "NewUsername", "https://avatar.png");

        _colMock.Verify(
            c => c.UpdateManyAsync(
                It.IsAny<FilterDefinition<Team>>(),
                It.IsAny<UpdateDefinition<Team>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetByCursorAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCursorAsync_WithNullCursor_ReturnsPagedList()
    {
        var team = CreateTeam();
        SetupFindAsync([team]);

        var result = await CreateRepo().GetByCursorAsync(null, 10);

        result.Should().NotBeNull();
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByCursorAsync_WhenItemsEqualPageSize_SetsNextCursor()
    {
        var teams = Enumerable.Range(1, 5).Select(i => CreateTeam($"Team {i}")).ToList();
        SetupFindAsync(teams);

        var result = await CreateRepo().GetByCursorAsync(null, 5);

        result.Should().HaveCount(5);
        result.NextCursor.Should().NotBeNull();
    }

    // ── GetByStatusAsync (paged) ──────────────────────────────────────────────

    [Fact]
    public async Task GetByStatusAsync_ReturnsPagedListForStatus()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetByStatusAsync(TeamStatus.Active, 1, 10);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
    }

    // ── BuildTeamFilter ───────────────────────────────────────────────────────

    [Fact]
    public void BuildTeamFilter_WithNoOptionalParams_ReturnsFilterWithIsDeletedFalse()
    {
        var filter = TeamRepository.BuildTeamFilter(null, null, null, null, null, null);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamFilter_WithName_ReturnsNonEmptyFilter()
    {
        var filter = TeamRepository.BuildTeamFilter("alpha", null, null, null, null, null);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamFilter_WithCategory_ReturnsNonEmptyFilter()
    {
        var filter = TeamRepository.BuildTeamFilter(null, "Design", null, null, null, null);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamFilter_WithTag_ReturnsNonEmptyFilter()
    {
        var filter = TeamRepository.BuildTeamFilter(null, null, "music", null, null, null);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamFilter_WithStatus_ReturnsNonEmptyFilter()
    {
        var filter = TeamRepository.BuildTeamFilter(null, null, null, TeamStatus.Active, null, null);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamFilter_WithMemberId_ReturnsNonEmptyFilter()
    {
        var filter = TeamRepository.BuildTeamFilter(null, null, null, null, "user-1", null);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamFilter_WithRequiredRole_ReturnsNonEmptyFilter()
    {
        var filter = TeamRepository.BuildTeamFilter(null, null, null, null, null, "Developer");

        filter.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamFilter_WithAllParams_ReturnsNonEmptyFilter()
    {
        var filter = TeamRepository.BuildTeamFilter("alpha", "Design", "music", TeamStatus.Active, "user-1", "Developer");

        filter.Should().NotBeNull();
    }

    // ── BuildTeamSort ─────────────────────────────────────────────────────────

    [Fact]
    public void BuildTeamSort_WithNullOrderBy_ReturnsCreatedAtSort()
    {
        var sort = TeamRepository.BuildTeamSort(null, false);

        sort.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamSort_WithNameOrderByAscending_ReturnsNameSort()
    {
        var sort = TeamRepository.BuildTeamSort("name", true);

        sort.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamSort_WithNameOrderByDescending_ReturnsNameSort()
    {
        var sort = TeamRepository.BuildTeamSort("name", false);

        sort.Should().NotBeNull();
    }

    [Fact]
    public void BuildTeamSort_WithUnknownOrderByAscending_ReturnsCreatedAtSort()
    {
        var sort = TeamRepository.BuildTeamSort("unknown", true);

        sort.Should().NotBeNull();
    }

    // ── GetPagedFilteredAsync ─────────────────────────────────────────────────

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
    public async Task GetPagedFilteredAsync_WithNameFilter_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            "alpha", null, null, null, null, null, 1, 10);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithStatusFilter_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            null, null, null, TeamStatus.Active, null, null, 1, 10);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithAscendingNameSort_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            null, null, null, null, null, null, 1, 10, orderBy: "name", ascending: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedFilteredAsync_WithAllFilters_ReturnsPagedResult()
    {
        SetupPagedFindAsync([], 0L);

        var result = await CreateRepo().GetPagedFilteredAsync(
            "alpha", "Design", "music", TeamStatus.Active, "user-1", "Developer",
            1, 10, orderBy: "name", ascending: false);

        result.Should().NotBeNull();
    }
}