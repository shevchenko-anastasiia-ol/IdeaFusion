using AutoMapper;
using ContentBLL.DTO.Tag;
using ContentBLL.Services;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using ContentDomain.Exception;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Services;

public class TagServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    public TagServiceTests()
    {
        _uow.Setup(u => u.TagRepository).Returns(_tagRepo.Object);
    }

    private TagService Svc() => new(_uow.Object, _mapper.Object);

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        _tagRepo.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(new List<Tag>());
        _mapper.Setup(m => m.Map<IEnumerable<TagDto>>(It.IsAny<IEnumerable<Tag>>()))
               .Returns(new List<TagDto>());

        var result = await Svc().GetAllAsync(_ct);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var tags = new List<Tag> { new() { TagId = 1, Name = "dotnet" } };
        var dtos = new List<TagDto> { new() { TagId = 1, Name = "dotnet" } };
        _tagRepo.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(tags);
        _mapper.Setup(m => m.Map<IEnumerable<TagDto>>(tags)).Returns(dtos);

        var result = await Svc().GetAllAsync(_ct);

        result.Should().ContainSingle(t => t.Name == "dotnet");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenTagNotFound_ReturnsNull()
    {
        _tagRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Tag?)null);

        var result = await Svc().GetByIdAsync(99, _ct);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTagFound_ReturnsMappedDto()
    {
        var tag = new Tag { TagId = 1, Name = "csharp" };
        var dto = new TagDto { TagId = 1, Name = "csharp" };
        _tagRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(tag);
        _mapper.Setup(m => m.Map<TagDto>(tag)).Returns(dto);

        var result = await Svc().GetByIdAsync(1, _ct);

        result.Should().NotBeNull();
        result!.Name.Should().Be("csharp");
    }

    // ── GetByPostIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPostIdAsync_ReturnsMappedTags()
    {
        var tags = new List<Tag> { new() { TagId = 2, Name = "azure" } };
        var dtos = new List<TagDto> { new() { TagId = 2, Name = "azure" } };
        _tagRepo.Setup(r => r.GetByPostIdAsync(5, _ct)).ReturnsAsync(tags);
        _mapper.Setup(m => m.Map<IEnumerable<TagDto>>(tags)).Returns(dtos);

        var result = await Svc().GetByPostIdAsync(5, _ct);

        result.Should().ContainSingle(t => t.Name == "azure");
    }

    // ── GetByNameAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByNameAsync_WhenTagNotFound_ReturnsNull()
    {
        _tagRepo.Setup(r => r.GetByNameAsync("unknown", _ct)).ReturnsAsync((Tag?)null);

        var result = await Svc().GetByNameAsync("unknown", _ct);

        result.Should().BeNull();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenTagAlreadyExists_ThrowsBusinessConflictException()
    {
        _tagRepo.Setup(r => r.GetByNameAsync("dotnet", _ct))
                .ReturnsAsync(new Tag { TagId = 1, Name = "dotnet" });

        var dto = new TagCreateDto { Name = "dotnet" };

        await Svc().Invoking(s => s.CreateAsync(dto, _ct))
                   .Should().ThrowAsync<BusinessConflictException>()
                   .WithMessage("*dotnet*");
    }

    [Fact]
    public async Task CreateAsync_WhenTagDoesNotExist_AddsAndReturnsDto()
    {
        var tag = new Tag { TagId = 10, Name = "newtech" };
        var tagDto = new TagDto { TagId = 10, Name = "newtech" };

        _tagRepo.Setup(r => r.GetByNameAsync("newtech", _ct)).ReturnsAsync((Tag?)null);
        _mapper.Setup(m => m.Map<Tag>(It.IsAny<TagCreateDto>())).Returns(tag);
        _tagRepo.Setup(r => r.AddAsync(tag, _ct)).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<TagDto>(tag)).Returns(tagDto);

        var dto = new TagCreateDto { Name = "newtech" };
        var result = await Svc().CreateAsync(dto, _ct);

        result.Name.Should().Be("newtech");
        _tagRepo.Verify(r => r.AddAsync(tag, _ct), Times.Once);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenTagNotFound_ThrowsNotFoundException()
    {
        _tagRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Tag?)null);

        await Svc().Invoking(s => s.DeleteAsync(99, _ct))
                   .Should().ThrowAsync<NotFoundException>()
                   .WithMessage("*99*");
    }

    [Fact]
    public async Task DeleteAsync_WhenTagExists_CallsDelete()
    {
        var tag = new Tag { TagId = 4, Name = "old" };
        _tagRepo.Setup(r => r.GetByIdAsync(4, _ct)).ReturnsAsync(tag);
        _tagRepo.Setup(r => r.DeleteAsync(4, _ct)).Returns(Task.CompletedTask);

        await Svc().DeleteAsync(4, _ct);

        _tagRepo.Verify(r => r.DeleteAsync(4, _ct), Times.Once);
    }
}