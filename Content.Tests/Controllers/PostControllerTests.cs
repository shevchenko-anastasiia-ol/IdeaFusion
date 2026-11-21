using ContentAPI.Controllers;
using ContentAPI.Services;
using ContentBLL.DTO.Post;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Content.Tests.Controllers;

public class PostControllerTests
{
    private readonly Mock<IPostService> _svcMock = new();
    private readonly Mock<IContentAuditWriter> _auditMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private PostController Ctrl() => new(_svcMock.Object, _auditMock.Object);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var posts = new List<PostDto> { new() { PostId = 1, Title = "T1" } };
        _svcMock.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(posts);

        var result = await Ctrl().GetAll(_ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(posts);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var post = new PostDto { PostId = 1, Title = "Post" };
        _svcMock.Setup(s => s.GetByIdAsync(1, _ct)).ReturnsAsync(post);

        var result = await Ctrl().GetById(1, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((PostDto)ok.Value!).PostId.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _svcMock.Setup(s => s.GetByIdAsync(99, _ct)).ReturnsAsync((PostDto?)null);

        var result = await Ctrl().GetById(99, _ct);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── GetByAuthor ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByAuthor_ReturnsOkWithPosts()
    {
        _svcMock.Setup(s => s.GetByAuthorAsync(5, _ct))
                .ReturnsAsync(new List<PostDto> { new() { PostId = 2 } });

        var result = await Ctrl().GetByAuthor(5, _ct);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── GetByUserId ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserId_ReturnsOk()
    {
        _svcMock.Setup(s => s.GetByUserIdAsync(3, _ct)).ReturnsAsync(new List<PostDto>());

        var result = await Ctrl().GetByUserId(3, _ct);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── GetByCollaboration ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCollaboration_ReturnsOk()
    {
        _svcMock.Setup(s => s.GetByCollaborationAsync(10, _ct)).ReturnsAsync(new List<PostDto>());

        var result = await Ctrl().GetByCollaboration(10, _ct);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── GetByTag ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTag_ReturnsOk()
    {
        _svcMock.Setup(s => s.GetByTagAsync(7, _ct)).ReturnsAsync(new List<PostDto>());

        var result = await Ctrl().GetByTag(7, _ct);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var created = new PostDto { PostId = 5, Title = "New Post" };
        _svcMock.Setup(s => s.CreateAsync(It.IsAny<PostCreateDto>(), _ct)).ReturnsAsync(created);

        var result = await Ctrl().Create(new PostCreateDto { Title = "New Post" }, _ct);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        ((PostDto)createdResult.Value!).PostId.Should().Be(5);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOkWithUpdatedPost()
    {
        var updated = new PostDto { PostId = 1, Title = "Updated" };
        _svcMock.Setup(s => s.UpdateAsync(1, It.IsAny<PostUpdateDto>(), _ct)).ReturnsAsync(updated);

        var result = await Ctrl().Update(1, new PostUpdateDto { Title = "Updated" }, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((PostDto)ok.Value!).Title.Should().Be("Updated");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _svcMock.Setup(s => s.DeleteAsync(1, _ct)).Returns(Task.CompletedTask);

        var result = await Ctrl().Delete(1, _ct);

        result.Should().BeOfType<NoContentResult>();
    }

    // ── Archive ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Archive_ReturnsNoContent()
    {
        _svcMock.Setup(s => s.ArchiveAsync(1, _ct)).Returns(Task.CompletedTask);

        var result = await Ctrl().Archive(1, _ct);

        result.Should().BeOfType<NoContentResult>();
    }
}