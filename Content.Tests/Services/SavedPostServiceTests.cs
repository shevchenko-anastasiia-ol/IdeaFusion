using AutoMapper;
using ContentBLL.DTO.SavedPost;
using ContentBLL.Services;
using ContentBLL.Services.Interfaces;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using ContentDomain.Exception;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Services;

public class SavedPostServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPostRepository> _postRepo = new();
    private readonly Mock<ISavedPostRepository> _savedPostRepo = new();
    private readonly Mock<IPostAuthorResolver> _authorResolver = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    public SavedPostServiceTests()
    {
        _uow.Setup(u => u.PostRepository).Returns(_postRepo.Object);
        _uow.Setup(u => u.SavedPostRepository).Returns(_savedPostRepo.Object);
    }

    /// <summary>
    /// The saver is the token's subject, so tests pin the content-side user id
    /// the repository is expected to be queried/deleted by. A null id means the
    /// caller has never written anything and therefore has no row.
    /// </summary>
    private void SetCaller(int? userId)
    {
        var resolved = userId is null
            ? null
            : new ResolvedPostAuthor(1, userId.Value, Guid.Empty, "alice", null, false);

        _authorResolver
            .Setup(r => r.ResolveForWriteAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved!);
        _authorResolver
            .Setup(r => r.ResolveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved!);
        _authorResolver
            .Setup(r => r.FindAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);
    }

    private SavedPostService Svc() => new(_uow.Object, _mapper.Object, _authorResolver.Object);

    // ── SaveAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_WhenPostNotFound_ThrowsNotFoundException()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync((Post?)null);
        var dto = new SavedPostCreateDto { PostId = 1 };

        await Svc().Invoking(s => s.SaveAsync(dto, _ct))
                   .Should().ThrowAsync<NotFoundException>()
                   .WithMessage("*1*");
    }

    [Fact]
    public async Task SaveAsync_WhenAlreadySaved_ThrowsBusinessConflictException()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(new Post { PostId = 1 });
        _savedPostRepo.Setup(r => r.ExistsAsync(1, 5, _ct)).ReturnsAsync(true);
        SetCaller(5);
        var dto = new SavedPostCreateDto { PostId = 1 };

        await Svc().Invoking(s => s.SaveAsync(dto, _ct))
                   .Should().ThrowAsync<BusinessConflictException>();
    }

    [Fact]
    public async Task SaveAsync_WithValidData_AddsAndReturnsDto()
    {
        var post = new Post { PostId = 1, Title = "My Post" };
        var savedPost = new SavedPost { SavedPostId = 10, PostId = 1, UserId = 5 };
        var savedPostDto = new SavedPostDto { SavedPostId = 10, PostId = 1, UserId = 5, PostTitle = "My Post" };

        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(post);
        _savedPostRepo.Setup(r => r.ExistsAsync(1, 5, _ct)).ReturnsAsync(false);
        _mapper.Setup(m => m.Map<SavedPost>(It.IsAny<SavedPostCreateDto>())).Returns(savedPost);
        _savedPostRepo.Setup(r => r.AddAsync(savedPost, _ct)).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<SavedPostDto>(savedPost)).Returns(savedPostDto);

        SetCaller(5);

        var dto = new SavedPostCreateDto { PostId = 1 };
        var result = await Svc().SaveAsync(dto, _ct);

        result.SavedPostId.Should().Be(10);
        result.PostTitle.Should().Be("My Post");
        _savedPostRepo.Verify(r => r.AddAsync(savedPost, _ct), Times.Once);
    }

    // ── UnsaveAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UnsaveAsync_WhenSavedPostNotExists_ThrowsNotFoundException()
    {
        _savedPostRepo.Setup(r => r.ExistsAsync(1, 5, _ct)).ReturnsAsync(false);
        SetCaller(5);

        await Svc().Invoking(s => s.UnsaveAsync(1, _ct))
                   .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UnsaveAsync_WhenExists_CallsDelete()
    {
        _savedPostRepo.Setup(r => r.ExistsAsync(1, 5, _ct)).ReturnsAsync(true);
        _savedPostRepo.Setup(r => r.DeleteAsync(1, 5, _ct)).Returns(Task.CompletedTask);
        SetCaller(5);

        await Svc().UnsaveAsync(1, _ct);

        _savedPostRepo.Verify(r => r.DeleteAsync(1, 5, _ct), Times.Once);
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_ReturnsRepositoryResult()
    {
        _savedPostRepo.Setup(r => r.ExistsAsync(2, 3, _ct)).ReturnsAsync(true);
        SetCaller(3);

        var result = await Svc().ExistsAsync(2, _ct);

        result.Should().BeTrue();
    }

    // ── CountByPostAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CountByPostAsync_ReturnsRepositoryCount()
    {
        _savedPostRepo.Setup(r => r.CountByPostAsync(4, _ct)).ReturnsAsync(3);

        var result = await Svc().CountByPostAsync(4, _ct);

        result.Should().Be(3);
    }

    // ── GetMineAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMineAsync_WhenNoSavedPosts_ReturnsEmpty()
    {
        _savedPostRepo.Setup(r => r.GetByUserIdAsync(7, _ct)).ReturnsAsync(new List<SavedPost>());
        SetCaller(7);

        var result = await Svc().GetMineAsync(_ct);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMineAsync_WithoutAnAuthorRow_ReturnsEmpty()
    {
        SetCaller(null);
        _savedPostRepo
            .Setup(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SavedPost>());

        var result = await Svc().GetMineAsync(_ct);

        result.Should().BeEmpty();
        _savedPostRepo.Verify(
            r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMineAsync_BuildsDtoWithTitleAndMedia()
    {
        var post = new Post { PostId = 1, Title = "Post Title" };
        var saved = new SavedPost { SavedPostId = 1, PostId = 1, UserId = 7, Post = post };

        _savedPostRepo.Setup(r => r.GetByUserIdAsync(7, _ct)).ReturnsAsync(new List<SavedPost> { saved });
        _postRepo.Setup(r => r.GetMediaByPostIdAsync(1, _ct)).ReturnsAsync(new List<PostMedia>());
        SetCaller(7);

        var result = (await Svc().GetMineAsync(_ct)).ToList();

        result.Should().ContainSingle();
        result[0].PostTitle.Should().Be("Post Title");
        result[0].PostMediaUrls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMineAsync_WhenPostIsNull_UsesEmptyStringForTitle()
    {
        var saved = new SavedPost { SavedPostId = 2, PostId = 5, UserId = 9, Post = null };

        _savedPostRepo.Setup(r => r.GetByUserIdAsync(9, _ct)).ReturnsAsync(new List<SavedPost> { saved });
        _postRepo.Setup(r => r.GetMediaByPostIdAsync(5, _ct)).ReturnsAsync(new List<PostMedia>());
        SetCaller(9);

        var result = (await Svc().GetMineAsync(_ct)).ToList();

        result.Should().ContainSingle();
        result[0].PostTitle.Should().Be(string.Empty);
    }

    [Fact]
    public async Task GetMineAsync_WithMediaItems_AddsUrlsToDto()
    {
        var post = new Post { PostId = 3, Title = "Media Post" };
        var media = new PostMedia { Id = 10, PostId = 3 };
        var saved = new SavedPost { SavedPostId = 3, PostId = 3, UserId = 11, Post = post };

        _savedPostRepo.Setup(r => r.GetByUserIdAsync(11, _ct)).ReturnsAsync(new List<SavedPost> { saved });
        _postRepo.Setup(r => r.GetMediaByPostIdAsync(3, _ct)).ReturnsAsync(new List<PostMedia> { media });
        _postRepo.Setup(r => r.GetMediaUrlAsync(media, _ct)).ReturnsAsync("https://cdn.example.com/img.jpg");
        SetCaller(11);

        var result = (await Svc().GetMineAsync(_ct)).ToList();

        result.Should().ContainSingle();
        result[0].PostMediaUrls.Should().ContainSingle("https://cdn.example.com/img.jpg");
    }
}