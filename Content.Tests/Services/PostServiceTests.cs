using AutoMapper;
using ContentBLL.DTO.Post;
using ContentBLL.Services;
using ContentBLL.Services.Interfaces;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using ContentDomain.Exception;
using FluentAssertions;
using Moq;
using ServiceDefaults.Interfaces;
using Xunit;

namespace Content.Tests.Services;

public class PostServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPostRepository> _postRepo = new();
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<IPostAuthorResolver> _authorResolver = new();
    private readonly Mock<ICurrentUserAccessor> _currentUser = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    /// <summary>The content-side author id every owned post in these tests carries.</summary>
    private const int CallerPostAuthorId = 7;

    public PostServiceTests()
    {
        _uow.Setup(u => u.PostRepository).Returns(_postRepo.Object);
        _uow.Setup(u => u.TagRepository).Returns(_tagRepo.Object);

        // The author is the token's subject, so the rows these tests edit belong
        // to caller.PostAuthorId and the ownership guard lets them through.
        _authorResolver
            .Setup(r => r.ResolveForWriteAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPostAuthor(
                CallerPostAuthorId, 42, Guid.Empty, "alice", null, false));
        _authorResolver
            .Setup(r => r.ResolveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPostAuthor(
                CallerPostAuthorId, 42, Guid.Empty, "alice", null, false));
    }

    private PostService Svc() =>
        new(_uow.Object, _mapper.Object, _authorResolver.Object, _currentUser.Object);

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenPostNotFound_ReturnsNull()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync((Post?)null);

        var result = await Svc().GetByIdAsync(1, _ct);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenPostFound_ReturnsDtoWithTags()
    {
        var post = new Post { PostId = 1, Title = "Hello", Media = new List<PostMedia>() };
        var dto = new PostDto { PostId = 1, Title = "Hello" };

        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(post);
        _tagRepo.Setup(r => r.GetByPostIdAsync(1, _ct)).ReturnsAsync(
            new List<Tag> { new() { Name = "dotnet" } });
        _mapper.Setup(m => m.Map<PostDto>(post)).Returns(dto);

        var result = await Svc().GetByIdAsync(1, _ct);

        result.Should().NotBeNull();
        result!.PostId.Should().Be(1);
        result.Tags.Should().ContainSingle("dotnet");
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WhenNoPosts_ReturnsEmpty()
    {
        _postRepo.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(new List<Post>());

        var result = await Svc().GetAllAsync(_ct);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var post = new Post { PostId = 2, Title = "Post2", Media = new List<PostMedia>() };
        var dto = new PostDto { PostId = 2, Title = "Post2" };

        _postRepo.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(new List<Post> { post });
        _tagRepo.Setup(r => r.GetByPostIdAsync(2, _ct)).ReturnsAsync(new List<Tag>());
        _postRepo.Setup(r => r.GetMediaByPostIdAsync(2, _ct)).ReturnsAsync(new List<PostMedia>());
        _mapper.Setup(m => m.Map<PostDto>(post)).Returns(dto);

        var result = await Svc().GetAllAsync(_ct);

        result.Should().ContainSingle(p => p.PostId == 2);
    }

    // ── GetByAuthorAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByAuthorAsync_DelegatesToRepository()
    {
        _postRepo.Setup(r => r.GetByAuthorAsync(7, _ct)).ReturnsAsync(new List<Post>());

        var result = await Svc().GetByAuthorAsync(7, _ct);

        result.Should().BeEmpty();
        _postRepo.Verify(r => r.GetByAuthorAsync(7, _ct), Times.Once);
    }

    // ── GetByTagAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTagAsync_DelegatesToRepository()
    {
        _postRepo.Setup(r => r.GetByTagAsync(3, _ct)).ReturnsAsync(new List<Post>());

        var result = await Svc().GetByTagAsync(3, _ct);

        result.Should().BeEmpty();
        _postRepo.Verify(r => r.GetByTagAsync(3, _ct), Times.Once);
    }

    // ── GetByUserIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_DelegatesToRepository()
    {
        _postRepo.Setup(r => r.GetByUserIdAsync(10, _ct)).ReturnsAsync(new List<Post>());

        var result = await Svc().GetByUserIdAsync(10, _ct);

        result.Should().BeEmpty();
        _postRepo.Verify(r => r.GetByUserIdAsync(10, _ct), Times.Once);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenPostNotFound_ThrowsNotFoundException()
    {
        _postRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Post?)null);

        await Svc().Invoking(s => s.DeleteAsync(99, _ct))
                   .Should().ThrowAsync<NotFoundException>()
                   .WithMessage("*99*");
    }

    [Fact]
    public async Task DeleteAsync_WhenPostExists_DeletesMediaAndPost()
    {
        var post = new Post { PostId = 5 };
        _postRepo.Setup(r => r.GetByIdAsync(5, _ct)).ReturnsAsync(post);
        _postRepo.Setup(r => r.DeleteMediaAsync(5, _ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.DeleteAsync(5, _ct)).Returns(Task.CompletedTask);

        await Svc().DeleteAsync(5, _ct);

        _postRepo.Verify(r => r.DeleteMediaAsync(5, _ct), Times.Once);
        _postRepo.Verify(r => r.DeleteAsync(5, _ct), Times.Once);
    }

    // ── ArchiveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ArchiveAsync_WhenPostNotFound_ThrowsNotFoundException()
    {
        _postRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Post?)null);

        await Svc().Invoking(s => s.ArchiveAsync(99, _ct))
                   .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ArchiveAsync_WhenPostExists_CallsArchive()
    {
        var post = new Post { PostId = 3 };
        _postRepo.Setup(r => r.GetByIdAsync(3, _ct)).ReturnsAsync(post);
        _postRepo.Setup(r => r.ArchiveAsync(3, _ct)).Returns(Task.CompletedTask);

        await Svc().ArchiveAsync(3, _ct);

        _postRepo.Verify(r => r.ArchiveAsync(3, _ct), Times.Once);
    }

    // ── GetByCollaborationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetByCollaborationAsync_DelegatesToRepository()
    {
        _postRepo.Setup(r => r.GetByCollaborationAsync(5, _ct)).ReturnsAsync(new List<Post>());

        var result = await Svc().GetByCollaborationAsync(5, _ct);

        result.Should().BeEmpty();
        _postRepo.Verify(r => r.GetByCollaborationAsync(5, _ct), Times.Once);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    private void SetupCreateAsync(Post post, PostCreateDto createDto, PostDto returnDto)
    {
        _mapper.Setup(m => m.Map<Post>(createDto)).Returns(post);
        _uow.Setup(u => u.BeginTransactionAsync(_ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.AddAsync(post, _ct)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitAsync(_ct)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(_ct)).Returns(Task.CompletedTask);
        // GetByIdAsync is called internally at the end of CreateAsync
        _postRepo.Setup(r => r.GetByIdAsync(post.PostId, _ct)).ReturnsAsync(post);
        _tagRepo.Setup(r => r.GetByPostIdAsync(post.PostId, _ct)).ReturnsAsync(new List<Tag>());
        _postRepo.Setup(r => r.GetMediaByPostIdAsync(post.PostId, _ct)).ReturnsAsync(new List<PostMedia>());
        _mapper.Setup(m => m.Map<PostDto>(post)).Returns(returnDto);
    }

    [Fact]
    public async Task CreateAsync_CallsBeginTransaction()
    {
        var post      = new Post { PostId = 0, Media = new List<PostMedia>() };
        var createDto = new PostCreateDto { Title = "T", TagIds = new(), MediaFiles = new() };
        var returnDto = new PostDto { PostId = 0 };
        SetupCreateAsync(post, createDto, returnDto);

        await Svc().CreateAsync(createDto, _ct);

        _uow.Verify(u => u.BeginTransactionAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OnSuccess_CallsCommit()
    {
        var post      = new Post { PostId = 0, Media = new List<PostMedia>() };
        var createDto = new PostCreateDto { Title = "T", TagIds = new(), MediaFiles = new() };
        var returnDto = new PostDto { PostId = 0 };
        SetupCreateAsync(post, createDto, returnDto);

        await Svc().CreateAsync(createDto, _ct);

        _uow.Verify(u => u.CommitAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_TakesThePostAuthorFromTheToken()
    {
        var post      = new Post { PostId = 0, Media = new List<PostMedia>() };
        var createDto = new PostCreateDto { Title = "T", TagIds = new(), MediaFiles = new() };
        var returnDto = new PostDto { PostId = 0 };
        SetupCreateAsync(post, createDto, returnDto);

        await Svc().CreateAsync(createDto, _ct);

        post.PostAuthorId.Should().Be(CallerPostAuthorId);
        post.CreatedBy.Should().Be("alice");
    }

    [Fact]
    public async Task CreateAsync_WithTagIds_AddsPostTagsForEachId()
    {
        var post      = new Post { PostId = 0, Media = new List<PostMedia>() };
        var createDto = new PostCreateDto { Title = "T", TagIds = new List<int> { 1, 2 }, MediaFiles = new() };
        var returnDto = new PostDto { PostId = 0 };
        _tagRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), _ct)).ReturnsAsync(new Tag { TagId = 1 });
        _tagRepo.Setup(r => r.AddPostTagAsync(It.IsAny<int>(), It.IsAny<int>(), _ct)).Returns(Task.CompletedTask);
        SetupCreateAsync(post, createDto, returnDto);

        await Svc().CreateAsync(createDto, _ct);

        _tagRepo.Verify(r => r.AddPostTagAsync(0, 1, _ct), Times.Once);
        _tagRepo.Verify(r => r.AddPostTagAsync(0, 2, _ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OnException_CallsRollback()
    {
        var post      = new Post { PostId = 0, Media = new List<PostMedia>() };
        var createDto = new PostCreateDto { Title = "T", TagIds = new(), MediaFiles = new() };
        _mapper.Setup(m => m.Map<Post>(createDto)).Returns(post);
        _uow.Setup(u => u.BeginTransactionAsync(_ct)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(_ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.AddAsync(post, _ct)).ThrowsAsync(new Exception("db error"));

        await Assert.ThrowsAsync<Exception>(() => Svc().CreateAsync(createDto, _ct));

        _uow.Verify(u => u.RollbackAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsMappedDto()
    {
        var post      = new Post { PostId = 0, Media = new List<PostMedia>() };
        var createDto = new PostCreateDto { Title = "T", TagIds = new(), MediaFiles = new() };
        var returnDto = new PostDto { PostId = 0, Title = "T" };
        SetupCreateAsync(post, createDto, returnDto);

        var result = await Svc().CreateAsync(createDto, _ct);

        result.Title.Should().Be("T");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    private void SetupUpdateAsync(int id, Post existing, PostUpdateDto updateDto, PostDto returnDto)
    {
        _postRepo.Setup(r => r.GetByIdAsync(id, _ct)).ReturnsAsync(existing);
        _mapper.Setup(m => m.Map<PostUpdateDto, Post>(updateDto, existing)).Returns(existing);
        _uow.Setup(u => u.BeginTransactionAsync(_ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.UpdateAsync(existing, _ct)).Returns(Task.CompletedTask);
        _tagRepo.Setup(r => r.DeletePostTagsAsync(id, _ct)).Returns(Task.CompletedTask);
        _tagRepo.Setup(r => r.AddPostTagAsync(It.IsAny<int>(), It.IsAny<int>(), _ct)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitAsync(_ct)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(_ct)).Returns(Task.CompletedTask);
        // GetByIdAsync called again at the end via GetByIdAsync(id, ct)
        _tagRepo.Setup(r => r.GetByPostIdAsync(id, _ct)).ReturnsAsync(new List<Tag>());
        _postRepo.Setup(r => r.GetMediaByPostIdAsync(id, _ct)).ReturnsAsync(new List<PostMedia>());
        _mapper.Setup(m => m.Map<PostDto>(existing)).Returns(returnDto);
    }

    [Fact]
    public async Task UpdateAsync_WhenPostNotFound_ThrowsNotFoundException()
    {
        _postRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Post?)null);
        var updateDto = new PostUpdateDto { Title = "T", TagIds = new() };

        await Svc().Invoking(s => s.UpdateAsync(99, updateDto, _ct))
                   .Should().ThrowAsync<ContentDomain.Exception.NotFoundException>()
                   .WithMessage("*99*");
    }

    [Fact]
    public async Task UpdateAsync_CallsBeginTransaction()
    {
        var existing  = new Post { PostId = 1, Media = new List<PostMedia>() };
        var updateDto = new PostUpdateDto { Title = "T", TagIds = new() };
        var returnDto = new PostDto { PostId = 1 };
        SetupUpdateAsync(1, existing, updateDto, returnDto);

        await Svc().UpdateAsync(1, updateDto, _ct);

        _uow.Verify(u => u.BeginTransactionAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsUpdateRepository()
    {
        var existing  = new Post { PostId = 2, Media = new List<PostMedia>() };
        var updateDto = new PostUpdateDto { Title = "New", TagIds = new() };
        var returnDto = new PostDto { PostId = 2 };
        SetupUpdateAsync(2, existing, updateDto, returnDto);

        await Svc().UpdateAsync(2, updateDto, _ct);

        _postRepo.Verify(r => r.UpdateAsync(existing, _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeletesAndReinsertsTags()
    {
        var existing  = new Post { PostId = 3, Media = new List<PostMedia>() };
        var updateDto = new PostUpdateDto { Title = "T", TagIds = new List<int> { 10 } };
        var returnDto = new PostDto { PostId = 3 };
        SetupUpdateAsync(3, existing, updateDto, returnDto);

        await Svc().UpdateAsync(3, updateDto, _ct);

        _tagRepo.Verify(r => r.DeletePostTagsAsync(3, _ct), Times.Once);
        _tagRepo.Verify(r => r.AddPostTagAsync(3, 10, _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_OnSuccess_CallsCommit()
    {
        var existing  = new Post { PostId = 4, Media = new List<PostMedia>() };
        var updateDto = new PostUpdateDto { Title = "T", TagIds = new() };
        var returnDto = new PostDto { PostId = 4 };
        SetupUpdateAsync(4, existing, updateDto, returnDto);

        await Svc().UpdateAsync(4, updateDto, _ct);

        _uow.Verify(u => u.CommitAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_OnException_CallsRollback()
    {
        var existing  = new Post { PostId = 5, Media = new List<PostMedia>() };
        var updateDto = new PostUpdateDto { Title = "T", TagIds = new() };
        _postRepo.Setup(r => r.GetByIdAsync(5, _ct)).ReturnsAsync(existing);
        _mapper.Setup(m => m.Map<PostUpdateDto, Post>(updateDto, existing)).Returns(existing);
        _uow.Setup(u => u.BeginTransactionAsync(_ct)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(_ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.UpdateAsync(existing, _ct)).ThrowsAsync(new Exception("db error"));

        await Assert.ThrowsAsync<Exception>(() => Svc().UpdateAsync(5, updateDto, _ct));

        _uow.Verify(u => u.RollbackAsync(_ct), Times.Once);
    }

    // ── GetByIdAsync with media ───────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WithMediaOnPost_BuildsMediaUrls()
    {
        var media = new PostMedia { Id = 1, PostId = 7 };
        var post = new Post { PostId = 7, Title = "Has Media", Media = new List<PostMedia> { media } };
        var dto = new PostDto { PostId = 7 };

        _postRepo.Setup(r => r.GetByIdAsync(7, _ct)).ReturnsAsync(post);
        _tagRepo.Setup(r => r.GetByPostIdAsync(7, _ct)).ReturnsAsync(new List<Tag>());
        _mapper.Setup(m => m.Map<PostDto>(post)).Returns(dto);
        _postRepo.Setup(r => r.GetMediaUrlAsync(media, default)).ReturnsAsync("https://cdn.example.com/img.jpg");

        var result = await Svc().GetByIdAsync(7, _ct);

        result!.MediaUrls.Should().ContainSingle("https://cdn.example.com/img.jpg");
    }

    // ── CreateAsync with MediaFiles ───────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithMediaFiles_CallsUploadMediaForEach()
    {
        var fakeFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>().Object;
        var post     = new Post { PostId = 0, Media = new List<PostMedia>() };
        var createDto = new PostCreateDto
        {
            Title = "T", TagIds = new(),
            MediaFiles = new List<Microsoft.AspNetCore.Http.IFormFile> { fakeFile }
        };
        var returnDto = new PostDto { PostId = 0 };
        var uploadedMedia = new PostMedia { Id = 99 };

        _mapper.Setup(m => m.Map<Post>(createDto)).Returns(post);
        _uow.Setup(u => u.BeginTransactionAsync(_ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.AddAsync(post, _ct)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitAsync(_ct)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(_ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.GetByIdAsync(post.PostId, _ct)).ReturnsAsync(post);
        _tagRepo.Setup(r => r.GetByPostIdAsync(post.PostId, _ct)).ReturnsAsync(new List<Tag>());
        _postRepo.Setup(r => r.GetMediaByPostIdAsync(post.PostId, _ct)).ReturnsAsync(new List<PostMedia>());
        _mapper.Setup(m => m.Map<PostDto>(post)).Returns(returnDto);
        _postRepo.Setup(r => r.UploadMediaAsync(post.PostId, fakeFile, _ct)).ReturnsAsync(uploadedMedia);

        await Svc().CreateAsync(createDto, _ct);

        _postRepo.Verify(r => r.UploadMediaAsync(post.PostId, fakeFile, _ct), Times.Once);
    }

    // ── UpdateAsync with NewMediaFiles ────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithNewMediaFiles_DeletesAndUploadsMedia()
    {
        var fakeFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>().Object;
        var existing = new Post { PostId = 6, Media = new List<PostMedia>() };
        var updateDto = new PostUpdateDto
        {
            Title = "T", TagIds = new(),
            NewMediaFiles = new List<Microsoft.AspNetCore.Http.IFormFile> { fakeFile }
        };
        var returnDto = new PostDto { PostId = 6 };
        var uploadedMedia = new PostMedia { Id = 77 };

        _postRepo.Setup(r => r.GetByIdAsync(6, _ct)).ReturnsAsync(existing);
        _mapper.Setup(m => m.Map<PostUpdateDto, Post>(updateDto, existing)).Returns(existing);
        _uow.Setup(u => u.BeginTransactionAsync(_ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.UpdateAsync(existing, _ct)).Returns(Task.CompletedTask);
        _tagRepo.Setup(r => r.DeletePostTagsAsync(6, _ct)).Returns(Task.CompletedTask);
        _tagRepo.Setup(r => r.AddPostTagAsync(It.IsAny<int>(), It.IsAny<int>(), _ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.DeleteMediaAsync(6, _ct)).Returns(Task.CompletedTask);
        _postRepo.Setup(r => r.UploadMediaAsync(6, fakeFile, _ct)).ReturnsAsync(uploadedMedia);
        _uow.Setup(u => u.CommitAsync(_ct)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(_ct)).Returns(Task.CompletedTask);
        _tagRepo.Setup(r => r.GetByPostIdAsync(6, _ct)).ReturnsAsync(new List<Tag>());
        _postRepo.Setup(r => r.GetMediaByPostIdAsync(6, _ct)).ReturnsAsync(new List<PostMedia>());
        _mapper.Setup(m => m.Map<PostDto>(existing)).Returns(returnDto);

        await Svc().UpdateAsync(6, updateDto, _ct);

        _postRepo.Verify(r => r.DeleteMediaAsync(6, _ct), Times.Once);
        _postRepo.Verify(r => r.UploadMediaAsync(6, fakeFile, _ct), Times.Once);
    }
}