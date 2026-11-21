using ContentAPI.Controllers;
using ContentBLL.DTO.Post;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Content.Tests.Controllers;

public class PostAuthorControllerTests
{
    private readonly Mock<IPostAuthorResolver> _resolverMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private PostAuthorController Ctrl() => new(_resolverMock.Object);

    private static ResolvedPostAuthor Author(
        int postAuthorId = 1,
        int userId = 201,
        string userName = "alice",
        string? avatarUrl = null,
        bool created = false) =>
        new(postAuthorId, userId, Guid.Empty, userName, avatarUrl, created);

    // ── GetMe ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_ReturnsOk()
    {
        _resolverMock.Setup(r => r.ResolveForWriteAsync(null, _ct)).ReturnsAsync(Author());

        var result = await Ctrl().GetMe(_ct);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMe_ReturnsBothIds()
    {
        _resolverMock.Setup(r => r.ResolveForWriteAsync(null, _ct)).ReturnsAsync(Author(1, 201));

        var result = await Ctrl().GetMe(_ct);

        var dto = (PostAuthorDto)((OkObjectResult)result.Result!).Value!;
        dto.PostAuthorId.Should().Be(1);
        dto.UserId.Should().Be(201);
    }

    [Fact]
    public async Task GetMe_ReportsWhetherTheRowWasJustCreated()
    {
        _resolverMock.Setup(r => r.ResolveForWriteAsync(null, _ct)).ReturnsAsync(Author(created: true));

        var result = await Ctrl().GetMe(_ct);

        var dto = (PostAuthorDto)((OkObjectResult)result.Result!).Value!;
        dto.Created.Should().BeTrue();
    }

    [Fact]
    public async Task GetMe_CallsResolverExactlyOnce()
    {
        _resolverMock.Setup(r => r.ResolveForWriteAsync(null, _ct)).ReturnsAsync(Author());

        await Ctrl().GetMe(_ct);

        _resolverMock.Verify(r => r.ResolveForWriteAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateOwnAvatar ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOwnAvatar_WithAvatarUrl_ReturnsOk()
    {
        _resolverMock
            .Setup(r => r.ResolveForWriteAsync("https://img.png", _ct))
            .ReturnsAsync(Author(2, 202, "bob", "https://img.png"));

        var result = await Ctrl().UpdateOwnAvatar("https://img.png", _ct);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateOwnAvatar_ReturnsUpdatedAvatar()
    {
        _resolverMock
            .Setup(r => r.ResolveForWriteAsync("https://avatar.jpg", _ct))
            .ReturnsAsync(Author(2, 202, "bob", "https://avatar.jpg"));

        var result = await Ctrl().UpdateOwnAvatar("https://avatar.jpg", _ct);

        var dto = (PostAuthorDto)((OkObjectResult)result.Result!).Value!;
        dto.AvatarUrl.Should().Be("https://avatar.jpg");
    }

    [Fact]
    public async Task UpdateOwnAvatar_NullAvatarUrl_PassesNullToResolver()
    {
        _resolverMock.Setup(r => r.ResolveForWriteAsync(null, _ct)).ReturnsAsync(Author(3, 203, "eve"));

        await Ctrl().UpdateOwnAvatar(null, _ct);

        _resolverMock.Verify(r => r.ResolveForWriteAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOwnAvatar_PassesAvatarUrlToResolver()
    {
        _resolverMock
            .Setup(r => r.ResolveForWriteAsync(It.IsAny<string?>(), _ct))
            .ReturnsAsync(Author());

        await Ctrl().UpdateOwnAvatar("https://avatar.jpg", _ct);

        _resolverMock.Verify(r => r.ResolveForWriteAsync("https://avatar.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetByUserName ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserName_ValidUser_ReturnsOk()
    {
        _resolverMock
            .Setup(r => r.FindByUserNameAsync("alice", _ct))
            .ReturnsAsync(Author(1, 201));

        var result = await Ctrl().GetByUserName("alice", _ct);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByUserName_ValidUser_ReturnsBothIds()
    {
        _resolverMock
            .Setup(r => r.FindByUserNameAsync("alice", _ct))
            .ReturnsAsync(Author(1, 201));

        var result = await Ctrl().GetByUserName("alice", _ct);

        var dto = (PostAuthorDto)((OkObjectResult)result.Result!).Value!;
        dto.PostAuthorId.Should().Be(1);
        dto.UserId.Should().Be(201);
    }

    [Fact]
    public async Task GetByUserName_WhenNoRow_ReturnsNotFound()
    {
        _resolverMock
            .Setup(r => r.FindByUserNameAsync("ghost", _ct))
            .ReturnsAsync((ResolvedPostAuthor?)null);

        var result = await Ctrl().GetByUserName("ghost", _ct);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByUserName_PassesUserNameToResolver()
    {
        _resolverMock
            .Setup(r => r.FindByUserNameAsync(It.IsAny<string>(), _ct))
            .ReturnsAsync(Author());

        await Ctrl().GetByUserName("charlie", _ct);

        _resolverMock.Verify(r => r.FindByUserNameAsync("charlie", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserName_DoesNotCreateARow()
    {
        _resolverMock
            .Setup(r => r.FindByUserNameAsync(It.IsAny<string>(), _ct))
            .ReturnsAsync(Author(created: false));

        var result = await Ctrl().GetByUserName("dave", _ct);

        var dto = (PostAuthorDto)((OkObjectResult)result.Result!).Value!;
        dto.Created.Should().BeFalse();
        _resolverMock.Verify(r => r.ResolveForWriteAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByUserName_CallsResolverExactlyOnce()
    {
        _resolverMock
            .Setup(r => r.FindByUserNameAsync(It.IsAny<string>(), _ct))
            .ReturnsAsync(Author(4, 204, "frank"));

        await Ctrl().GetByUserName("frank", _ct);

        _resolverMock.Verify(r => r.FindByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
