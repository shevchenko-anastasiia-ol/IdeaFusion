using FluentAssertions;
using Grpc.Core;
using IdeaFusion.Grpc.Teams;
using IdeaFusion.GrpcClients.Clients;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GrpcClients.Tests.Clients;

public class TeamGrpcClientTests
{
    private readonly Mock<TeamGrpcService.TeamGrpcServiceClient> _grpcMock   = new();
    private readonly Mock<ILogger<TeamGrpcClient>>               _loggerMock = new();

    private TeamGrpcClient CreateClient() => new(_grpcMock.Object, _loggerMock.Object);

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

    // ── GetTeamByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeamByIdAsync_WhenTeamExists_ReturnsTeamDto()
    {
        var teamId = Guid.NewGuid();
        var dto    = new TeamDto { TeamId = teamId.ToString(), Name = "Alpha" };

        _grpcMock.Setup(c => c.GetTeamByIdAsync(
                It.IsAny<GetTeamByIdRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(new TeamResponse { Team = dto }));

        var result = await CreateClient().GetTeamByIdAsync(teamId);

        result.Should().NotBeNull();
        result!.TeamId.Should().Be(teamId.ToString());
        result.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetTeamByIdAsync_WhenNotFound_ReturnsNullAndLogsWarning()
    {
        var rpc = new RpcException(new Status(StatusCode.NotFound, "not found"));

        _grpcMock.Setup(c => c.GetTeamByIdAsync(
                It.IsAny<GetTeamByIdRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<TeamResponse>(rpc));

        var result = await CreateClient().GetTeamByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // ── GetTeamsByIdsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeamsByIdsAsync_WhenSuccessful_ReturnsTeams()
    {
        var ids  = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var resp = new TeamsResponse();
        resp.Teams.Add(new TeamDto { TeamId = ids[0].ToString() });
        resp.Teams.Add(new TeamDto { TeamId = ids[1].ToString() });

        _grpcMock.Setup(c => c.GetTeamsByIdsAsync(
                It.IsAny<GetTeamsByIdsRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(resp));

        var result = await CreateClient().GetTeamsByIdsAsync(ids);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTeamsByIdsAsync_WhenRpcException_ReturnsEmptyAndLogsError()
    {
        var rpc = new RpcException(new Status(StatusCode.Unavailable, "unavailable"));

        _grpcMock.Setup(c => c.GetTeamsByIdsAsync(
                It.IsAny<GetTeamsByIdsRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<TeamsResponse>(rpc));

        var result = await CreateClient().GetTeamsByIdsAsync([Guid.NewGuid()]);

        result.Should().BeEmpty();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // ── GetTeamsPagedAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeamsPagedAsync_WhenSuccessful_ReturnsPagedResponse()
    {
        var resp = new PagedTeamsResponse { TotalCount = 1, PageNumber = 1, PageSize = 10 };
        resp.Items.Add(new TeamDto { Name = "Alpha" });

        _grpcMock.Setup(c => c.GetTeamsPagedAsync(
                It.IsAny<GetTeamsPagedRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(resp));

        var result = await CreateClient().GetTeamsPagedAsync(1, 10);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTeamsPagedAsync_WithAllOptionalParams_PassesThem()
    {
        var resp = new PagedTeamsResponse();

        _grpcMock.Setup(c => c.GetTeamsPagedAsync(
                It.IsAny<GetTeamsPagedRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(resp));

        var result = await CreateClient().GetTeamsPagedAsync(
            2, 5, "name", "alpha", "Design", "art", "Active");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTeamsPagedAsync_WhenRpcException_ReturnsNullAndLogsError()
    {
        var rpc = new RpcException(new Status(StatusCode.Internal, "error"));

        _grpcMock.Setup(c => c.GetTeamsPagedAsync(
                It.IsAny<GetTeamsPagedRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<PagedTeamsResponse>(rpc));

        var result = await CreateClient().GetTeamsPagedAsync(1, 10);

        result.Should().BeNull();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    // ── CheckUserIsMemberAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CheckUserIsMemberAsync_WhenMember_ReturnsResponse()
    {
        var resp = new CheckUserIsMemberResponse { IsMember = true, Role = "Developer" };

        _grpcMock.Setup(c => c.CheckUserIsMemberAsync(
                It.IsAny<CheckUserIsMemberRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeCall(resp));

        var result = await CreateClient().CheckUserIsMemberAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().NotBeNull();
        result!.IsMember.Should().BeTrue();
        result.Role.Should().Be("Developer");
    }

    [Fact]
    public async Task CheckUserIsMemberAsync_WhenRpcException_ReturnsNullAndLogsError()
    {
        var rpc = new RpcException(new Status(StatusCode.Internal, "error"));

        _grpcMock.Setup(c => c.CheckUserIsMemberAsync(
                It.IsAny<CheckUserIsMemberRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(MakeFailingCall<CheckUserIsMemberResponse>(rpc));

        var result = await CreateClient().CheckUserIsMemberAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            rpc,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}