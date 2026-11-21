using Collaboration.Application.Commands.Team;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Commands.Team;

public class TeamCommandHandlersTests
{
    private readonly Mock<ITeamRepository> _repoMock = new();
    private readonly Mock<IUserSnapshotService> _usersMock = new();
    private readonly Mock<ICallerContext> _callerMock = new();
    private readonly Mock<ITeamVacancyRepository> _vacancyRepoMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private void SetCaller(string userId, string? username = null)
    {
        _callerMock.Setup(c => c.UserId).Returns(userId);
        _callerMock.Setup(c => c.Username).Returns(username ?? userId);
        _callerMock.Setup(c => c.ToSnapshot())
                   .Returns(() => new UserSnapshot(userId, username ?? userId));
    }

    private static Domain.Entities.Team CreateTeam(string name = "Test Team")
    {
        var owner = new UserSnapshot("owner-id", "owner-username");
        return new Domain.Entities.Team(name, "A valid description", "Design", ["design"], owner);
    }

    // ── CreateTeamCommandHandler ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTeam_WhenNameIsUnique_CreatesAndReturnsTeam()
    {
        _repoMock.Setup(r => r.ExistsByNameAsync("New Team", null, _ct)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Domain.Entities.Team>(), _ct))
                 .ReturnsAsync((Domain.Entities.Team t, CancellationToken _) => t);

        SetCaller("user-1", "alice");
        var handler = new CreateTeamCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new CreateTeamCommand
        {
            Name = "New Team",
            Description = "Some description",
            Category = "Design",
            Tags = ["design"]
        };
    
        var result = await handler.Handle(command, _ct);

        result.Name.Should().Be("New Team");
        result.Members.Should().ContainSingle(m => m.UserId == "user-1" && m.Role == "Owner");
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Domain.Entities.Team>(), _ct), Times.Once);
    }

    [Fact]
    public async Task CreateTeam_WhenNameAlreadyExists_ThrowsDomainException()
    {
        _repoMock.Setup(r => r.ExistsByNameAsync("Duplicate", null, _ct)).ReturnsAsync(true);

        SetCaller("user-1", "alice");
        var handler = new CreateTeamCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new CreateTeamCommand
        {
            Name = "Duplicate",
            Description = "desc",
            Category = "Design"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already exists*");
    }

    // ── UpdateTeamCommandHandler ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateTeam_WhenTeamExistsAndNameIsUnique_UpdatesAndReturnsTeam()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.ExistsByNameAsync("Updated Name", "team-1", _ct)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new UpdateTeamCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new UpdateTeamCommand
        {
            TeamId = "team-1",
            Name = "Updated Name",
            Description = "New desc",
            Category = "Art",
            Tags = ["art"]
        };

        var result = await handler.Handle(command, _ct);

        result.Name.Should().Be("Updated Name");
        result.Category.Should().Be("Art");
        _repoMock.Verify(r => r.UpdateAsync(team, _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateTeam_WhenTeamNotFound_ThrowsDomainException()
    {
        _repoMock.Setup(r => r.GetByIdAsync("missing", _ct)).ReturnsAsync((Domain.Entities.Team?)null);

        SetCaller("owner-id", "owner-username");
        var handler = new UpdateTeamCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new UpdateTeamCommand
        {
            TeamId = "missing",
            Name = "X",
            Description = "desc",
            Category = "Design"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateTeam_WhenTeamIsDeleted_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.MarkAsDeleted();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new UpdateTeamCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new UpdateTeamCommand
        {
            TeamId = "team-1",
            Name = "X",
            Description = "desc",
            Category = "Design"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*deleted*");
    }

    // ── DeleteTeamCommandHandler ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteTeam_WhenTeamExists_MarksAsDeletedAndReturnsUnit()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new DeleteTeamCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new DeleteTeamCommand { TeamId = "team-1" };

        var result = await handler.Handle(command, _ct);

        result.Should().Be(Unit.Value);
        team.IsDeleted.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(team, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteTeam_WhenTeamNotFound_ThrowsDomainException()
    {
        _repoMock.Setup(r => r.GetByIdAsync("missing", _ct)).ReturnsAsync((Domain.Entities.Team?)null);

        SetCaller("owner-id", "owner-username");
        var handler = new DeleteTeamCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new DeleteTeamCommand { TeamId = "missing" };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>();
    }

    // ── SetTeamStatusCommandHandler ───────────────────────────────────────────

    [Fact]
    public async Task SetTeamStatus_WhenTeamExists_UpdatesStatusAndReturnsTeam()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new SetTeamStatusCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new SetTeamStatusCommand
        {
            TeamId = "team-1",
            Status = TeamStatus.Active
        };

        var result = await handler.Handle(command, _ct);

        result.Status.Should().Be(TeamStatus.Active);
    }

    [Fact]
    public async Task SetTeamStatus_WhenTeamIsDeleted_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.MarkAsDeleted();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new SetTeamStatusCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new SetTeamStatusCommand
        {
            TeamId = "team-1",
            Status = TeamStatus.Active
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*deleted*");
    }

    // ── AddTeamMemberCommandHandler ───────────────────────────────────────────

    [Fact]
    public async Task AddTeamMember_WhenUserNotAlreadyMember_AddsMemberAndReturnsTeam()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new AddTeamMemberCommandHandler(_repoMock.Object, _usersMock.Object, _callerMock.Object);
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1",
            MemberUserId = "new-user",
            MemberUsername = "bob",
            Role = TeamRole.Member
        };

        var result = await handler.Handle(command, _ct);

        result.Members.Should().Contain(m => m.UserId == "new-user" && m.Role == TeamRole.Member);
    }

    [Fact]
    public async Task AddTeamMember_WhenUserAlreadyMember_ThrowsDomainException()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new AddTeamMemberCommandHandler(_repoMock.Object, _usersMock.Object, _callerMock.Object);
        var command = new AddTeamMemberCommand
        {
            TeamId = "team-1",
            MemberUserId = "owner-id",  // owner is already a member
            MemberUsername = "owner-username",
            Role = "Developer"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already a member*");
    }

    // ── RemoveTeamMemberCommandHandler ────────────────────────────────────────

    [Fact]
    public async Task RemoveTeamMember_WhenMemberExists_RemovesMemberAndReturnsTeam()
    {
        var team = CreateTeam();
        var member = new UserSnapshot("member-id", "member-username");
        team.Join(member, "Developer");
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new RemoveTeamMemberCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new RemoveTeamMemberCommand
        {
            TeamId = "team-1",
            MemberUserId = "member-id"
        };

        var result = await handler.Handle(command, _ct);

        result.Members.Should().NotContain(m => m.UserId == "member-id");
    }

    [Fact]
    public async Task RemoveTeamMember_WhenMemberNotFound_ThrowsDomainException()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new RemoveTeamMemberCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new RemoveTeamMemberCommand
        {
            TeamId = "team-1",
            MemberUserId = "nonexistent-user"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    // ── AddRequiredRoleCommandHandler ─────────────────────────────────────────

    [Fact]
    public async Task AddRequiredRole_WhenTeamExists_AddsRoleAndReturnsTeam()
    {
        var team = CreateTeam();
        _vacancyRepoMock.Setup(r => r.GetOpenByTeamIdsAsync(It.IsAny<IEnumerable<string>>(), _ct)).ReturnsAsync(new List<TeamVacancy>());
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new AddRequiredRoleCommandHandler(_repoMock.Object, _vacancyRepoMock.Object, _callerMock.Object);
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1",
            Role = "Backend Developer",
            Description = "Needs .NET experience"
        };

        var result = await handler.Handle(command, _ct);

        result.RequiredRoles.Should().Contain(r => r.Role == "Backend Developer");
    }

    [Fact]
    public async Task AddRequiredRole_WhenRoleIsEmpty_ThrowsDomainException()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new AddRequiredRoleCommandHandler(_repoMock.Object, _vacancyRepoMock.Object, _callerMock.Object);
        var command = new AddRequiredRoleCommand
        {
            TeamId = "team-1",
            Role = ""
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*cannot be empty*");
    }

    [Fact]
    public async Task AddRequiredRole_OpensVacancyForTheRole()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);
        _vacancyRepoMock.Setup(r => r.GetOpenByTeamIdsAsync(It.IsAny<IEnumerable<string>>(), _ct)).ReturnsAsync(new List<TeamVacancy>());
        _vacancyRepoMock.Setup(r => r.CreateAsync(It.IsAny<TeamVacancy>(), _ct))
                        .ReturnsAsync((TeamVacancy v, CancellationToken _) => v);

        SetCaller("owner-id", "owner-username");
        var handler = new AddRequiredRoleCommandHandler(_repoMock.Object, _vacancyRepoMock.Object, _callerMock.Object);

        var result = await handler.Handle(
            new AddRequiredRoleCommand { TeamId = "team-1", Role = "Веб-розробник", Description = "React" }, _ct);

        _vacancyRepoMock.Verify(r => r.CreateAsync(
            It.Is<TeamVacancy>(v => v.TeamId == team.Id && v.Role == "Веб-розробник" && v.Description == "React"
                                    && v.Status == TeamVacancyStatus.Open), _ct), Times.Once);
        result.OpenVacancyCount.Should().Be(1);
    }

    [Fact]
    public async Task AddRequiredRole_WhenOpenVacancyForRoleExists_DoesNotDuplicateIt()
    {
        var team = CreateTeam();
        var existing = new TeamVacancy(team.Id, team.Name, team.Category, team.Tags, "веб-розробник", "owner-id");
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);
        _vacancyRepoMock.Setup(r => r.GetOpenByTeamIdsAsync(It.IsAny<IEnumerable<string>>(), _ct)).ReturnsAsync(new List<TeamVacancy> { existing });

        SetCaller("owner-id", "owner-username");
        var handler = new AddRequiredRoleCommandHandler(_repoMock.Object, _vacancyRepoMock.Object, _callerMock.Object);

        await handler.Handle(new AddRequiredRoleCommand { TeamId = "team-1", Role = "Веб-розробник" }, _ct);

        _vacancyRepoMock.Verify(r => r.CreateAsync(It.IsAny<TeamVacancy>(), _ct), Times.Never);
    }

    // ── SetTeamAvatarUrlCommandHandler ────────────────────────────────────────

    [Fact]
    public async Task SetTeamAvatarUrl_WhenTeamExists_SetsAvatarAndReturnsTeam()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new SetTeamAvatarUrlCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new SetTeamAvatarUrlCommand
        {
            TeamId = "team-1",
            AvatarUrl = "https://example.com/avatar.png"
        };

        var result = await handler.Handle(command, _ct);

        result.AvatarUrl.Should().Be("https://example.com/avatar.png");
        _repoMock.Verify(r => r.UpdateAsync(team, _ct), Times.Once);
    }

    [Fact]
    public async Task SetTeamAvatarUrl_WhenTeamIsDeleted_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.MarkAsDeleted();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new SetTeamAvatarUrlCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new SetTeamAvatarUrlCommand
        {
            TeamId = "team-1",
            AvatarUrl = "https://example.com/avatar.png"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*deleted*");
    }

    // ── UpdateTeamMemberPermissionsCommandHandler ─────────────────────────────

    [Fact]
    public async Task SetPermissions_WhenOwner_SetsPermissionsAndReturnsTeam()
    {
        var team = CreateTeam();
        team.Join(new UserSnapshot("member-id", "member-username"), "Developer");
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new UpdateTeamMemberPermissionsCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new UpdateTeamMemberPermissionsCommand
        {
            TeamId = "team-1",
            TargetMemberId = "member-id",
            Permissions = [TeamPermission.EditPosts, TeamPermission.InviteMembers]
        };

        var result = await handler.Handle(command, _ct);

        var member = result.Members.Single(m => m.UserId == "member-id");
        member.Permissions.Should().Contain(TeamPermission.EditPosts);
        member.Permissions.Should().Contain(TeamPermission.InviteMembers);
        _repoMock.Verify(r => r.UpdateAsync(team, _ct), Times.Once);
    }

    [Fact]
    public async Task SetPermissions_WhenNotOwner_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.Join(new UserSnapshot("member-id", "member-username"), "Developer");
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        SetCaller("member-id", "member-username");
        var handler = new UpdateTeamMemberPermissionsCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new UpdateTeamMemberPermissionsCommand
        {
            TeamId = "team-1",
            TargetMemberId = "member-id",
            Permissions = [TeamPermission.EditPosts]
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*team leader*");
    }

    [Fact]
    public async Task SetPermissions_WhenTargetIsOwner_ThrowsDomainException()
    {
        var team = CreateTeam();
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        SetCaller("owner-id", "owner-username");
        var handler = new UpdateTeamMemberPermissionsCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new UpdateTeamMemberPermissionsCommand
        {
            TeamId = "team-1",
            TargetMemberId = "owner-id",
            Permissions = [TeamPermission.EditPosts]
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*team leader*");
    }

    [Fact]
    public async Task UpdateTeam_WhenMemberHasNoEditTeamPermission_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.Join(new UserSnapshot("member-id", "member-username"), "Developer");
        _repoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _repoMock.Setup(r => r.ExistsByNameAsync("Updated Name", "team-1", _ct)).ReturnsAsync(false);

        SetCaller("member-id", "member-username");
        var handler = new UpdateTeamCommandHandler(_repoMock.Object, _callerMock.Object);
        var command = new UpdateTeamCommand
        {
            TeamId = "team-1",
            Name = "Updated Name",
            Description = "New desc",
            Category = "Art",
            Tags = ["art"]
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*required permission*");
    }
}