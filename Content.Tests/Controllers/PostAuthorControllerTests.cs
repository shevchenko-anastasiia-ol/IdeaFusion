using ContentAPI.Controllers;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Content.Tests.Controllers;

public class PostAuthorControllerTests
{
    private readonly Mock<IPostService> _serviceMock = new();
    private PostAuthorController Ctrl() => new(_serviceMock.Object);

    // ── Returns Ok with the user id ───────────────────────────────────────────

    [Fact]
    public async Task GetByUserName_ValidUser_ReturnsOk()
    {
        _serviceMock
            .Setup(s => s.EnsurePostAuthorAsync("alice", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 1, UserId: 201));

        var result = await Ctrl().GetByUserName("alice", null, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByUserName_ValidUser_ReturnsBothIds()
    {
        _serviceMock
            .Setup(s => s.EnsurePostAuthorAsync("alice", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 1, UserId: 201));

        var result = await Ctrl().GetByUserName("alice", null, CancellationToken.None);

        var ok = (OkObjectResult)result.Result!;
        var value = ok.Value!;
        var postAuthorId = (int)value.GetType().GetProperty("postAuthorId")!.GetValue(value)!;
        var userId = (int)value.GetType().GetProperty("userId")!.GetValue(value)!;
        postAuthorId.Should().Be(1);
        userId.Should().Be(201);
    }

    [Fact]
    public async Task GetByUserName_WithAvatarUrl_ReturnsOk()
    {
        _serviceMock
            .Setup(s => s.EnsurePostAuthorAsync("bob", "https://img.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 2, UserId: 202));

        var result = await Ctrl().GetByUserName("bob", "https://img.png", CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── Forwards arguments to service ─────────────────────────────────────────

    [Fact]
    public async Task GetByUserName_PassesUserNameToService()
    {
        _serviceMock
            .Setup(s => s.EnsurePostAuthorAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 1, UserId: 201));

        await Ctrl().GetByUserName("charlie", null, CancellationToken.None);

        _serviceMock.Verify(
            s => s.EnsurePostAuthorAsync("charlie", It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByUserName_PassesAvatarUrlToService()
    {
        _serviceMock
            .Setup(s => s.EnsurePostAuthorAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 1, UserId: 201));

        await Ctrl().GetByUserName("dave", "https://avatar.jpg", CancellationToken.None);

        _serviceMock.Verify(
            s => s.EnsurePostAuthorAsync("dave", "https://avatar.jpg", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByUserName_NullAvatarUrl_PassesNullToService()
    {
        _serviceMock
            .Setup(s => s.EnsurePostAuthorAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 3, UserId: 203));

        await Ctrl().GetByUserName("eve", null, CancellationToken.None);

        _serviceMock.Verify(
            s => s.EnsurePostAuthorAsync("eve", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Service is called exactly once ────────────────────────────────────────

    [Fact]
    public async Task GetByUserName_CallsServiceExactlyOnce()
    {
        _serviceMock
            .Setup(s => s.EnsurePostAuthorAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostAuthorId: 4, UserId: 204));

        await Ctrl().GetByUserName("frank", null, CancellationToken.None);

        _serviceMock.Verify(
            s => s.EnsurePostAuthorAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
