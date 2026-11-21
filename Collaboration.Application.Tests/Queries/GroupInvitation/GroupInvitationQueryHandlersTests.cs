using Collaboration.Application.Interfaces.Services;
using Collaboration.Application.Queries.GroupInvitation;
using Collaboration.Domain.ValueOfObjects;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Entities.Parameters;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Queries.GroupInvitation;

public class GroupInvitationQueryHandlersTests
{
    private readonly Mock<IGroupInvitationRepository> _repoMock = new();
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly Mock<ICallerContext> _callerMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private void SetCaller(string userId)
    {
        _callerMock.Setup(c => c.UserId).Returns(userId);
    }

    private void SetupReviewableTeam(string teamId = "team-1", string callerId = "owner-id")
    {
        var team = new Domain.Entities.Team(
            "Test Team", "desc", "Design", ["design"], new UserSnapshot(callerId, "owner"));
        _teamRepoMock.Setup(r => r.GetByIdAsync(teamId, _ct)).ReturnsAsync(team);
        SetCaller(callerId);
    }

    private static Domain.Entities.GroupInvitation CreateInvitation(
        string teamId = "team-1",
        string invitedUserId = "user-1",
        string invitedByUserId = "owner-id",
        string role = "Developer")
    {
        return new Domain.Entities.GroupInvitation(teamId, invitedUserId, invitedByUserId, role, null, 7);
    }

    // ── GetGroupInvitationByIdQueryHandler ────────────────────────────────────

    [Fact]
    public async Task GetById_WhenInvitationExists_ReturnsInvitation()
    {
        var invitation = CreateInvitation();
        _repoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(invitation);
        SetCaller("user-1");

        var handler = new GetGroupInvitationByIdQueryHandler(_repoMock.Object, _teamRepoMock.Object, _callerMock.Object);
        var result = await handler.Handle(
            new GetGroupInvitationByIdQuery { InvitationId = "inv-1" }, _ct);

        result.Should().NotBeNull();
        result!.TeamId.Should().Be("team-1");
        result.InvitedUserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetById_WhenInvitationNotFound_ThrowsDomainException()
    {
        _repoMock.Setup(r => r.GetByIdAsync("missing", _ct))
                 .ReturnsAsync((Domain.Entities.GroupInvitation?)null);
        SetCaller("user-1");

        var handler = new GetGroupInvitationByIdQueryHandler(_repoMock.Object, _teamRepoMock.Object, _callerMock.Object);

        await handler.Invoking(h => h.Handle(
                         new GetGroupInvitationByIdQuery { InvitationId = "missing" }, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    // ── GetGroupInvitationsByTeamQueryHandler ─────────────────────────────────

    [Fact]
    public async Task GetByTeam_ReturnsInvitationsFromRepository()
    {
        var invitations = new[]
        {
            CreateInvitation(teamId: "team-1", invitedUserId: "user-1"),
            CreateInvitation(teamId: "team-1", invitedUserId: "user-2")
        };
        _repoMock.Setup(r => r.GetByTeamIdAsync("team-1", _ct))
                 .ReturnsAsync(invitations);
        SetupReviewableTeam();

        var handler = new GetGroupInvitationsByTeamQueryHandler(_repoMock.Object, _teamRepoMock.Object, _callerMock.Object);
        var result = await handler.Handle(
            new GetGroupInvitationsByTeamQuery { TeamId = "team-1" }, _ct);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.TeamId == "team-1");
    }

    [Fact]
    public async Task GetByTeam_WhenNoInvitations_ReturnsEmptyCollection()
    {
        _repoMock.Setup(r => r.GetByTeamIdAsync("team-1", _ct))
                 .ReturnsAsync([]);
        SetupReviewableTeam();

        var handler = new GetGroupInvitationsByTeamQueryHandler(_repoMock.Object, _teamRepoMock.Object, _callerMock.Object);
        var result = await handler.Handle(
            new GetGroupInvitationsByTeamQuery { TeamId = "team-1" }, _ct);

        result.Should().BeEmpty();
    }

    // ── GetMyGroupInvitationsQueryHandler ─────────────────────────────────────

    [Fact]
    public async Task GetMyGroupInvitations_ReturnsInvitationsForCaller()
    {
        var invitations = new[]
        {
            CreateInvitation(invitedUserId: "user-1", teamId: "team-1"),
            CreateInvitation(invitedUserId: "user-1", teamId: "team-2")
        };
        _repoMock.Setup(r => r.GetByInvitedUserIdAsync("user-1", _ct))
                 .ReturnsAsync(invitations);
        SetCaller("user-1");

        var handler = new GetMyGroupInvitationsQueryHandler(_repoMock.Object, _callerMock.Object);
        var result = await handler.Handle(new GetMyGroupInvitationsQuery(), _ct);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.InvitedUserId == "user-1");
    }

    [Fact]
    public async Task GetMyGroupInvitations_WhenNoInvitations_ReturnsEmptyCollection()
    {
        _repoMock.Setup(r => r.GetByInvitedUserIdAsync("user-1", _ct))
                 .ReturnsAsync([]);
        SetCaller("user-1");

        var handler = new GetMyGroupInvitationsQueryHandler(_repoMock.Object, _callerMock.Object);
        var result = await handler.Handle(new GetMyGroupInvitationsQuery(), _ct);

        result.Should().BeEmpty();
    }

    // ── GetExpiredGroupInvitationsQueryHandler ────────────────────────────────

    [Fact]
    public async Task GetExpired_ReturnsExpiredInvitationsFromRepository()
    {
        var expired = CreateInvitation();
        typeof(Domain.Entities.GroupInvitation)
            .GetProperty("ExpiresAt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(expired, DateTime.UtcNow.AddDays(-1));
        _repoMock.Setup(r => r.GetExpiredAsync(_ct))
                 .ReturnsAsync([expired]);

        var handler = new GetExpiredGroupInvitationsQueryHandler(_repoMock.Object);
        var result = await handler.Handle(new GetExpiredGroupInvitationsQuery(), _ct);

        result.Should().HaveCount(1);
        result.First().IsExpired().Should().BeTrue();
    }

    [Fact]
    public async Task GetExpired_WhenNoneExpired_ReturnsEmptyCollection()
    {
        _repoMock.Setup(r => r.GetExpiredAsync(_ct)).ReturnsAsync([]);

        var handler = new GetExpiredGroupInvitationsQueryHandler(_repoMock.Object);
        var result = await handler.Handle(new GetExpiredGroupInvitationsQuery(), _ct);

        result.Should().BeEmpty();
    }

    // ── GetGroupInvitationsPagedQueryHandler ──────────────────────────────────

    [Fact]
    public async Task GetPaged_ReturnsPagedListFromRepository()
    {
        var invitation = CreateInvitation();
        var paged = new PagedList<Domain.Entities.GroupInvitation>([invitation], 1, 1, 10);
        var parameters = new GroupInvitationParameters
        {
            TeamId = "team-1", PageNumber = 1, PageSize = 10
        };
        _repoMock.Setup(r => r.GetPagedFilteredAsync(
                     "team-1", null, null, null, null, null,
                     1, 10, false, _ct))
                 .ReturnsAsync(paged);
        SetupReviewableTeam();

        var handler = new GetGroupInvitationsPagedQueryHandler(_repoMock.Object, _teamRepoMock.Object, _callerMock.Object);
        var result = await handler.Handle(
            new GetGroupInvitationsPagedQuery { Parameters = parameters }, _ct);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }
}