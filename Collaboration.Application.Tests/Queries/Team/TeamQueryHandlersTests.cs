using Collaboration.Application.Queries.Team;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Entities.Parameters;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Queries.Team;

public class TeamQueryHandlersTests
{
    private readonly Mock<ITeamRepository> _repoMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static Domain.Entities.Team CreateTeam(string name = "Test Team")
    {
        var owner = new UserSnapshot("owner-id", "owner-username");
        return new Domain.Entities.Team(name, "A valid description", "Design", ["design"], owner);
    }

    private static PagedList<Domain.Entities.Team> CreatePagedList(params Domain.Entities.Team[] teams) =>
        new(teams.ToList(), teams.Length, 1, 10);

    // ── GetTeamByIdQueryHandler ───────────────────────────────────────────────

    [Fact]
    public async Task GetTeamById_WhenTeamExists_ReturnsTeam()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        var handler = new GetTeamByIdQueryHandler(_repoMock.Object);
        var result = await handler.Handle(new GetTeamByIdQuery { TeamId = "team-1" }, _ct);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Team");
    }

    [Fact]
    public async Task GetTeamById_WhenTeamNotFound_ThrowsDomainException()
    {
        _repoMock.Setup(r => r.GetByIdAsync("missing", _ct)).ReturnsAsync((Domain.Entities.Team?)null);

        var handler = new GetTeamByIdQueryHandler(_repoMock.Object);

        await handler.Invoking(h => h.Handle(new GetTeamByIdQuery { TeamId = "missing" }, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetTeamById_WhenTeamIsDeleted_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.MarkAsDeleted();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        var handler = new GetTeamByIdQueryHandler(_repoMock.Object);

        await handler.Invoking(h => h.Handle(new GetTeamByIdQuery { TeamId = "team-1" }, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    // ── GetTeamsPagedQueryHandler ─────────────────────────────────────────────

    [Fact]
    public async Task GetTeamsPaged_ReturnsPagedListFromRepository()
    {
        var team = CreateTeam();
        var paged = CreatePagedList(team);
        _repoMock.Setup(r => r.GetPagedFilteredAsync(
                null, null, null, null, null, null,
                1, 10, "Id", false, _ct))
            .ReturnsAsync(paged);

        var handler = new GetTeamsPagedQueryHandler(_repoMock.Object);
        var query = new GetTeamsPagedQuery
        {
            Parameters = new TeamParameters { PageNumber = 1, PageSize = 10 }
        };

        var result = await handler.Handle(query, _ct);

        result.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    // ── GetTeamsByMemberQueryHandler ──────────────────────────────────────────

    [Fact]
    public async Task GetTeamsByMember_ReturnsTeamsUserIsMemberOf()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByMemberIdAsync("owner-id", _ct))
                 .ReturnsAsync(new List<Domain.Entities.Team> { team });

        var handler = new GetTeamsByMemberQueryHandler(_repoMock.Object);
        var result = await handler.Handle(new GetTeamsByMemberQuery { UserId = "owner-id" }, _ct);

        result.Should().ContainSingle(t => t.Name == "Test Team");
    }

    [Fact]
    public async Task GetTeamsByMember_WhenNoTeams_ReturnsEmptyCollection()
    {
        _repoMock.Setup(r => r.GetByMemberIdAsync("user-no-teams", _ct))
                 .ReturnsAsync(new List<Domain.Entities.Team>());

        var handler = new GetTeamsByMemberQueryHandler(_repoMock.Object);
        var result = await handler.Handle(new GetTeamsByMemberQuery { UserId = "user-no-teams" }, _ct);

        result.Should().BeEmpty();
    }

    // ── GetTeamsByStatusQueryHandler ──────────────────────────────────────────

    [Fact]
    public async Task GetTeamsByStatus_ReturnsPagedListFilteredByStatus()
    {
        var team = CreateTeam();
        team.SetStatus(TeamStatus.Active, "owner-id");
        var paged = CreatePagedList(team);
        _repoMock.Setup(r => r.GetByStatusAsync(TeamStatus.Active, 1, 10, _ct))
                 .ReturnsAsync(paged);

        var handler = new GetTeamsByStatusQueryHandler(_repoMock.Object);
        var query = new GetTeamsByStatusQuery
        {
            Status = TeamStatus.Active,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await handler.Handle(query, _ct);

        result.Should().ContainSingle();
        result[0].Status.Should().Be(TeamStatus.Active);
    }

    // ── GetTeamsByCategoryQueryHandler ────────────────────────────────────────

    [Fact]
    public async Task GetTeamsByCategory_ReturnsPagedListFilteredByCategory()
    {
        var team = CreateTeam();
        var paged = CreatePagedList(team);
        _repoMock.Setup(r => r.GetByCategoryAsync("Design", 1, 10, _ct))
                 .ReturnsAsync(paged);

        var handler = new GetTeamsByCategoryQueryHandler(_repoMock.Object);
        var query = new GetTeamsByCategoryQuery
        {
            Category = "Design",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await handler.Handle(query, _ct);

        result.Should().ContainSingle(t => t.Category == "Design");
    }

    // ── SearchTeamsByNameQueryHandler ─────────────────────────────────────────

    [Fact]
    public async Task SearchTeamsByName_ReturnsMatchingTeams()
    {
        var team = CreateTeam("Alpha Squad");
        _repoMock.Setup(r => r.SearchByNameAsync("Alpha", _ct))
                 .ReturnsAsync(new List<Domain.Entities.Team> { team });

        var handler = new SearchTeamsByNameQueryHandler(_repoMock.Object);
        var result = await handler.Handle(new SearchTeamsByNameQuery { Name = "Alpha" }, _ct);

        result.Should().ContainSingle(t => t.Name == "Alpha Squad");
    }

    [Fact]
    public async Task SearchTeamsByName_WhenNoMatches_ReturnsEmptyCollection()
    {
        _repoMock.Setup(r => r.SearchByNameAsync("xyz", _ct))
                 .ReturnsAsync(new List<Domain.Entities.Team>());

        var handler = new SearchTeamsByNameQueryHandler(_repoMock.Object);
        var result = await handler.Handle(new SearchTeamsByNameQuery { Name = "xyz" }, _ct);

        result.Should().BeEmpty();
    }
}