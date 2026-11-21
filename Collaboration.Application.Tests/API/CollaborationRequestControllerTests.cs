using Collaboration.API.Controllers;
using Collaboration.Application.Commands.CollaborationRequest;
using Collaboration.Application.Queries.CollaborationRequest;
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

public class CollaborationRequestControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly CollaborationRequestController _controller;

    public CollaborationRequestControllerTests()
    {
        _controller = new CollaborationRequestController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static Domain.Entities.CollaborationRequest CreateRequest() =>
        new("team-1", "user-1", "Dev", "Hello", fromUsername: "alice");

    // ── GET api/collaboration-requests/{id} ───────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var request = CreateRequest();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestByIdQuery>(), default))
                     .ReturnsAsync(request);

        var result = await _controller.GetById("req-1", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(request);
    }

    [Fact]
    public async Task GetById_WhenNull_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestByIdQuery>(), default))
                     .ReturnsAsync((Domain.Entities.CollaborationRequest?)null);

        var result = await _controller.GetById("missing", default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestByIdQuery>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var result = await _controller.GetById("missing", default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestByIdQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetById("req-1", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GET api/collaboration-requests ────────────────────────────────────────

    [Fact]
    public async Task GetPaged_ReturnsOkWithPagedRequests()
    {
        var request = CreateRequest();
        var paged = new PagedList<Domain.Entities.CollaborationRequest>([request], 1, 1, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsPagedQuery>(), default))
                     .ReturnsAsync(paged);

        var result = await _controller.GetPaged(new CollaborationRequestParameters(), default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(paged);
    }

    // ── GET api/collaboration-requests/team/{teamId} ──────────────────────────

    [Fact]
    public async Task GetByTeam_ReturnsOkWithRequests()
    {
        var requests = new List<Domain.Entities.CollaborationRequest> { CreateRequest() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsByTeamQuery>(), default))
                     .ReturnsAsync(requests);

        var result = await _controller.GetByTeam("team-1", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(requests);
    }

    // ── GET api/collaboration-requests/mine ───────────────────────────────────

    [Fact]
    public async Task GetMine_ReturnsOkWithRequests()
    {
        var requests = new List<Domain.Entities.CollaborationRequest> { CreateRequest() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetMyCollaborationRequestsQuery>(), default))
                     .ReturnsAsync(requests);

        var result = await _controller.GetMine(default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(requests);
    }

    // ── POST api/collaboration-requests ──────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var request = CreateRequest();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateCollaborationRequestCommand>(), default))
                     .ReturnsAsync(request);

        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", Role = "Dev"
        };
        var result = await _controller.Create(command, default);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.Value.Should().Be(request);
    }

    [Fact]
    public async Task Create_WhenConflictException_ReturnsConflict()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateCollaborationRequestCommand>(), default))
                     .ThrowsAsync(new ConflictException("already exists"));

        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", Role = "Dev"
        };
        var result = await _controller.Create(command, default);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── PATCH api/collaboration-requests/{id}/accept ──────────────────────────

    [Fact]
    public async Task Accept_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new AcceptCollaborationRequestCommand { RequestId = "other-id" };

        var result = await _controller.Accept("req-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Accept_WhenSuccess_ReturnsOk()
    {
        var request = CreateRequest();
        _mediatorMock.Setup(m => m.Send(It.IsAny<AcceptCollaborationRequestCommand>(), default))
                     .ReturnsAsync(request);

        var command = new AcceptCollaborationRequestCommand { RequestId = "req-1" };
        var result = await _controller.Accept("req-1", command, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(request);
    }

    [Fact]
    public async Task Accept_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AcceptCollaborationRequestCommand>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var command = new AcceptCollaborationRequestCommand { RequestId = "req-1" };
        var result = await _controller.Accept("req-1", command, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── PATCH api/collaboration-requests/{id}/reject ──────────────────────────

    [Fact]
    public async Task Reject_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new RejectCollaborationRequestCommand { RequestId = "other-id" };

        var result = await _controller.Reject("req-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Reject_WhenSuccess_ReturnsOk()
    {
        var request = CreateRequest();
        _mediatorMock.Setup(m => m.Send(It.IsAny<RejectCollaborationRequestCommand>(), default))
                     .ReturnsAsync(request);

        var command = new RejectCollaborationRequestCommand { RequestId = "req-1" };
        var result = await _controller.Reject("req-1", command, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(request);
    }

    // ── PATCH api/collaboration-requests/{id}/cancel ──────────────────────────

    [Fact]
    public async Task Cancel_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new CancelCollaborationRequestCommand { RequestId = "other-id" };

        var result = await _controller.Cancel("req-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Cancel_WhenSuccess_ReturnsNoContent()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelCollaborationRequestCommand>(), default))
                     .ReturnsAsync(Unit.Value);

        var command = new CancelCollaborationRequestCommand { RequestId = "req-1" };
        var result = await _controller.Cancel("req-1", command, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Cancel_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelCollaborationRequestCommand>(), default))
                     .ThrowsAsync(new DomainException("already cancelled"));

        var command = new CancelCollaborationRequestCommand { RequestId = "req-1" };
        var result = await _controller.Cancel("req-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── catch block coverage for remaining methods ─────────────────────────────

    [Fact]
    public async Task GetPaged_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsPagedQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetPaged(new CollaborationRequestParameters(), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByTeam_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsByTeamQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetByTeam("team-1", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMine_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetMyCollaborationRequestsQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetMine(default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Reject_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<RejectCollaborationRequestCommand>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var command = new RejectCollaborationRequestCommand { RequestId = "req-1" };
        var result = await _controller.Reject("req-1", command, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Accept_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AcceptCollaborationRequestCommand>(), default))
                     .ThrowsAsync(new DomainException("already accepted"));

        var command = new AcceptCollaborationRequestCommand { RequestId = "req-1" };
        var result = await _controller.Accept("req-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Cancel_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelCollaborationRequestCommand>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var command = new CancelCollaborationRequestCommand { RequestId = "req-1" };
        var result = await _controller.Cancel("req-1", command, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateCollaborationRequestCommand>(), default))
                     .ThrowsAsync(new DomainException("invalid"));

        var command = new CreateCollaborationRequestCommand
        {
            TeamId = "team-1", Role = "Dev"
        };
        var result = await _controller.Create(command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}