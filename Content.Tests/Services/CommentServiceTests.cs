using AutoMapper;
using ContentBLL.DTO.Comment;
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

public class CommentServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPostRepository> _postRepo = new();
    private readonly Mock<ICommentRepository> _commentRepo = new();
    private readonly Mock<IPostAuthorResolver> _authorResolver = new();
    private readonly Mock<ICurrentUserAccessor> _currentUser = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    public CommentServiceTests()
    {
        _uow.Setup(u => u.PostRepository).Returns(_postRepo.Object);
        _uow.Setup(u => u.CommentRepository).Returns(_commentRepo.Object);

        // The caller is the token's subject: author id 1, so the comments these
        // tests act on (PostAuthorId 1) belong to them.
        _authorResolver
            .Setup(r => r.ResolveForWriteAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPostAuthor(1, 55, Guid.Empty, "user", null, false));
    }

    private CommentService Svc() =>
        new(_uow.Object, _mapper.Object, _authorResolver.Object, _currentUser.Object);

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenCommentNotFound_ReturnsNull()
    {
        _commentRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync((Comment?)null);

        var result = await Svc().GetByIdAsync(1, _ct);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCommentFound_ReturnsMappedDto()
    {
        var comment = new Comment { CommentId = 1, Body = "Hi" };
        var dto = new CommentDto { CommentId = 1, Body = "Hi" };
        _commentRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(comment);
        _mapper.Setup(m => m.Map<CommentDto>(comment)).Returns(dto);

        var result = await Svc().GetByIdAsync(1, _ct);

        result.Should().NotBeNull();
        result!.CommentId.Should().Be(1);
    }

    // ── GetByPostIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPostIdAsync_ReturnsMappedComments()
    {
        var comments = new List<Comment> { new() { CommentId = 1 } };
        var dtos = new List<CommentDto> { new() { CommentId = 1 } };
        _commentRepo.Setup(r => r.GetByPostIdAsync(5, _ct)).ReturnsAsync(comments);
        _mapper.Setup(m => m.Map<IEnumerable<CommentDto>>(comments)).Returns(dtos);

        var result = await Svc().GetByPostIdAsync(5, _ct);

        result.Should().ContainSingle(c => c.CommentId == 1);
    }

    // ── GetRepliesAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRepliesAsync_ReturnsMappedReplies()
    {
        var replies = new List<Comment>();
        _commentRepo.Setup(r => r.GetRepliesAsync(10, _ct)).ReturnsAsync(replies);
        _mapper.Setup(m => m.Map<IEnumerable<CommentDto>>(replies)).Returns(new List<CommentDto>());

        var result = await Svc().GetRepliesAsync(10, _ct);

        result.Should().BeEmpty();
        _commentRepo.Verify(r => r.GetRepliesAsync(10, _ct), Times.Once);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenPostNotFound_ThrowsNotFoundException()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync((Post?)null);
        var dto = new CommentCreateDto { PostId = 1, Body = "Hi" };

        await Svc().Invoking(s => s.CreateAsync(dto, _ct))
                   .Should().ThrowAsync<NotFoundException>()
                   .WithMessage("*1*");
    }

    [Fact]
    public async Task CreateAsync_WhenParentCommentNotFound_ThrowsNotFoundException()
    {
        var post = new Post { PostId = 2 };
        _postRepo.Setup(r => r.GetByIdAsync(2, _ct)).ReturnsAsync(post);
        _commentRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Comment?)null);

        var dto = new CommentCreateDto
        {
            PostId = 2, Body = "Reply",
            ParentCommentId = 99
        };

        await Svc().Invoking(s => s.CreateAsync(dto, _ct))
                   .Should().ThrowAsync<NotFoundException>()
                   .WithMessage("*99*");
    }

    [Fact]
    public async Task CreateAsync_WhenParentBelongsToDifferentPost_ThrowsBusinessConflictException()
    {
        var post = new Post { PostId = 2 };
        var parent = new Comment { CommentId = 5, PostId = 99 };
        _postRepo.Setup(r => r.GetByIdAsync(2, _ct)).ReturnsAsync(post);
        _commentRepo.Setup(r => r.GetByIdAsync(5, _ct)).ReturnsAsync(parent);

        var dto = new CommentCreateDto
        {
            PostId = 2, Body = "Reply",
            ParentCommentId = 5
        };

        await Svc().Invoking(s => s.CreateAsync(dto, _ct))
                   .Should().ThrowAsync<BusinessConflictException>();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_AddsCommentAndReturnsDto()
    {
        var post = new Post { PostId = 2 };
        var comment = new Comment { CommentId = 10, PostId = 2, Body = "Hello" };
        var resultDto = new CommentDto { CommentId = 10, Body = "Hello" };

        _postRepo.Setup(r => r.GetByIdAsync(2, _ct)).ReturnsAsync(post);
        _mapper.Setup(m => m.Map<Comment>(It.IsAny<CommentCreateDto>())).Returns(comment);
        _commentRepo.Setup(r => r.AddAsync(comment, _ct)).Returns(Task.CompletedTask);
        _commentRepo.Setup(r => r.GetByIdAsync(10, _ct)).ReturnsAsync(comment);
        _mapper.Setup(m => m.Map<CommentDto>(comment)).Returns(resultDto);

        var dto = new CommentCreateDto { PostId = 2, Body = "Hello" };
        var result = await Svc().CreateAsync(dto, _ct);

        result.CommentId.Should().Be(10);
        _commentRepo.Verify(r => r.AddAsync(comment, _ct), Times.Once);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenCommentNotFound_ThrowsNotFoundException()
    {
        _commentRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Comment?)null);
        var dto = new CommentUpdateDto { Body = "Updated" };

        await Svc().Invoking(s => s.UpdateAsync(99, dto, _ct))
                   .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenCommentFound_UpdatesAndReturnsDto()
    {
        var comment = new Comment { CommentId = 3, Body = "Old", PostAuthorId = 1 };
        var updatedDto = new CommentDto { CommentId = 3, Body = "New" };

        _commentRepo.Setup(r => r.GetByIdAsync(3, _ct)).ReturnsAsync(comment);
        _mapper.Setup(m => m.Map(It.IsAny<CommentUpdateDto>(), comment));
        _commentRepo.Setup(r => r.UpdateAsync(comment, _ct)).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<CommentDto>(comment)).Returns(updatedDto);

        var dto = new CommentUpdateDto { Body = "New" };
        var result = await Svc().UpdateAsync(3, dto, _ct);

        result.CommentId.Should().Be(3);
        _commentRepo.Verify(r => r.UpdateAsync(comment, _ct), Times.Once);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenCommentNotFound_ThrowsNotFoundException()
    {
        _commentRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Comment?)null);

        await Svc().Invoking(s => s.DeleteAsync(99, _ct))
                   .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenCommentExists_CallsDelete()
    {
        var comment = new Comment { CommentId = 7, PostAuthorId = 1 };
        _commentRepo.Setup(r => r.GetByIdAsync(7, _ct)).ReturnsAsync(comment);
        _commentRepo.Setup(r => r.DeleteAsync(7, _ct)).Returns(Task.CompletedTask);

        await Svc().DeleteAsync(7, _ct);

        _commentRepo.Verify(r => r.DeleteAsync(7, _ct), Times.Once);
    }

    // ── CreateAsync: valid reply (parent found, same post) ────────────────────

    [Fact]
    public async Task CreateAsync_WithValidParentComment_SamePost_CreatesReply()
    {
        var post    = new Post { PostId = 4 };
        var parent  = new Comment { CommentId = 20, PostId = 4 };
        var created = new Comment { CommentId = 21, PostId = 4 };
        var dto     = new CommentDto { CommentId = 21 };

        _postRepo.Setup(r => r.GetByIdAsync(4, _ct)).ReturnsAsync(post);
        _commentRepo.Setup(r => r.GetByIdAsync(20, _ct)).ReturnsAsync(parent);
        _mapper.Setup(m => m.Map<Comment>(It.IsAny<CommentCreateDto>())).Returns(created);
        _commentRepo.Setup(r => r.AddAsync(created, _ct)).Returns(Task.CompletedTask);
        _commentRepo.Setup(r => r.GetByIdAsync(21, _ct)).ReturnsAsync(created);
        _mapper.Setup(m => m.Map<CommentDto>(created)).Returns(dto);

        var createDto = new CommentCreateDto
        {
            PostId = 4, Body = "Reply",
            ParentCommentId = 20
        };
        var result = await Svc().CreateAsync(createDto, _ct);

        result.CommentId.Should().Be(21);
        _commentRepo.Verify(r => r.AddAsync(created, _ct), Times.Once);
    }
}