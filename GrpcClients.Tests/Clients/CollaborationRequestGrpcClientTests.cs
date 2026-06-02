using FluentAssertions;
using Grpc.Core;
using IdeaFusion.Grpc.CollaborationRequests;
using IdeaFusion.GrpcClients.Clients;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GrpcClients.Tests.Clients;

public class CollaborationRequestGrpcClientTests
{
    private readonly Mock<CollaborationRequestGrpcService.CollaborationRequestGrpcServiceClient> _grpcMock   = new();
    private readonly Mock<ILogger<CollaborationRequestGrpcClient>>                               _loggerMock = new();

    private CollaborationRequestGrpcClient CreateClient() => new(_grpcMock.Object, _loggerMock.Object);

    private static AsyncUnaryCall<T> MakeCall<T>(T response) =>
        new(Task.FromResult(response),
            Task.FromResult(Metadata.Empty),
            () => Status.DefaultSuccess,
            () => Metadata.Empty,
            () => { });

    private static AsyncUnaryCall<T> MakeFailingCall<T>(RpcException exception) =>
        new(Task.FromException<T>(exception),
            Task.FromResult(Metadata.Empty),
            () => new Status(StatusCode.Internal, ""),
            () => Metadata.Empty,
            () => { });

    // ── GetRequestByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetRequestByIdAsync_WhenExists_ReturnsDto()
    {
        var requestId = Guid.NewGuid();
        var dto       = new CollaborationRequestDto { RequestId = requestId.ToString(), Role = "Designer" };

        _grpcMock.Setup(c => c.GetRequestByIdAsync(
                It.IsAny<GetRequestByIdRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(new CollaborationRequestResponse { Request = dto }));

        var result = await CreateClient().GetRequestByIdAsync(requestId);

        result.Should().NotBeNull();
        result!.RequestId.Should().Be(requestId.ToString());
        result.Role.Should().Be("Designer");
    }

    [Fact]
    public async Task GetRequestByIdAsync_WhenNotFound_ReturnsNullAndLogsWarning()
    {
        var rpc = new RpcException(new Status(StatusCode.NotFound, "not found"));

        _grpcMock.Setup(c => c.GetRequestByIdAsync(
                It.IsAny<GetRequestByIdRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<CollaborationRequestResponse>(rpc));

        var result = await CreateClient().GetRequestByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // ── GetRequestsByTeamAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetRequestsByTeamAsync_WhenSuccessful_ReturnsList()
    {
        var teamId = Guid.NewGuid();
        var resp   = new CollaborationRequestsResponse();
        resp.Items.Add(new CollaborationRequestDto { TeamId = teamId.ToString() });

        _grpcMock.Setup(c => c.GetRequestsByTeamAsync(
                It.IsAny<GetRequestsByTeamRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(resp));

        var result = await CreateClient().GetRequestsByTeamAsync(teamId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRequestsByTeamAsync_WithStatus_PassesStatusToRequest()
    {
        var resp = new CollaborationRequestsResponse();

        _grpcMock.Setup(c => c.GetRequestsByTeamAsync(
                It.IsAny<GetRequestsByTeamRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(resp));

        var result = await CreateClient().GetRequestsByTeamAsync(Guid.NewGuid(), "Pending");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRequestsByTeamAsync_WhenRpcException_ReturnsEmptyAndLogsError()
    {
        var rpc = new RpcException(new Status(StatusCode.Unavailable, "unavailable"));

        _grpcMock.Setup(c => c.GetRequestsByTeamAsync(
                It.IsAny<GetRequestsByTeamRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<CollaborationRequestsResponse>(rpc));

        var result = await CreateClient().GetRequestsByTeamAsync(Guid.NewGuid());

        result.Should().BeEmpty();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // ── GetRequestsByUserAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetRequestsByUserAsync_WhenSuccessful_ReturnsList()
    {
        var userId = Guid.NewGuid();
        var resp   = new CollaborationRequestsResponse();
        resp.Items.Add(new CollaborationRequestDto { FromUserId = userId.ToString() });
        resp.Items.Add(new CollaborationRequestDto { FromUserId = userId.ToString() });

        _grpcMock.Setup(c => c.GetRequestsByUserAsync(
                It.IsAny<GetRequestsByUserRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(resp));

        var result = await CreateClient().GetRequestsByUserAsync(userId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRequestsByUserAsync_WithStatus_PassesStatusToRequest()
    {
        var resp = new CollaborationRequestsResponse();

        _grpcMock.Setup(c => c.GetRequestsByUserAsync(
                It.IsAny<GetRequestsByUserRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(resp));

        var result = await CreateClient().GetRequestsByUserAsync(Guid.NewGuid(), "Accepted");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRequestsByUserAsync_WhenRpcException_ReturnsEmptyAndLogsError()
    {
        var rpc = new RpcException(new Status(StatusCode.Internal, "error"));

        _grpcMock.Setup(c => c.GetRequestsByUserAsync(
                It.IsAny<GetRequestsByUserRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<CollaborationRequestsResponse>(rpc));

        var result = await CreateClient().GetRequestsByUserAsync(Guid.NewGuid());

        result.Should().BeEmpty();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}