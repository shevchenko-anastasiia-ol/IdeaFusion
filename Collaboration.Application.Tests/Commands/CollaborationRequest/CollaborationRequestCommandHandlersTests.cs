using Collaboration.Application.Commands.CollaborationRequest;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Commands.CollaborationRequest;

public class CollaborationRequestCommandHandlersTests
{
    private readonly Mock<ICollaborationRequestRepository> _requestRepoMock = new();
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly Mock<IUserSnapshotService> _snapshotServiceMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static Domain.Entities.Team CreateTeam(string id = "team-1")
    {
        var owner = new UserSnapshot("owner-id", "owner");
        var team = new Domain.Entities.Team("Test Team", "desc", "Design", ["design"], owner);
        typeof(Domain.Entities.Team).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(team, id);
        return team;
    }

    private static Domain.Entities.CollaborationRequest CreatePendingRequest(
        string requestId = "req-1",
        string teamId = "team-1",
        string fromUserId = "user-1",
        string role = "Developer",
        string? fromUsername = "alice",
        string? fromAvatarUrl = null)
    {
        var req = new Domain.Entities.CollaborationRequest(
            teamId, fromUserId, role, null, null, fromUsername, fromAvatarUrl);
        typeof(Domain.Entities.CollaborationRequest).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(req, requestId);
        return req;
    }

    // ── CreateCollaborationRequestCommandHandler ──────────────────────────────

    [Fact]
    public async Task CreateRequest_WhenAllConditionsMet_CreatesAndReturnsRequest()
    {
        var team = CreateTeam();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "user-1", _ct)).ReturnsAsync(false);
        _requestRepoMock.Setup(r => r.HasPendingRequestAsync("team-1", "user-1", "Developer", _ct))
                        .ReturnsAsync(false);
        _requestRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Domain.Entities.CollaborationRequest>(), _ct))
            .Returns<Domain.Entities.CollaborationRequest, CancellationToken>((req, _) => Task.FromResult(req));

        var handler = new CreateCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1",
            FromUserId = "user-1",
            FromUsername = "alice",
            Role = "Developer",
            Message = "Hi!"
        };

        var result = await handler.Handle(command, _ct);

        result.TeamId.Should().Be("team-1");
        result.FromUserId.Should().Be("user-1");
        result.Role.Should().Be("Developer");
        result.Status.Should().Be(Domain.Entities.CollaborationRequestStatus.Pending);
        _requestRepoMock.Verify(r => r.CreateAsync(It.IsAny<Domain.Entities.CollaborationRequest>(), _ct), Times.Once);
    }

    [Fact]
    public async Task CreateRequest_WhenTeamNotFound_ThrowsDomainException()
    {
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct))
                     .ReturnsAsync((Domain.Entities.Team?)null);

        var handler = new CreateCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", FromUserId = "user-1", Role = "Developer"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateRequest_WhenTeamIsDeleted_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.MarkAsDeleted();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        var handler = new CreateCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", FromUserId = "user-1", Role = "Developer"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*deleted*");
    }

    [Fact]
    public async Task CreateRequest_WhenUserAlreadyMember_ThrowsDomainException()
    {
        var team = CreateTeam();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "user-1", _ct)).ReturnsAsync(true);

        var handler = new CreateCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", FromUserId = "user-1", Role = "Developer"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already a member*");
    }

    [Fact]
    public async Task CreateRequest_WhenPendingRequestExists_ThrowsDomainException()
    {
        var team = CreateTeam();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "user-1", _ct)).ReturnsAsync(false);
        _requestRepoMock.Setup(r => r.HasPendingRequestAsync("team-1", "user-1", "Developer", _ct))
                        .ReturnsAsync(true);

        var handler = new CreateCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", FromUserId = "user-1", Role = "Developer"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*pending request*");
    }

    // ── AcceptCollaborationRequestCommandHandler ──────────────────────────────

    [Fact]
    public async Task AcceptRequest_WithKnownUsername_AcceptsAndAddsMemberWithoutCallingSnapshotService()
    {
        var colReq = CreatePendingRequest(fromUsername: "alice");
        var team = CreateTeam();
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);
        _requestRepoMock.Setup(r => r.UpdateAsync(colReq, _ct)).ReturnsAsync(colReq);

        var handler = new AcceptCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object, _snapshotServiceMock.Object);
        var command = new AcceptCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "owner-id"
        };

        var result = await handler.Handle(command, _ct);

        result.Status.Should().Be(Domain.Entities.CollaborationRequestStatus.Accepted);
        team.Members.Should().Contain(m => m.UserId == "user-1");
        _snapshotServiceMock.Verify(s => s.GetUserSnapshotDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _teamRepoMock.Verify(r => r.UpdateAsync(team, _ct), Times.Once);
        _requestRepoMock.Verify(r => r.UpdateAsync(colReq, _ct), Times.Once);
    }

    [Fact]
    public async Task AcceptRequest_WithoutUsername_FetchesSnapshotAndAddsMember()
    {
        var colReq = CreatePendingRequest(fromUsername: null);
        var team = CreateTeam();
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);
        _requestRepoMock.Setup(r => r.UpdateAsync(colReq, _ct)).ReturnsAsync(colReq);
        _snapshotServiceMock.Setup(s => s.GetUserSnapshotDataAsync("user-1", _ct))
                            .ReturnsAsync(("alice", "https://img/avatar.png"));

        var handler = new AcceptCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object, _snapshotServiceMock.Object);
        var command = new AcceptCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "owner-id"
        };

        var result = await handler.Handle(command, _ct);

        result.Status.Should().Be(Domain.Entities.CollaborationRequestStatus.Accepted);
        team.Members.Should().Contain(m => m.UserId == "user-1");
        _snapshotServiceMock.Verify(s => s.GetUserSnapshotDataAsync("user-1", _ct), Times.Once);
    }

    [Fact]
    public async Task AcceptRequest_WhenUserAlreadyMember_DoesNotAddDuplicateMember()
    {
        var colReq = CreatePendingRequest(fromUsername: "alice");
        var team = CreateTeam();
        team.AddMember(new UserSnapshot("user-1", "alice"), "Developer");
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.UpdateAsync(team, _ct)).ReturnsAsync(team);
        _requestRepoMock.Setup(r => r.UpdateAsync(colReq, _ct)).ReturnsAsync(colReq);

        var handler = new AcceptCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object, _snapshotServiceMock.Object);
        var command = new AcceptCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "owner-id"
        };

        await handler.Handle(command, _ct);

        team.Members.Count(m => m.UserId == "user-1").Should().Be(1);
    }

    [Fact]
    public async Task AcceptRequest_WhenRequestNotFound_ThrowsDomainException()
    {
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct))
                        .ReturnsAsync((Domain.Entities.CollaborationRequest?)null);

        var handler = new AcceptCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object, _snapshotServiceMock.Object);
        var command = new AcceptCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "owner-id"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task AcceptRequest_WhenTeamNotFound_ThrowsDomainException()
    {
        var colReq = CreatePendingRequest();
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct))
                     .ReturnsAsync((Domain.Entities.Team?)null);

        var handler = new AcceptCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object, _snapshotServiceMock.Object);
        var command = new AcceptCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "owner-id"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task AcceptRequest_WhenRequestAlreadyAccepted_ThrowsDomainException()
    {
        var colReq = CreatePendingRequest();
        colReq.Accept("owner-id");
        var team = CreateTeam();
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        var handler = new AcceptCollaborationRequestCommandHandler(
            _requestRepoMock.Object, _teamRepoMock.Object, _snapshotServiceMock.Object);
        var command = new AcceptCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "owner-id"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already*");
    }

    // ── CancelCollaborationRequestCommandHandler ──────────────────────────────

    [Fact]
    public async Task CancelRequest_WhenAuthorCancels_ReturnsUnitAndCancelsRequest()
    {
        var colReq = CreatePendingRequest(fromUserId: "user-1");
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);
        _requestRepoMock.Setup(r => r.UpdateAsync(colReq, _ct)).ReturnsAsync(colReq);

        var handler = new CancelCollaborationRequestCommandHandler(_requestRepoMock.Object);
        var command = new CancelCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "user-1"
        };

        var result = await handler.Handle(command, _ct);

        result.Should().Be(Unit.Value);
        colReq.Status.Should().Be(Domain.Entities.CollaborationRequestStatus.Cancelled);
        _requestRepoMock.Verify(r => r.UpdateAsync(colReq, _ct), Times.Once);
    }

    [Fact]
    public async Task CancelRequest_WhenNotAuthor_ThrowsDomainException()
    {
        var colReq = CreatePendingRequest(fromUserId: "user-1");
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);

        var handler = new CancelCollaborationRequestCommandHandler(_requestRepoMock.Object);
        var command = new CancelCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "other-user"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*author*");
    }

    [Fact]
    public async Task CancelRequest_WhenRequestNotFound_ThrowsDomainException()
    {
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct))
                        .ReturnsAsync((Domain.Entities.CollaborationRequest?)null);

        var handler = new CancelCollaborationRequestCommandHandler(_requestRepoMock.Object);
        var command = new CancelCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task CancelRequest_WhenAlreadyCancelled_ThrowsDomainException()
    {
        var colReq = CreatePendingRequest(fromUserId: "user-1");
        colReq.Cancel("user-1");
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);

        var handler = new CancelCollaborationRequestCommandHandler(_requestRepoMock.Object);
        var command = new CancelCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already*");
    }

    // ── RejectCollaborationRequestCommandHandler ──────────────────────────────

    [Fact]
    public async Task RejectRequest_WhenRequestPending_RejectsAndReturnsRequest()
    {
        var colReq = CreatePendingRequest();
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);
        _requestRepoMock.Setup(r => r.UpdateAsync(colReq, _ct)).ReturnsAsync(colReq);

        var handler = new RejectCollaborationRequestCommandHandler(_requestRepoMock.Object);
        var command = new RejectCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "owner-id"
        };

        var result = await handler.Handle(command, _ct);

        result.Status.Should().Be(Domain.Entities.CollaborationRequestStatus.Rejected);
        _requestRepoMock.Verify(r => r.UpdateAsync(colReq, _ct), Times.Once);
    }

    [Fact]
    public async Task RejectRequest_WhenRequestNotFound_ThrowsDomainException()
    {
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct))
                        .ReturnsAsync((Domain.Entities.CollaborationRequest?)null);

        var handler = new RejectCollaborationRequestCommandHandler(_requestRepoMock.Object);
        var command = new RejectCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "owner-id"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task RejectRequest_WhenAlreadyRejected_ThrowsDomainException()
    {
        var colReq = CreatePendingRequest();
        colReq.Reject("owner-id");
        _requestRepoMock.Setup(r => r.GetByIdAsync("req-1", _ct)).ReturnsAsync(colReq);

        var handler = new RejectCollaborationRequestCommandHandler(_requestRepoMock.Object);
        var command = new RejectCollaborationRequestCommand
        {
            RequestId = "req-1", UserId = "owner-id"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already*");
    }
}