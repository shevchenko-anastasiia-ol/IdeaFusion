using ContentAPI.Controllers;
using ContentBLL.DTO.SavedPost;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Content.Tests.Controllers;

public class SavedPostControllerTests
{
    private readonly Mock<ISavedPostService> _svcMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private SavedPostController Ctrl() => new(_svcMock.Object);

    // ── Count ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Count_ReturnsOkWithCount()
    {
        _svcMock.Setup(s => s.CountByPostAsync(1, _ct)).ReturnsAsync(4);

        var result = await Ctrl().Count(postId: 1, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(4);
    }

    // ── GetByUser ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUser_ReturnsOkWithSavedPosts()
    {
        var saved = new List<SavedPostDto> { new() { SavedPostId = 1 } };
        _svcMock.Setup(s => s.GetByUserIdAsync(3, _ct)).ReturnsAsync(saved);

        var result = await Ctrl().GetByUser(3, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((IEnumerable<SavedPostDto>)ok.Value!).Should().ContainSingle();
    }

    // ── Exists ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Exists_ReturnsOkWithBool()
    {
        _svcMock.Setup(s => s.ExistsAsync(1, 5, _ct)).ReturnsAsync(true);

        var result = await Ctrl().Exists(postId: 1, userId: 5, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(true);
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Save_ReturnsOkWithSavedPostDto()
    {
        var savedDto = new SavedPostDto { SavedPostId = 7, PostId = 1, UserId = 5 };
        _svcMock.Setup(s => s.SaveAsync(It.IsAny<SavedPostCreateDto>(), _ct)).ReturnsAsync(savedDto);

        var result = await Ctrl().Save(new SavedPostCreateDto { PostId = 1, UserId = 5 }, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((SavedPostDto)ok.Value!).SavedPostId.Should().Be(7);
    }

    // ── Unsave ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unsave_ReturnsNoContent()
    {
        _svcMock.Setup(s => s.UnsaveAsync(1, 5, _ct)).Returns(Task.CompletedTask);

        var result = await Ctrl().Unsave(postId: 1, userId: 5, _ct);

        result.Should().BeOfType<NoContentResult>();
    }
}