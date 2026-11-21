using Collaboration.API.Controllers;
using Collaboration.API.Services;
using Collaboration.Application.Commands.TeamPost;
using Collaboration.Application.Queries.TeamPost;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Collaboration.Application.Tests.API;

public class TeamPostControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICollaborationAuditWriter> _auditMock = new();
    private readonly TeamPostController _controller;

    public TeamPostControllerTests()
    {
        _controller = new TeamPostController(_mediatorMock.Object, _auditMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static Domain.Entities.TeamPost CreatePost() =>
        new("post-1", "team-1", new UserSnapshot("author-1", "alice"), "My Title");

    // ── GET api/team-posts/{postId} ───────────────────────────────────────────

    [Fact]
    public async Task GetByPostId_WhenFound_ReturnsOkWithETag()
    {
        var post = CreatePost();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostByPostIdQuery>(), default))
                     .ReturnsAsync(post);

        var result = await _controller.GetByPostId("post-1", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(post);
        _controller.Response.Headers.Should().ContainKey("ETag");
    }

    [Fact]
    public async Task GetByPostId_WhenNull_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostByPostIdQuery>(), default))
                     .ReturnsAsync((Domain.Entities.TeamPost?)null);

        var result = await _controller.GetByPostId("missing", default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetByPostId_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostByPostIdQuery>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var result = await _controller.GetByPostId("missing", default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── GET api/team-posts/team/{teamId} ──────────────────────────────────────

    [Fact]
    public async Task GetByTeam_ReturnsOkWithPosts()
    {
        var posts = new List<Domain.Entities.TeamPost> { CreatePost() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostsByTeamQuery>(), default))
                     .ReturnsAsync(posts);

        var result = await _controller.GetByTeam("team-1", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(posts);
    }

    // ── GET api/team-posts/team/{teamId}/paged ────────────────────────────────

    [Fact]
    public async Task GetByTeamPaged_ReturnsOkWithPagedPosts()
    {
        var post = CreatePost();
        var paged = new PagedList<Domain.Entities.TeamPost>([post], 1, 1, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostsByTeamPagedQuery>(), default))
                     .ReturnsAsync(paged);

        var result = await _controller.GetByTeamPaged("team-1", 1, 10, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(paged);
    }

    // ── GET api/team-posts/author/{userId} ────────────────────────────────────

    [Fact]
    public async Task GetByAuthor_ReturnsOkWithPosts()
    {
        var posts = new List<Domain.Entities.TeamPost> { CreatePost() };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostsByAuthorQuery>(), default))
                     .ReturnsAsync(posts);

        var result = await _controller.GetByAuthor("author-1", default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(posts);
    }

    // ── POST api/team-posts ───────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var post = CreatePost();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateTeamPostCommand>(), default))
                     .ReturnsAsync(post);

        var command = new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "My Title"
        };
        var result = await _controller.Create(command, default);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetByPostId));
        created.Value.Should().Be(post);
    }

    [Fact]
    public async Task Create_WhenDomainException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateTeamPostCommand>(), default))
                     .ThrowsAsync(new DomainException("invalid"));

        var command = new CreateTeamPostCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "T"
        };
        var result = await _controller.Create(command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── PATCH api/team-posts/{postId}/title ───────────────────────────────────

    [Fact]
    public async Task UpdateTitle_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new UpdateTeamPostTitleCommand
        {
            PostId = "other-id", TeamId = "team-1", Title = "New Title"
        };

        var result = await _controller.UpdateTitle("post-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateTitle_WhenSuccess_ReturnsOkWithETag()
    {
        var post = CreatePost();
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTeamPostTitleCommand>(), default))
                     .ReturnsAsync(post);

        var command = new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "New Title"
        };
        var result = await _controller.UpdateTitle("post-1", command, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(post);
        _controller.Response.Headers.Should().ContainKey("ETag");
    }

    [Fact]
    public async Task UpdateTitle_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTeamPostTitleCommand>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var command = new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "New"
        };
        var result = await _controller.UpdateTitle("post-1", command, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── DELETE api/team-posts/{postId} ────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenIdMismatch_ReturnsBadRequest()
    {
        var command = new DeleteTeamPostCommand { PostId = "other-id", TeamId = "team-1" };

        var result = await _controller.Delete("post-1", command, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTeamPostCommand>(), default))
                     .ReturnsAsync(Unit.Value);

        var command = new DeleteTeamPostCommand { PostId = "post-1", TeamId = "team-1" };
        var result = await _controller.Delete("post-1", command, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotFoundException_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteTeamPostCommand>(), default))
                     .ThrowsAsync(new NotFoundException("not found"));

        var command = new DeleteTeamPostCommand { PostId = "post-1", TeamId = "team-1" };
        var result = await _controller.Delete("post-1", command, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── catch block coverage ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByTeam_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostsByTeamQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetByTeam("team-1", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByTeamPaged_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostsByTeamPagedQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetByTeamPaged("team-1", 1, 10, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByAuthor_WhenException_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostsByAuthorQuery>(), default))
                     .ThrowsAsync(new DomainException("error"));

        var result = await _controller.GetByAuthor("author-1", default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByPostId_WhenPostHasUpdatedAt_AddsETagFromUpdatedAt()
    {
        var post = CreatePost();
        post.MarkAsUpdated("author-1");
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeamPostByPostIdQuery>(), default))
                     .ReturnsAsync(post);

        var result = await _controller.GetByPostId("post-1", default);

        result.Should().BeOfType<OkObjectResult>();
        _controller.Response.Headers.Should().ContainKey("ETag");
    }

    [Fact]
    public async Task UpdateTitle_WhenPostHasUpdatedAt_AddsETagFromUpdatedAt()
    {
        var post = CreatePost();
        post.MarkAsUpdated("author-1");
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTeamPostTitleCommand>(), default))
                     .ReturnsAsync(post);

        var command = new UpdateTeamPostTitleCommand
        {
            PostId = "post-1", TeamId = "team-1", Title = "New Title"
        };
        var result = await _controller.UpdateTitle("post-1", command, default);

        result.Should().BeOfType<OkObjectResult>();
        _controller.Response.Headers.Should().ContainKey("ETag");
    }
}