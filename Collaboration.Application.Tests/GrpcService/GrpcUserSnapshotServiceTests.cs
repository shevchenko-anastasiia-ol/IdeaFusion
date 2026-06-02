using Collaboration.API.Services;
using FluentAssertions;
using GrpcClients.Interfaces;
using IdeaFusion.Grpc.Users;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.GrpcService;

public class GrpcUserSnapshotServiceTests
{
    private readonly Mock<IUserGrpcClient> _grpcClientMock = new();

    private GrpcUserSnapshotService CreateService() =>
        new(_grpcClientMock.Object);

    private static readonly Guid ValidGuid = Guid.NewGuid();

    // ── Valid GUID + gRPC returns user ────────────────────────────────────────

    [Fact]
    public async Task GetUserSnapshotDataAsync_ValidGuidWithUser_ReturnsUsernameAndAvatarUrl()
    {
        var dto = new UserDto { UserName = "alice", AvatarUrl = "https://avatar.png" };
        _grpcClientMock.Setup(c => c.GetUserByIdAsync(ValidGuid, default)).ReturnsAsync(dto);

        var (username, avatarUrl) = await CreateService().GetUserSnapshotDataAsync(ValidGuid.ToString());

        username.Should().Be("alice");
        avatarUrl.Should().Be("https://avatar.png");
    }

    [Fact]
    public async Task GetUserSnapshotDataAsync_ValidGuidWithEmptyAvatarUrl_ReturnsNullAvatarUrl()
    {
        var dto = new UserDto { UserName = "bob", AvatarUrl = "" };
        _grpcClientMock.Setup(c => c.GetUserByIdAsync(ValidGuid, default)).ReturnsAsync(dto);

        var (username, avatarUrl) = await CreateService().GetUserSnapshotDataAsync(ValidGuid.ToString());

        username.Should().Be("bob");
        avatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSnapshotDataAsync_ValidGuidWithWhitespaceAvatarUrl_ReturnsWhitespaceAsIs()
    {
        // Production code uses IsNullOrEmpty (not IsNullOrWhiteSpace), so whitespace is returned unchanged.
        var dto = new UserDto { UserName = "carol", AvatarUrl = "   " };
        _grpcClientMock.Setup(c => c.GetUserByIdAsync(ValidGuid, default)).ReturnsAsync(dto);

        var (username, avatarUrl) = await CreateService().GetUserSnapshotDataAsync(ValidGuid.ToString());

        avatarUrl.Should().Be("   ");
    }

    // ── Valid GUID but gRPC returns null ──────────────────────────────────────

    [Fact]
    public async Task GetUserSnapshotDataAsync_ValidGuidButNullDto_FallsBackToUserId()
    {
        _grpcClientMock.Setup(c => c.GetUserByIdAsync(ValidGuid, default))
                       .ReturnsAsync((UserDto?)null);

        var userId = ValidGuid.ToString();
        var (username, avatarUrl) = await CreateService().GetUserSnapshotDataAsync(userId);

        username.Should().Be(userId);
        avatarUrl.Should().BeNull();
    }

    // ── Non-GUID userId ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserSnapshotDataAsync_NonGuidUserId_FallsBackToUserId()
    {
        const string rawUserId = "not-a-valid-guid";

        var (username, avatarUrl) = await CreateService().GetUserSnapshotDataAsync(rawUserId);

        username.Should().Be(rawUserId);
        avatarUrl.Should().BeNull();
        _grpcClientMock.Verify(c => c.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── gRPC throws ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserSnapshotDataAsync_WhenGrpcThrows_FallsBackToUserId()
    {
        _grpcClientMock.Setup(c => c.GetUserByIdAsync(ValidGuid, default))
                       .ThrowsAsync(new Exception("gRPC unavailable"));

        var userId = ValidGuid.ToString();
        var (username, avatarUrl) = await CreateService().GetUserSnapshotDataAsync(userId);

        username.Should().Be(userId);
        avatarUrl.Should().BeNull();
    }

    // ── CancellationToken is forwarded ────────────────────────────────────────

    [Fact]
    public async Task GetUserSnapshotDataAsync_ForwardsCancellationToken()
    {
        var dto = new UserDto { UserName = "dave", AvatarUrl = "" };
        using var cts = new CancellationTokenSource();
        _grpcClientMock.Setup(c => c.GetUserByIdAsync(ValidGuid, cts.Token)).ReturnsAsync(dto);

        var (username, _) = await CreateService().GetUserSnapshotDataAsync(ValidGuid.ToString(), cts.Token);

        username.Should().Be("dave");
        _grpcClientMock.Verify(c => c.GetUserByIdAsync(ValidGuid, cts.Token), Times.Once);
    }
}