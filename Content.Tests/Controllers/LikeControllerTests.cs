using ContentAPI.Controllers;
using ContentBLL.DTO.Like;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Content.Tests.Controllers;

public class LikeControllerTests
{
    private readonly Mock<ILikeService> _svcMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private LikeController Ctrl() => new(_svcMock.Object);

    // ── Count ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Count_ReturnsOkWithCount()
    {
        _svcMock.Setup(s => s.CountByPostAsync(1, _ct)).ReturnsAsync(5);

        var result = await Ctrl().Count(postId: 1, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(5);
    }

    // ── Exists ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Exists_WhenLikeExists_ReturnsOkTrue()
    {
        _svcMock.Setup(s => s.ExistsAsync(1, _ct)).ReturnsAsync(true);

        var result = await Ctrl().Exists(postId: 1, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(true);
    }

    [Fact]
    public async Task Exists_WhenLikeNotExists_ReturnsOkFalse()
    {
        _svcMock.Setup(s => s.ExistsAsync(1, _ct)).ReturnsAsync(false);

        var result = await Ctrl().Exists(postId: 1, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(false);
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_ReturnsOkWithLikeDto()
    {
        var likeDto = new LikeDto { LikeId = 1, PostId = 1, UserId = 2 };
        _svcMock.Setup(s => s.AddAsync(It.IsAny<LikeCreateDto>(), _ct)).ReturnsAsync(likeDto);

        var result = await Ctrl().Add(new LikeCreateDto { PostId = 1 }, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((LikeDto)ok.Value!).LikeId.Should().Be(1);
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_ReturnsNoContent()
    {
        _svcMock.Setup(s => s.RemoveAsync(1, _ct)).Returns(Task.CompletedTask);

        var result = await Ctrl().Remove(postId: 1, _ct);

        result.Should().BeOfType<NoContentResult>();
    }
}