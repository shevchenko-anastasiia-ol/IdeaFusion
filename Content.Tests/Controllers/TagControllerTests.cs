using ContentAPI.Controllers;
using ContentBLL.DTO.Tag;
using ContentBLL.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Content.Tests.Controllers;

public class TagControllerTests
{
    private readonly Mock<ITagService> _svcMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private TagController Ctrl() => new(_svcMock.Object);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithAllTags()
    {
        var tags = new List<TagDto> { new() { TagId = 1, Name = "dotnet" } };
        _svcMock.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(tags);

        var result = await Ctrl().GetAll(_ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((IEnumerable<TagDto>)ok.Value!).Should().ContainSingle();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var tag = new TagDto { TagId = 1, Name = "dotnet" };
        _svcMock.Setup(s => s.GetByIdAsync(1, _ct)).ReturnsAsync(tag);

        var result = await Ctrl().GetById(1, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((TagDto)ok.Value!).TagId.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _svcMock.Setup(s => s.GetByIdAsync(99, _ct)).ReturnsAsync((TagDto?)null);

        var result = await Ctrl().GetById(99, _ct);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── GetByPost ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPost_ReturnsOkWithTags()
    {
        var tags = new List<TagDto> { new() { TagId = 2, Name = "azure" } };
        _svcMock.Setup(s => s.GetByPostIdAsync(5, _ct)).ReturnsAsync(tags);

        var result = await Ctrl().GetByPost(5, _ct);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((IEnumerable<TagDto>)ok.Value!).Should().ContainSingle();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var created = new TagDto { TagId = 10, Name = "newtech" };
        _svcMock.Setup(s => s.CreateAsync(It.IsAny<TagCreateDto>(), _ct)).ReturnsAsync(created);

        var result = await Ctrl().Create(new TagCreateDto { Name = "newtech" }, _ct);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        ((TagDto)createdResult.Value!).TagId.Should().Be(10);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _svcMock.Setup(s => s.DeleteAsync(1, _ct)).Returns(Task.CompletedTask);

        var result = await Ctrl().Delete(1, _ct);

        result.Should().BeOfType<NoContentResult>();
    }
}