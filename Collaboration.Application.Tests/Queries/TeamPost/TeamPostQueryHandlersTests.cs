using Collaboration.Application.Queries.TeamPost;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Queries.TeamPost;

public class TeamPostQueryHandlersTests
{
    private readonly Mock<ITeamPostRepository> _repoMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static Domain.Entities.TeamPost CreatePost(
        string postId = "post-1",
        string teamId = "team-1",
        string authorId = "user-1",
        string title = "My Title")
    {
        var author = new UserSnapshot(authorId, "alice");
        return new Domain.Entities.TeamPost(postId, teamId, author, title);
    }

    // ── GetTeamPostByPostIdQueryHandler ───────────────────────────────────────

    [Fact]
    public async Task GetByPostId_WhenPostExists_ReturnsPost()
    {
        var post = CreatePost();
        _repoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct)).ReturnsAsync(post);

        var handler = new GetTeamPostByPostIdQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetTeamPostByPostIdQuery { PostId = "post-1" }, _ct);

        result.Should().NotBeNull();
        result!.PostId.Should().Be("post-1");
        result.TeamId.Should().Be("team-1");
    }

    [Fact]
    public async Task GetByPostId_WhenPostNotFound_ThrowsDomainException()
    {
        _repoMock.Setup(r => r.GetByPostIdAsync("missing", _ct))
                 .ReturnsAsync((Domain.Entities.TeamPost?)null);

        var handler = new GetTeamPostByPostIdQueryHandler(_repoMock.Object);

        await handler.Invoking(h => h.Handle(
                         new GetTeamPostByPostIdQuery { PostId = "missing" }, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetByPostId_WhenPostIsDeleted_ThrowsDomainException()
    {
        var post = CreatePost();
        post.MarkAsDeleted();
        _repoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct)).ReturnsAsync(post);

        var handler = new GetTeamPostByPostIdQueryHandler(_repoMock.Object);

        await handler.Invoking(h => h.Handle(
                         new GetTeamPostByPostIdQuery { PostId = "post-1" }, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    // ── GetTeamPostsByTeamQueryHandler ────────────────────────────────────────

    [Fact]
    public async Task GetByTeam_ReturnsPostsFromRepository()
    {
        var posts = new[] { CreatePost(postId: "p1"), CreatePost(postId: "p2") };
        _repoMock.Setup(r => r.GetByTeamIdAsync("team-1", _ct)).ReturnsAsync(posts);

        var handler = new GetTeamPostsByTeamQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetTeamPostsByTeamQuery { TeamId = "team-1" }, _ct);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.TeamId == "team-1");
    }

    [Fact]
    public async Task GetByTeam_WhenNoPosts_ReturnsEmptyCollection()
    {
        _repoMock.Setup(r => r.GetByTeamIdAsync("team-1", _ct)).ReturnsAsync([]);

        var handler = new GetTeamPostsByTeamQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetTeamPostsByTeamQuery { TeamId = "team-1" }, _ct);

        result.Should().BeEmpty();
    }

    // ── GetTeamPostsByTeamPagedQueryHandler ───────────────────────────────────

    [Fact]
    public async Task GetByTeamPaged_ReturnsPagedListFromRepository()
    {
        var post = CreatePost();
        var paged = new PagedList<Domain.Entities.TeamPost>([post], 1, 1, 10);
        _repoMock.Setup(r => r.GetByTeamPagedAsync("team-1", 1, 10, _ct))
                 .ReturnsAsync(paged);

        var handler = new GetTeamPostsByTeamPagedQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetTeamPostsByTeamPagedQuery { TeamId = "team-1", PageNumber = 1, PageSize = 10 },
            _ct);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByTeamPaged_PassesCorrectPaginationParameters()
    {
        var paged = new PagedList<Domain.Entities.TeamPost>([], 0, 2, 5);
        _repoMock.Setup(r => r.GetByTeamPagedAsync("team-1", 2, 5, _ct))
                 .ReturnsAsync(paged);

        var handler = new GetTeamPostsByTeamPagedQueryHandler(_repoMock.Object);
        await handler.Handle(
            new GetTeamPostsByTeamPagedQuery { TeamId = "team-1", PageNumber = 2, PageSize = 5 },
            _ct);

        _repoMock.Verify(r => r.GetByTeamPagedAsync("team-1", 2, 5, _ct), Times.Once);
    }

    // ── GetTeamPostsByAuthorQueryHandler ──────────────────────────────────────

    [Fact]
    public async Task GetByAuthor_ReturnsPostsFromRepository()
    {
        var posts = new[]
        {
            CreatePost(postId: "p1", authorId: "user-1"),
            CreatePost(postId: "p2", authorId: "user-1")
        };
        _repoMock.Setup(r => r.GetByAuthorIdAsync("user-1", _ct)).ReturnsAsync(posts);

        var handler = new GetTeamPostsByAuthorQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetTeamPostsByAuthorQuery { UserId = "user-1" }, _ct);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Author.UserId == "user-1");
    }

    [Fact]
    public async Task GetByAuthor_WhenNoPosts_ReturnsEmptyCollection()
    {
        _repoMock.Setup(r => r.GetByAuthorIdAsync("user-1", _ct)).ReturnsAsync([]);

        var handler = new GetTeamPostsByAuthorQueryHandler(_repoMock.Object);
        var result = await handler.Handle(
            new GetTeamPostsByAuthorQuery { UserId = "user-1" }, _ct);

        result.Should().BeEmpty();
    }
}