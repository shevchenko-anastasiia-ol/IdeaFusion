using FluentAssertions;
using Grpc.Core;
using GrpcClients.Models;
using IdeaFusion.Grpc.Users;
using IdeaFusion.GrpcClients.Clients;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GrpcClients.Tests.Clients;

public class UserGrpcClientTests
{
    private readonly Mock<UserGrpcService.UserGrpcServiceClient> _grpcMock = new();
    private readonly Mock<ILogger<UserGrpcClient>>               _loggerMock = new();

    private UserGrpcClient CreateClient() => new(_grpcMock.Object, _loggerMock.Object);

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

    // ── GetUserByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsUserDto()
    {
        var userId = Guid.NewGuid();
        var dto    = new UserDto { UserId = userId.ToString(), UserName = "alice" };

        _grpcMock.Setup(c => c.GetUserByIdAsync(
                It.IsAny<GetUserByIdRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(new UserResponse { User = dto }));

        var result = await CreateClient().GetUserByIdAsync(userId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenNotFound_ReturnsNullAndLogsWarning()
    {
        var rpc = new RpcException(new Status(StatusCode.NotFound, "not found"));

        _grpcMock.Setup(c => c.GetUserByIdAsync(
                It.IsAny<GetUserByIdRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<UserResponse>(rpc));

        var result = await CreateClient().GetUserByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // ── GetUsersByIdsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersByIdsAsync_WhenSuccessful_ReturnsUsers()
    {
        var ids  = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var resp = new UsersResponse();
        resp.Users.Add(new UserDto { UserId = ids[0].ToString() });
        resp.Users.Add(new UserDto { UserId = ids[1].ToString() });

        _grpcMock.Setup(c => c.GetUsersByIdsAsync(
                It.IsAny<GetUsersByIdsRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(resp));

        var result = await CreateClient().GetUsersByIdsAsync(ids);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsersByIdsAsync_WhenRpcException_ReturnsEmptyAndLogsError()
    {
        var rpc = new RpcException(new Status(StatusCode.Unavailable, "unavailable"));

        _grpcMock.Setup(c => c.GetUsersByIdsAsync(
                It.IsAny<GetUsersByIdsRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<UsersResponse>(rpc));

        var result = await CreateClient().GetUsersByIdsAsync([Guid.NewGuid()]);

        result.Should().BeEmpty();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // ── RecordAuditAsync ──────────────────────────────────────────────────────

    private static AuditRecordMessage Audit(
        string action = "post.delete",
        string category = "Content") => new()
    {
        ActorId    = "22222222-2222-2222-2222-222222222222",
        ActorName  = "admin",
        Action     = action,
        TargetType = "post",
        TargetId   = "p-1",
        Details    = "{}",
        Category   = category
    };

    private void SetupRecordAudit(RecordAuditResponse response) =>
        _grpcMock.Setup(c => c.RecordAuditAsync(
                It.IsAny<RecordAuditRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(response));

    [Fact]
    public async Task RecordAuditAsync_WhenRecorded_ReturnsTrue()
    {
        SetupRecordAudit(new RecordAuditResponse { EntryId = "e-1", Recorded = true });

        (await CreateClient().RecordAuditAsync(Audit())).Should().BeTrue();
    }

    [Fact]
    public async Task RecordAuditAsync_SendsEveryFieldOfTheRecord()
    {
        var captured = new RecordAuditRequest();
        _grpcMock.Setup(c => c.RecordAuditAsync(
                It.IsAny<RecordAuditRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Callback<RecordAuditRequest, Metadata, DateTime?, CancellationToken>(
                (r, _, _, _) => captured = r)
            .Returns(MakeCall(new RecordAuditResponse { Recorded = true }));

        await CreateClient().RecordAuditAsync(Audit(action: "report.resolve", category: "Moderation"));

        captured.ActorId.Should().Be("22222222-2222-2222-2222-222222222222");
        captured.ActorName.Should().Be("admin");
        captured.Action.Should().Be("report.resolve");
        captured.TargetType.Should().Be("post");
        captured.TargetId.Should().Be("p-1");
        captured.Details.Should().Be("{}");
        captured.Category.Should().Be("Moderation");
    }

    [Fact]
    public async Task RecordAuditAsync_WhenServerRejects_ReturnsFalse()
    {
        SetupRecordAudit(new RecordAuditResponse { Recorded = false });

        (await CreateClient().RecordAuditAsync(Audit())).Should().BeFalse();
    }

    [Fact]
    public async Task RecordAuditAsync_WhenRpcFails_ReturnsFalseAndLogsTheError()
    {
        var rpc = new RpcException(new Status(StatusCode.Internal, "error"));

        _grpcMock.Setup(c => c.RecordAuditAsync(
                It.IsAny<RecordAuditRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<RecordAuditResponse>(rpc));

        // A caller reporting an audit entry must not take its own request down
        // because the trail was unavailable, so the failure is contained.
        (await CreateClient().RecordAuditAsync(Audit())).Should().BeFalse();

        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // ── CheckUserExistsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CheckUserExistsAsync_WhenExists_ReturnsTrue()
    {
        _grpcMock.Setup(c => c.CheckUserExistsAsync(
                It.IsAny<CheckUserExistsRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(new CheckUserExistsResponse { Exists = true }));

        var result = await CreateClient().CheckUserExistsAsync(Guid.NewGuid());

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckUserExistsAsync_WhenNotExists_ReturnsFalse()
    {
        _grpcMock.Setup(c => c.CheckUserExistsAsync(
                It.IsAny<CheckUserExistsRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(new CheckUserExistsResponse { Exists = false }));

        var result = await CreateClient().CheckUserExistsAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckUserExistsAsync_WhenRpcException_ReturnsFalseAndLogsError()
    {
        var rpc = new RpcException(new Status(StatusCode.Internal, "error"));

        _grpcMock.Setup(c => c.CheckUserExistsAsync(
                It.IsAny<CheckUserExistsRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<CheckUserExistsResponse>(rpc));

        var result = await CreateClient().CheckUserExistsAsync(Guid.NewGuid());

        result.Should().BeFalse();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}