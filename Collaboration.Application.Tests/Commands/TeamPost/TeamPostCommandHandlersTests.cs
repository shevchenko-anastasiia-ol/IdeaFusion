using Collaboration.Application.Commands.TeamPost;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.Commands.TeamPost;

public class TeamPostCommandHandlersTests
{
    private readonly Mock<ITeamPostRepository> _postRepoMock = new();
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private static Domain.Entities.Team CreateTeam(string id = "team-1")
    {
        var owner = new UserSnapshot("owner-id", "owner");
        var team = new Domain.Entities.Team("Test Team", "desc", "Design", ["design"], owner);
        typeof(Domain.Entities.Team)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(team, id);
        return team;
    }

    private static Domain.Entities.TeamPost CreateTeamPost(
        string postId = "post-1",
        string teamId = "team-1",
        string authorId = "user-1",
        string title = "My Title")
    {
        var author = new UserSnapshot(authorId, "alice");
        return new Domain.Entities.TeamPost(postId, teamId, author, title);
    }

    // ── CreateTeamPostCommandHandler ──────────────────────────────────────────

    [Fact]
    public async Task CreatePost_WhenAllConditionsMet_CreatesAndReturnsPost()
    {
        var team = CreateTeam();
        team.AddMember(new UserSnapshot("user-1", "alice"), "Member");
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "user-1", _ct)).ReturnsAsync(true);
        _postRepoMock.Setup(r => r.ExistsByPostIdAsync("post-1", "team-1", _ct)).ReturnsAsync(false);
        _postRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Domain.Entities.TeamPost>(), _ct))
            .Returns<Domain.Entities.TeamPost, CancellationToken>((p, _) => Task.FromResult(p));

        var handler = new CreateTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateTeamPostCommand
        {
            PostId = "post-1",
            TeamId = "team-1",
            AuthorUserId = "user-1",
            AuthorUsername = "alice",
            Title = "My Title"
        };

        var result = await handler.Handle(command, _ct);

        result.PostId.Should().Be("post-1");
        result.TeamId.Should().Be("team-1");
        result.Title.Should().Be("My Title");
        result.Author.UserId.Should().Be("user-1");
        _postRepoMock.Verify(r => r.CreateAsync(It.IsAny<Domain.Entities.TeamPost>(), _ct), Times.Once);
    }

    [Fact]
    public async Task CreatePost_WhenTeamNotFound_ThrowsDomainException()
    {
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct))
                     .ReturnsAsync((Domain.Entities.Team?)null);

        var handler = new CreateTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", AuthorUserId = "user-1",
            AuthorUsername = "alice", Title = "Title"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreatePost_WhenTeamIsDeleted_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.MarkAsDeleted();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);

        var handler = new CreateTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", AuthorUserId = "user-1",
            AuthorUsername = "alice", Title = "Title"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*deleted*");
    }

    [Fact]
    public async Task CreatePost_WhenUserNotMember_ThrowsDomainException()
    {
        var team = CreateTeam();
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "outsider", _ct)).ReturnsAsync(false);

        var handler = new CreateTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", AuthorUserId = "outsider",
            AuthorUsername = "bob", Title = "Title"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*team members*");
    }

    [Fact]
    public async Task CreatePost_WhenPostAlreadyLinked_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.AddMember(new UserSnapshot("user-1", "alice"), "Member");
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "user-1", _ct)).ReturnsAsync(true);
        _postRepoMock.Setup(r => r.ExistsByPostIdAsync("post-1", "team-1", _ct)).ReturnsAsync(true);

        var handler = new CreateTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", AuthorUserId = "user-1",
            AuthorUsername = "alice", Title = "Title"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*already linked*");
    }

    [Fact]
    public async Task CreatePost_WhenMemberHasNoPublishPermission_ThrowsDomainException()
    {
        var team = CreateTeam();
        team.AddMember(new UserSnapshot("user-1", "alice"), "Member");
        team.Members.Single(m => m.UserId == "user-1").Permissions = [Domain.Entities.TeamPermission.EditPosts];
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _teamRepoMock.Setup(r => r.IsUserMemberAsync("team-1", "user-1", _ct)).ReturnsAsync(true);

        var handler = new CreateTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", AuthorUserId = "user-1",
            AuthorUsername = "alice", Title = "Title"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*required permission*");
    }

    // ── UpdateTeamPostTitleCommandHandler ─────────────────────────────────────

    [Fact]
    public async Task UpdateTitle_WhenPostExistsAndBelongsToTeam_UpdatesAndReturnsPost()
    {
        var post = CreateTeamPost();
        _postRepoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct)).ReturnsAsync(post);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(CreateTeam());
        _postRepoMock.Setup(r => r.UpdateAsync(post, _ct)).ReturnsAsync(post);

        var handler = new UpdateTeamPostTitleCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "New Title", UserId = "user-1"
        };

        var result = await handler.Handle(command, _ct);

        result.Title.Should().Be("New Title");
        _postRepoMock.Verify(r => r.UpdateAsync(post, _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateTitle_WhenPostNotFound_ThrowsDomainException()
    {
        _postRepoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct))
                     .ReturnsAsync((Domain.Entities.TeamPost?)null);

        var handler = new UpdateTeamPostTitleCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "New Title", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateTitle_WhenPostBelongsToDifferentTeam_ThrowsDomainException()
    {
        var post = CreateTeamPost(teamId: "other-team");
        _postRepoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct)).ReturnsAsync(post);

        var handler = new UpdateTeamPostTitleCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "New Title", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*does not belong*");
    }

    [Fact]
    public async Task UpdateTitle_WhenPostIsDeleted_ThrowsDomainException()
    {
        var post = CreateTeamPost();
        post.MarkAsDeleted();
        _postRepoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct)).ReturnsAsync(post);

        var handler = new UpdateTeamPostTitleCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "New Title", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*deleted*");
    }

    // ── DeleteTeamPostCommandHandler ──────────────────────────────────────────

    [Fact]
    public async Task DeletePost_WhenAuthorDeletes_ReturnsUnitAndMarksDeleted()
    {
        var post = CreateTeamPost(authorId: "user-1");
        _postRepoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct)).ReturnsAsync(post);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(CreateTeam());
        _postRepoMock.Setup(r => r.UpdateAsync(post, _ct)).ReturnsAsync(post);

        var handler = new DeleteTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new DeleteTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", UserId = "user-1"
        };

        var result = await handler.Handle(command, _ct);

        result.Should().Be(Unit.Value);
        post.IsDeleted.Should().BeTrue();
        _postRepoMock.Verify(r => r.UpdateAsync(post, _ct), Times.Once);
    }

    [Fact]
    public async Task DeletePost_WhenMemberWithEditPermissionDeletes_ReturnsUnitAndMarksDeleted()
    {
        var post = CreateTeamPost(authorId: "author-id");
        var team = CreateTeam();
        team.AddMember(new UserSnapshot("member-id", "deleter"), "Moderator");
        team.Members.Single(m => m.UserId == "member-id").Permissions = [Domain.Entities.TeamPermission.EditPosts];
        _postRepoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct)).ReturnsAsync(post);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(team);
        _postRepoMock.Setup(r => r.UpdateAsync(post, _ct)).ReturnsAsync(post);

        var handler = new DeleteTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new DeleteTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", UserId = "member-id"
        };

        var result = await handler.Handle(command, _ct);

        result.Should().Be(Unit.Value);
        post.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePost_WhenPostNotFound_ThrowsDomainException()
    {
        _postRepoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct))
                     .ReturnsAsync((Domain.Entities.TeamPost?)null);

        var handler = new DeleteTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new DeleteTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeletePost_WhenPostBelongsToDifferentTeam_ThrowsDomainException()
    {
        var post = CreateTeamPost(teamId: "other-team");
        _postRepoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct)).ReturnsAsync(post);

        var handler = new DeleteTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new DeleteTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", UserId = "user-1"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*does not belong*");
    }

    [Fact]
    public async Task DeletePost_WhenNeitherMemberNorAuthor_ThrowsDomainException()
    {
        var post = CreateTeamPost(authorId: "author-id");
        _postRepoMock.Setup(r => r.GetByPostIdAsync("post-1", _ct)).ReturnsAsync(post);
        _teamRepoMock.Setup(r => r.GetByIdAsync("team-1", _ct)).ReturnsAsync(CreateTeam());

        var handler = new DeleteTeamPostCommandHandler(_postRepoMock.Object, _teamRepoMock.Object);
        var command = new DeleteTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", UserId = "outsider"
        };

        await handler.Invoking(h => h.Handle(command, _ct))
                     .Should().ThrowAsync<DomainException>()
                     .WithMessage("*edit permission*");
    }
}