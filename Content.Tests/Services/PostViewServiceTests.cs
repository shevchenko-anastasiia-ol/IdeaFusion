using ContentBLL.Services;
using ContentBLL.Services.Interfaces;
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
    private readonly Mock<IPostAuthorResolver> _authorResolver = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    public PostViewServiceTests()
    {
        _uow.Setup(u => u.PostRepository).Returns(_postRepo.Object);
        _uow.Setup(u => u.PostViewRepository).Returns(_viewRepo.Object);
    }

    /// <summary>The viewer is the token's subject: content-side user id 5.</summary>
    private void SetViewer(int? userId) =>
        _authorResolver
            .Setup(r => r.FindAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId is null
                ? null
                : new ResolvedPostAuthor(1, userId.Value, Guid.Empty, "viewer", null, false));

    private PostViewService Svc() => new(_uow.Object, _authorResolver.Object);

    // ── RecordAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordAsync_WhenPostNotFound_DoesNotAddView()
    {
        _postRepo.Setup(r => r.GetByIdAsync(99, _ct)).ReturnsAsync((Post?)null);
        SetViewer(1);

        await Svc().RecordAsync(99, _ct);

        _viewRepo.Verify(r => r.AddAsync(It.IsAny<PostView>(), _ct), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_WhenPostExists_AddsView()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(new Post { PostId = 1 });
        _viewRepo.Setup(r => r.AddAsync(It.IsAny<PostView>(), _ct)).Returns(Task.CompletedTask);
        SetViewer(5);

        await Svc().RecordAsync(1, _ct);

        _viewRepo.Verify(r => r.AddAsync(
            It.Is<PostView>(v => v.PostId == 1 && v.UserId == 5), _ct), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_WithoutAnAuthorRow_AddsAnonymousView()
    {
        _postRepo.Setup(r => r.GetByIdAsync(1, _ct)).ReturnsAsync(new Post { PostId = 1 });
        _viewRepo.Setup(r => r.AddAsync(It.IsAny<PostView>(), _ct)).Returns(Task.CompletedTask);
        SetViewer(null);

        await Svc().RecordAsync(1, _ct);

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