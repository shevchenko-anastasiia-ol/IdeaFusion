using ContentAPI.Controllers;
using ContentBLL.DTO.Comment;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Content.Tests.Controllers;

public class CommentControllerTests
{
    private readonly Mock<ICommentService> _svcMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private CommentController Ctrl() => new(_svcMock.Object);

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var comment = new CommentDto { CommentId = 1, Body = "Hello" };
        _svcMock.Setup(s => s.GetByIdAsync(1, _ct)).ReturnsAsync(comment);

        var result = await Ctrl().GetById(1, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((CommentDto)ok.Value!).CommentId.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _svcMock.Setup(s => s.GetByIdAsync(99, _ct)).ReturnsAsync((CommentDto?)null);

        var result = await Ctrl().GetById(99, _ct);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── GetByPost ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPost_ReturnsOkWithComments()
    {
        var comments = new List<CommentDto> { new() { CommentId = 1 } };
        _svcMock.Setup(s => s.GetByPostIdAsync(5, _ct)).ReturnsAsync(comments);

        var result = await Ctrl().GetByPost(5, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((IEnumerable<CommentDto>)ok.Value!).Should().ContainSingle();
    }

    // ── GetReplies ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReplies_ReturnsOk()
    {
        _svcMock.Setup(s => s.GetRepliesAsync(10, _ct)).ReturnsAsync(new List<CommentDto>());

        var result = await Ctrl().GetReplies(10, _ct);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var created = new CommentDto { CommentId = 7, Body = "New comment" };
        _svcMock.Setup(s => s.CreateAsync(It.IsAny<CommentCreateDto>(), _ct)).ReturnsAsync(created);

        var result = await Ctrl().Create(
            new CommentCreateDto { PostId = 1, PostAuthorId = 1, Body = "New comment", CreatedBy = "user" }, _ct);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        ((CommentDto)createdResult.Value!).CommentId.Should().Be(7);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var updated = new CommentDto { CommentId = 3, Body = "Updated" };
        _svcMock.Setup(s => s.UpdateAsync(3, It.IsAny<CommentUpdateDto>(), _ct)).ReturnsAsync(updated);

        var result = await Ctrl().Update(3, new CommentUpdateDto { Body = "Updated", UpdatedBy = "user" }, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((CommentDto)ok.Value!).Body.Should().Be("Updated");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _svcMock.Setup(s => s.DeleteAsync(5, _ct)).Returns(Task.CompletedTask);

        var result = await Ctrl().Delete(5, _ct);

        result.Should().BeOfType<NoContentResult>();
    }
}