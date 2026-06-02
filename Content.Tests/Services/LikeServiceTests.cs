using AutoMapper;
using ContentBLL.DTO.Like;
using ContentBLL.Services;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using ContentDomain.Exception;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Services;

public class LikeServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPostRepository> _postRepo = new();
    private readonly Mock<ILikeRepository> _likeRepo = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    public LikeServiceTests()
    {
        _uow.Setup(u => u.PostRepository).Returns(_postRepo.Object);
        _uow.Setup(u => u.LikeRepository).Returns(_likeRepo.Object);
    }

    private LikeService Svc() => new(_uow.Object, _mapper.Object);

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_WhenPostNotFound_ThrowsNotFoundException()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync((Post?)null);
        var dto = new LikeCreateDto { PostId = 1, UserId = 10 };

        await Svc().Invoking(s => s.AddAsync(dto, _ct))
                   .Should().ThrowAsync<NotFoundException>()
                   .WithMessage("*1*");
    }

    [Fact]
    public async Task AddAsync_WhenAlreadyLiked_ThrowsBusinessConflictException()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(new Post { PostId = 1 });
        _likeRepo.Setup(r => r.ExistsAsync(1, 10, _ct)).ReturnsAsync(true);
        var dto = new LikeCreateDto { PostId = 1, UserId = 10 };

        await Svc().Invoking(s => s.AddAsync(dto, _ct))
                   .Should().ThrowAsync<BusinessConflictException>();
    }

    [Fact]
    public async Task AddAsync_WithValidData_AddsLikeAndReturnsDto()
    {
        var post = new Post { PostId = 1 };
        var like = new Like { LikeId = 5, PostId = 1, UserId = 10 };
        var likeDto = new LikeDto { LikeId = 5, PostId = 1, UserId = 10 };

        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(post);
        _likeRepo.Setup(r => r.ExistsAsync(1, 10, _ct)).ReturnsAsync(false);
        _mapper.Setup(m => m.Map<Like>(It.IsAny<LikeCreateDto>())).Returns(like);
        _likeRepo.Setup(r => r.AddAsync(like, _ct)).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<LikeDto>(like)).Returns(likeDto);

        var dto = new LikeCreateDto { PostId = 1, UserId = 10 };
        var result = await Svc().AddAsync(dto, _ct);

        result.LikeId.Should().Be(5);
        _likeRepo.Verify(r => r.AddAsync(like, _ct), Times.Once);
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_WhenLikeNotExists_ThrowsNotFoundException()
    {
        _likeRepo.Setup(r => r.ExistsAsync(1, 10, _ct)).ReturnsAsync(false);

        await Svc().Invoking(s => s.RemoveAsync(1, 10, _ct))
                   .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_WhenLikeExists_CallsDelete()
    {
        _likeRepo.Setup(r => r.ExistsAsync(1, 10, _ct)).ReturnsAsync(true);
        _likeRepo.Setup(r => r.DeleteAsync(1, 10, _ct)).Returns(Task.CompletedTask);

        await Svc().RemoveAsync(1, 10, _ct);

        _likeRepo.Verify(r => r.DeleteAsync(1, 10, _ct), Times.Once);
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_ReturnsRepositoryResult()
    {
        _likeRepo.Setup(r => r.ExistsAsync(2, 5, _ct)).ReturnsAsync(true);

        var result = await Svc().ExistsAsync(2, 5, _ct);

        result.Should().BeTrue();
    }

    // ── CountByPostAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CountByPostAsync_ReturnsRepositoryCount()
    {
        _likeRepo.Setup(r => r.CountByPostAsync(3, _ct)).ReturnsAsync(7);

        var result = await Svc().CountByPostAsync(3, _ct);

        result.Should().Be(7);
    }
}