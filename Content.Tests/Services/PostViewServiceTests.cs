using ContentBLL.Services;
using ContentDAL.Repository.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using FluentAssertions;
using Moq;
using Xunit;

namespace Content.Tests.Services;

public class PostViewServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPostRepository> _postRepo = new();
    private readonly Mock<IPostViewRepository> _viewRepo = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    public PostViewServiceTests()
    {
        _uow.Setup(u => u.PostRepository).Returns(_postRepo.Object);
        _uow.Setup(u => u.PostViewRepository).Returns(_viewRepo.Object);
    }

    private PostViewService Svc() => new(_uow.Object);

    // ── RecordAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordAsync_WhenPostNotFound_DoesNotAddView()
    {
        _postRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Post?)null);

        await Svc().RecordAsync(99, userId: 1, _ct);

        _viewRepo.Verify(r => r.AddAsync(It.IsAny<PostView>(), _ct), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_WhenPostExists_AddsView()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(new Post { PostId = 1 });
        _viewRepo.Setup(r => r.AddAsync(It.IsAny<PostView>(), _ct)).Returns(Task.CompletedTask);

        await Svc().RecordAsync(1, userId: 5, _ct);

        _viewRepo.Verify(r => r.AddAsync(
            It.Is<PostView>(v => v.PostId == 1 && v.UserId == 5), _ct), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_WithNullUserId_AddsAnonymousView()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(new Post { PostId = 1 });
        _viewRepo.Setup(r => r.AddAsync(It.IsAny<PostView>(), _ct)).Returns(Task.CompletedTask);

        await Svc().RecordAsync(1, userId: null, _ct);

        _viewRepo.Verify(r => r.AddAsync(
            It.Is<PostView>(v => v.PostId == 1 && v.UserId == null), _ct), Times.Once);
    }

    // ── CountByPostAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CountByPostAsync_ReturnsRepositoryCount()
    {
        _viewRepo.Setup(r => r.CountByPostAsync(3, _ct)).ReturnsAsync(42);

        var result = await Svc().CountByPostAsync(3, _ct);

        result.Should().Be(42);
    }
}