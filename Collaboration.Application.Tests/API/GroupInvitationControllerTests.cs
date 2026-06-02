using Collaboration.API.Controllers;
using Collaboration.Application.Commands.GroupInvitation;
using Collaboration.Application.Queries.GroupInvitation;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities.Parameters;
using Collaboration.Domain.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.API;

public class GroupInvitationControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly GroupInvitationController _controller;

    public GroupInvitationControllerTests()
    {
        _controller = new GroupInvitationController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static Domain.Entities.GroupInvitation CreateInvitation() =>
        new("team-1", "invited-user", "owner-user", "Dev", "Please join");

    // ── GET api/group-invitations/{id} ────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var invitation = CreateInvitation();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationByIdQuery>(), default))
                     .ReturnsAsync(invitation);

        var result = await _controller.GetById("inv-1", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(invitation);
    }

    [Fact]
    public async Task GetById_WhenNull_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationByIdQuery>(), default))
                     .ReturnsAsync((Domain.Entities.GroupInvitation?)null);

        var result = await _controller.GetById("missing", default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationByIdQuery>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var result = await _controller.GetById("missing", default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationByIdQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetById("inv-1", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GET api/group-invitations ─────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_ReturnsOkWithInvitations()
    {
        var invitation = CreateInvitation();
        var paged = new PagedList<Domain.Entities.GroupInvitation>([invitation], 1, 1, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationsPagedQuery>(), default))
                     .ReturnsAsync(paged);

        var result = await _controller.GetPaged(new GroupInvitationParameters(), default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(paged);
    }

    // ── GET api/group-invitations/team/{teamId} ───────────────────────────────

    [Fact]
    public async Task GetByTeam_ReturnsOkWithInvitations()
    {
        var invitations = new List<Domain.Entities.GroupInvitation> { CreateInvitation() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationsByTeamQuery>(), default))
                     .ReturnsAsync(invitations);

        var result = await _controller.GetByTeam("team-1", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(invitations);
    }

    // ── GET api/group-invitations/user/{userId} ───────────────────────────────

    [Fact]
    public async Task GetByUser_ReturnsOkWithInvitations()
    {
        var invitations = new List<Domain.Entities.GroupInvitation> { CreateInvitation() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationsByUserQuery>(), default))
                     .ReturnsAsync(invitations);

        var result = await _controller.GetByUser("invited-user", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(invitations);
    }

    // ── GET api/group-invitations/expired ─────────────────────────────────────

    [Fact]
    public async Task GetExpired_ReturnsOkWithInvitations()
    {
        var invitations = new List<Domain.Entities.GroupInvitation> { CreateInvitation() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetExpiredGroupInvitationsQuery>(), default))
                     .ReturnsAsync(invitations);

        var result = await _controller.GetExpired(default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(invitations);
    }

    // ── POST api/group-invitations ────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var invitation = CreateInvitation();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateGroupInvitationCommand>(), default))
                     .ReturnsAsync(invitation);

        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "u2", InvitedByUserId = "u1", Role = "Dev"
        };
        var result = await _controller.Create(command, default);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.Value.Should().Be(invitation);
    }

    [Fact]
    public async Task Create_WhenConflictException_ReturnsConflict()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateGroupInvitationCommand>(), default))
                     .ThrowsAsync(new ConflictException("already invited"));

        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "u2", InvitedByUserId = "u1", Role = "Dev"
        };
        var result = await _controller.Create(command, default);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── PATCH api/group-invitations/{id}/accept ───────────────────────────────

    [Fact]
    public async Task Accept_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "other-id", UserId = "u1", Username = "alice"
        };

        var result = await _controller.Accept("inv-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Accept_WhenSuccess_ReturnsOk()
    {
        var invitation = CreateInvitation();
        _mediatorMock.Setup(m => m.Send(It.IsAny<AcceptGroupInvitationCommand>(), default))
                     .ReturnsAsync(invitation);

        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "u1", Username = "alice"
        };
        var result = await _controller.Accept("inv-1", command, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(invitation);
    }

    [Fact]
    public async Task Accept_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AcceptGroupInvitationCommand>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "u1", Username = "alice"
        };
        var result = await _controller.Accept("inv-1", command, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── PATCH api/group-invitations/{id}/decline ──────────────────────────────

    [Fact]
    public async Task Decline_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new DeclineGroupInvitationCommand { InvitationId = "other-id", UserId = "u1" };

        var result = await _controller.Decline("inv-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Decline_WhenSuccess_ReturnsOk()
    {
        var invitation = CreateInvitation();
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeclineGroupInvitationCommand>(), default))
                     .ReturnsAsync(invitation);

        var command = new DeclineGroupInvitationCommand { InvitationId = "inv-1", UserId = "u1" };
        var result = await _controller.Decline("inv-1", command, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(invitation);
    }

    [Fact]
    public async Task Decline_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeclineGroupInvitationCommand>(), default))
                     .ThrowsAsync(new DomainException("already declined"));

        var command = new DeclineGroupInvitationCommand { InvitationId = "inv-1", UserId = "u1" };
        var result = await _controller.Decline("inv-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── PATCH api/group-invitations/{id}/revoke ───────────────────────────────

    [Fact]
    public async Task Revoke_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new RevokeGroupInvitationCommand { InvitationId = "other-id", UserId = "u1" };

        var result = await _controller.Revoke("inv-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Revoke_WhenSuccess_ReturnsNoContent()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<RevokeGroupInvitationCommand>(), default))
                     .ReturnsAsync(Unit.Value);

        var command = new RevokeGroupInvitationCommand { InvitationId = "inv-1", UserId = "u1" };
        var result = await _controller.Revoke("inv-1", command, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Revoke_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<RevokeGroupInvitationCommand>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var command = new RevokeGroupInvitationCommand { InvitationId = "inv-1", UserId = "u1" };
        var result = await _controller.Revoke("inv-1", command, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── catch block coverage ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationsPagedQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetPaged(new GroupInvitationParameters(), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByTeam_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationsByTeamQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetByTeam("team-1", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByUser_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupInvitationsByUserQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetByUser("user-1", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetExpired_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetExpiredGroupInvitationsQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetExpired(default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Accept_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AcceptGroupInvitationCommand>(), default))
                     .ThrowsAsync(new DomainException("already accepted"));

        var command = new AcceptGroupInvitationCommand
        {
            InvitationId = "inv-1", UserId = "u1", Username = "alice"
        };
        var result = await _controller.Accept("inv-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Decline_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeclineGroupInvitationCommand>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var command = new DeclineGroupInvitationCommand { InvitationId = "inv-1", UserId = "u1" };
        var result = await _controller.Decline("inv-1", command, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Revoke_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<RevokeGroupInvitationCommand>(), default))
                     .ThrowsAsync(new DomainException("already revoked"));

        var command = new RevokeGroupInvitationCommand { InvitationId = "inv-1", UserId = "u1" };
        var result = await _controller.Revoke("inv-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateGroupInvitationCommand>(), default))
                     .ThrowsAsync(new DomainException("team deleted"));

        var command = new CreateGroupInvitationCommand
        {
            TeamId = "team-1", InvitedUserId = "u2", InvitedByUserId = "u1", Role = "Dev"
        };
        var result = await _controller.Create(command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}