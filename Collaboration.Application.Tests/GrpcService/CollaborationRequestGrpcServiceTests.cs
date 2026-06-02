using AutoMapper;
using Collaboration.Application.Queries.CollaborationRequest;
using Collaboration.Domain.Entities;
using CollaborationGrpcService.Services;
using FluentAssertions;
using Grpc.Core;
using IdeaFusion.Grpc.CollaborationRequests;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.GrpcService;

public class CollaborationRequestGrpcServiceTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly CollaborationRequestGrpcServiceImpl _service;
    private readonly ServerCallContext _fakeContext = new FakeServerCallContext();

    public CollaborationRequestGrpcServiceTests()
    {
        _service = new CollaborationRequestGrpcServiceImpl(
            _mediatorMock.Object,
            _mapperMock.Object,
            NullLogger<CollaborationRequestGrpcServiceImpl>.Instance);
    }

    private static CollaborationRequest CreateCollabRequest() =>
        new("team-1", "user-1", "Developer", "Hello!");

    // ── GetRequestById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRequestById_WithEmptyRequestId_ThrowsRpcInvalidArgument()
    {
        var request = new GetRequestByIdRequest { RequestId = "" };

        var act = async () => await _service.GetRequestById(request, _fakeContext);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetRequestById_WhenFound_ReturnsResponse()
    {
        var collab = CreateCollabRequest();
        var dto = new CollaborationRequestDto { RequestId = collab.Id };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestByIdQuery>(), default))
                     .ReturnsAsync(collab);
        _mapperMock.Setup(m => m.Map<CollaborationRequestDto>(collab)).Returns(dto);

        var result = await _service.GetRequestById(new GetRequestByIdRequest { RequestId = "req-1" }, _fakeContext);

        result.Request.Should().Be(dto);
    }

    [Fact]
    public async Task GetRequestById_WhenNotFound_ThrowsRpcNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestByIdQuery>(), default))
                     .ReturnsAsync((CollaborationRequest?)null);

        var act = async () => await _service.GetRequestById(
            new GetRequestByIdRequest { RequestId = "missing" }, _fakeContext);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetRequestById_WhenMediatorThrows_ThrowsRpcInternal()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestByIdQuery>(), default))
                     .ThrowsAsync(new Exception("db error"));

        var act = async () => await _service.GetRequestById(
            new GetRequestByIdRequest { RequestId = "req-1" }, _fakeContext);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Internal);
    }

    // ── GetRequestsByTeam ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRequestsByTeam_WithEmptyTeamId_ThrowsRpcInvalidArgument()
    {
        var act = async () => await _service.GetRequestsByTeam(
            new GetRequestsByTeamRequest { TeamId = "" }, _fakeContext);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetRequestsByTeam_WithNoStatusFilter_ReturnsAllItems()
    {
        var requests = new List<CollaborationRequest> { CreateCollabRequest(), CreateCollabRequest() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsByTeamQuery>(), default))
                     .ReturnsAsync(requests);
        _mapperMock.Setup(m => m.Map<CollaborationRequestDto>(It.IsAny<CollaborationRequest>()))
                   .Returns(new CollaborationRequestDto());

        var result = await _service.GetRequestsByTeam(
            new GetRequestsByTeamRequest { TeamId = "team-1" }, _fakeContext);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRequestsByTeam_WithStatusFilter_FiltersResults()
    {
        var pending = CreateCollabRequest();
        var accepted = CreateCollabRequest();
        accepted.Accept("reviewer");

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsByTeamQuery>(), default))
                     .ReturnsAsync(new List<CollaborationRequest> { pending, accepted });
        _mapperMock.Setup(m => m.Map<CollaborationRequestDto>(It.IsAny<CollaborationRequest>()))
                   .Returns(new CollaborationRequestDto());

        var result = await _service.GetRequestsByTeam(
            new GetRequestsByTeamRequest { TeamId = "team-1", Status = "Pending" }, _fakeContext);

        result.Items.Should().HaveCount(1);
    }

    // ── GetRequestsByUser ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRequestsByUser_WithEmptyUserId_ThrowsRpcInvalidArgument()
    {
        var act = async () => await _service.GetRequestsByUser(
            new GetRequestsByUserRequest { UserId = "" }, _fakeContext);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetRequestsByUser_ReturnsItemsForUser()
    {
        var requests = new List<CollaborationRequest> { CreateCollabRequest() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsByUserQuery>(), default))
                     .ReturnsAsync(requests);
        _mapperMock.Setup(m => m.Map<CollaborationRequestDto>(It.IsAny<CollaborationRequest>()))
                   .Returns(new CollaborationRequestDto());

        var result = await _service.GetRequestsByUser(
            new GetRequestsByUserRequest { UserId = "user-1" }, _fakeContext);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRequestsByUser_WithStatusFilter_FiltersResults()
    {
        var pending = CreateCollabRequest();
        var rejected = CreateCollabRequest();
        rejected.Reject("admin");

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsByUserQuery>(), default))
                     .ReturnsAsync(new List<CollaborationRequest> { pending, rejected });
        _mapperMock.Setup(m => m.Map<CollaborationRequestDto>(It.IsAny<CollaborationRequest>()))
                   .Returns(new CollaborationRequestDto());

        var result = await _service.GetRequestsByUser(
            new GetRequestsByUserRequest { UserId = "user-1", Status = "Rejected" }, _fakeContext);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRequestsByTeam_WhenMediatorThrows_ThrowsRpcInternal()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsByTeamQuery>(), default))
                     .ThrowsAsync(new Exception("db failure"));

        var act = async () => await _service.GetRequestsByTeam(
            new GetRequestsByTeamRequest { TeamId = "team-1" }, _fakeContext);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Internal);
    }

    [Fact]
    public async Task GetRequestsByUser_WhenMediatorThrows_ThrowsRpcInternal()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCollaborationRequestsByUserQuery>(), default))
                     .ThrowsAsync(new Exception("db failure"));

        var act = async () => await _service.GetRequestsByUser(
            new GetRequestsByUserRequest { UserId = "user-1" }, _fakeContext);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Internal);
    }

    // ── FakeServerCallContext ─────────────────────────────────────────────────

    private sealed class FakeServerCallContext : ServerCallContext
    {
        protected override string MethodCore => "Test";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "ipv4:127.0.0.1:12345";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => new Metadata();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new Metadata();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new AuthContext(null, new Dictionary<string, List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotImplementedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}