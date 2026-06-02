using Collaboration.Application.Commands.GroupInvitation;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Commands.GroupInvitation;

public class GroupInvitationCommandHandlersTests
{
    private readonly Mock<IGroupInvitationRepository> _invitationRepoMock = new();
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static Domain.Entities.Team CreateTeam(string id = "team-1")
    {
        var owner = new UserSnapshot("owner-id", "owner");
        var team = new Domain.Entities.Team("Test Team", "desc", "Design", ["design"], owner);
        typeof(Domain.Entities.Team)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(team, id);
        return team;
    }

    private static Domain.Entities.GroupInvitation CreatePendingInvitation(
        string invitationId = "inv-1",
        string teamId = "team-1",
        string invitedUserId = "user-1",
        string invitedByUserId = "owner-id",
        string role = "Developer")
    {
        var inv = new Domain.Entities.GroupInvitation(
            teamId, invitedUserId, invitedByUserId, role, null, 7);
        typeof(Domain.Entities.GroupInvitation)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(inv, invitationId);
        return inv;
    }

    private static Domain.Entities.GroupInvitation CreateExpiredInvitation(
        string invitationId = "inv-1",
        string teamId = "team-1",
        string invitedUserId = "user-1")
    {
        var inv = CreatePendingInvitation(invitationId, teamId, invitedUserId);
        typeof(Domain.Entities.GroupInvitation)
            .GetProperty("ExpiresAt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(inv, DateTime.UtcNow.AddDays(-1));
        return inv;
    }

    // ── CreateGroupInvitationCommandHandler ───────────────────────────────────

    [Fact]
    public async Task CreateInvitation_WhenAllConditionsMet_CreatesAndReturnsInvitation()
    {
        var team = CreateTeam();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "user-1", _ct)).ReturnsAsync(false);
        _invitationRepoMock.Setup(r => r.HasPendingInvitationAsync("team-1", "user-1", _ct)).ReturnsAsync(false);
        _invitationRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Domain.Entities.GroupInvitation>(), _ct))
            .Returns<Domain.Entities.GroupInvitation, CancellationToken>((inv, _) => Task.FromResult(inv));

        var handler = new CreateGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1",
            InvitedUserId = "user-1",
            InvitedByUserId = "owner-id",
            Role = "Developer",
            Message = "Join us!",
            ExpirationDays = 7
        };

        var result = await handler.Handle(command, _ct);

        result.TeamId.Should().Be("team-1");
        result.InvitedUserId.Should().Be("user-1");
        result.Role.Should().Be("Developer");
        result.Status.Should().Be(Domain.Entities.InvitationStatus.Pending);
        _invitationRepoMock.Verify(r => r.CreateAsync(It.IsAny<Domain.Entities.GroupInvitation>(), _ct), Times.Once);
    }

    [Fact]
    public async Task CreateInvitation_WhenTeamNotFound_ThrowsDomainException()
    {
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct))
                     .ReturnsAsync((Domain.Entities.Team?)null);

        var handler = new CreateGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1", InvitedByUserId = "owner-id",
            Role = "Developer", ExpirationDays = 7
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateInvitation_WhenTeamIsDeleted_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.MarkAsDeleted();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        var handler = new CreateGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1", InvitedByUserId = "owner-id",
            Role = "Developer", ExpirationDays = 7
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*deleted*");
    }

    [Fact]
    public async Task CreateInvitation_WhenUserAlreadyMember_ThrowsDomainException()
    {
        var team = CreateTeam();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "user-1", _ct)).ReturnsAsync(true);

        var handler = new CreateGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1", InvitedByUserId = "owner-id",
            Role = "Developer", ExpirationDays = 7
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already a member*");
    }

    [Fact]
    public async Task CreateInvitation_WhenPendingInvitationExists_ThrowsDomainException()
    {
        var team = CreateTeam();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "user-1", _ct)).ReturnsAsync(false);
        _invitationRepoMock.Setup(r => r.HasPendingInvitationAsync("team-1", "user-1", _ct)).ReturnsAsync(true);

        var handler = new CreateGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "user-1", InvitedByUserId = "owner-id",
            Role = "Developer", ExpirationDays = 7
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*pending invitation*");
    }

    // ── AcceptGroupInvitationCommandHandler ───────────────────────────────────

    [Fact]
    public async Task AcceptInvitation_WhenValid_AcceptsAndAddsMemberToTeam()
    {
        var inv = CreatePendingInvitation(invitedUserId: "user-1");
        var team = CreateTeam();
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _invitationRepoMock.Setup(r => r.UpdateAsync(inv, _ct)).ReturnsAsync(inv);
        _teamRepoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);

        var handler = new AcceptGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1",
            UserId = "user-1",
            Username = "alice",
            AvatarUrl = "https://img/avatar.png"
        };

        var result = await handler.Handle(command, _ct);

        result.Status.Should().Be(Domain.Entities.InvitationStatus.Accepted);
        team.Members.Should().Contain(m => m.UserId == "user-1" && m.Role == "Developer");
        _invitationRepoMock.Verify(r => r.UpdateAsync(inv, _ct), Times.Once);
        _teamRepoMock.Verify(r => r.UpdateAsync(team, _ct), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitation_WhenInvitationNotFound_ThrowsDomainException()
    {
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct))
                           .ReturnsAsync((Domain.Entities.GroupInvitation?)null);

        var handler = new AcceptGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "user-1", Username = "alice"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task AcceptInvitation_WhenWrongUser_ThrowsDomainException()
    {
        var inv = CreatePendingInvitation(invitedUserId: "user-1");
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);

        var handler = new AcceptGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "other-user", Username = "bob"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*invited user*");
    }

    [Fact]
    public async Task AcceptInvitation_WhenTeamNotFound_ThrowsDomainException()
    {
        var inv = CreatePendingInvitation(invitedUserId: "user-1");
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct))
                     .ReturnsAsync((Domain.Entities.Team?)null);

        var handler = new AcceptGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "user-1", Username = "alice"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task AcceptInvitation_WhenAlreadyAccepted_ThrowsDomainException()
    {
        var inv = CreatePendingInvitation(invitedUserId: "user-1");
        inv.Accept("user-1");
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(CreateTeam());

        var handler = new AcceptGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "user-1", Username = "alice"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already*");
    }

    [Fact]
    public async Task AcceptInvitation_WhenExpired_ThrowsDomainException()
    {
        var inv = CreateExpiredInvitation(invitedUserId: "user-1");
        var team = CreateTeam();
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        var handler = new AcceptGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "user-1", Username = "alice"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*expired*");
    }

    // ── DeclineGroupInvitationCommandHandler ──────────────────────────────────

    [Fact]
    public async Task DeclineInvitation_WhenValid_DeclinesAndReturnsInvitation()
    {
        var inv = CreatePendingInvitation(invitedUserId: "user-1");
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);
        _invitationRepoMock.Setup(r => r.UpdateAsync(inv, _ct)).ReturnsAsync(inv);

        var handler = new DeclineGroupInvitationCommandHandler(_invitationRepoMock.Object);
        var command = new DeclineGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "user-1"
        };

        var result = await handler.Handle(command, _ct);

        result.Status.Should().Be(Domain.Entities.InvitationStatus.Declined);
        _invitationRepoMock.Verify(r => r.UpdateAsync(inv, _ct), Times.Once);
    }

    [Fact]
    public async Task DeclineInvitation_WhenInvitationNotFound_ThrowsDomainException()
    {
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct))
                           .ReturnsAsync((Domain.Entities.GroupInvitation?)null);

        var handler = new DeclineGroupInvitationCommandHandler(_invitationRepoMock.Object);
        var command = new DeclineGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeclineInvitation_WhenWrongUser_ThrowsDomainException()
    {
        var inv = CreatePendingInvitation(invitedUserId: "user-1");
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);

        var handler = new DeclineGroupInvitationCommandHandler(_invitationRepoMock.Object);
        var command = new DeclineGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "other-user"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*invited user*");
    }

    [Fact]
    public async Task DeclineInvitation_WhenAlreadyDeclined_ThrowsDomainException()
    {
        var inv = CreatePendingInvitation(invitedUserId: "user-1");
        inv.Decline("user-1");
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);

        var handler = new DeclineGroupInvitationCommandHandler(_invitationRepoMock.Object);
        var command = new DeclineGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already*");
    }

    [Fact]
    public async Task DeclineInvitation_WhenExpired_ThrowsDomainException()
    {
        var inv = CreateExpiredInvitation(invitedUserId: "user-1");
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);

        var handler = new DeclineGroupInvitationCommandHandler(_invitationRepoMock.Object);
        var command = new DeclineGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*expired*");
    }

    // ── RevokeGroupInvitationCommandHandler ───────────────────────────────────

    [Fact]
    public async Task RevokeInvitation_WhenTeamMemberRevokes_ReturnsUnitAndRevokesInvitation()
    {
        var inv = CreatePendingInvitation();
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "owner-id", _ct)).ReturnsAsync(true);
        _invitationRepoMock.Setup(r => r.UpdateAsync(inv, _ct)).ReturnsAsync(inv);

        var handler = new RevokeGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new RevokeGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "owner-id"
        };

        var result = await handler.Handle(command, _ct);

        result.Should().Be(Unit.Value);
        inv.Status.Should().Be(Domain.Entities.InvitationStatus.Revoked);
        _invitationRepoMock.Verify(r => r.UpdateAsync(inv, _ct), Times.Once);
    }

    [Fact]
    public async Task RevokeInvitation_WhenInvitationNotFound_ThrowsDomainException()
    {
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct))
                           .ReturnsAsync((Domain.Entities.GroupInvitation?)null);

        var handler = new RevokeGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new RevokeGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "owner-id"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task RevokeInvitation_WhenUserNotTeamMember_ThrowsDomainException()
    {
        var inv = CreatePendingInvitation();
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "outsider", _ct)).ReturnsAsync(false);

        var handler = new RevokeGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new RevokeGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "outsider"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*team member*");
    }

    [Fact]
    public async Task RevokeInvitation_WhenAlreadyRevoked_ThrowsDomainException()
    {
        var inv = CreatePendingInvitation();
        inv.Revoke("owner-id");
        _invitationRepoMock.Setup(r => r.GetByIdAsync("inv-1", _ct)).ReturnsAsync(inv);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "owner-id", _ct)).ReturnsAsync(true);

        var handler = new RevokeGroupInvitationCommandHandler(
            _invitationRepoMock.Object, _teamRepoMock.Object);
        var command = new RevokeGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "owner-id"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already*");
    }
}