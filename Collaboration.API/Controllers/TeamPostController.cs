using Collaboration.API.Services;
using Collaboration.Application.Commands.TeamPost;
using Collaboration.Application.Queries.TeamPost;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collaboration.API.Controllers;

[Route("api/team-posts")]
[Authorize]
public class TeamPostController : BaseApiController
{
    private readonly ICollaborationAuditWriter _audit;

    public TeamPostController(IMediator mediator, ICollaborationAuditWriter audit) : base(mediator)
    {
        _audit = audit;
    }
 
    // GET: api/team-posts/{postId}
    [AllowAnonymous]
    [HttpGet("{postId}")]
    public async Task<IActionResult> GetByPostId(string postId, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _mediator.Send(new GetTeamPostByPostIdQuery { PostId = postId }, cancellationToken);
            if (post is null) return NotFound();
 
            AddETagHeader(GenerateETag(post.UpdatedAt ?? post.CreatedAt));
            return Ok(post);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team-posts/team/{teamId}
    [AllowAnonymous]
    [HttpGet("team/{teamId}")]
    public async Task<IActionResult> GetByTeam(string teamId, CancellationToken cancellationToken)
    {
        try
        {
            var posts = await _mediator.Send(new GetTeamPostsByTeamQuery { TeamId = teamId }, cancellationToken);
            return Ok(posts);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team-posts/team/{teamId}/paged
    [AllowAnonymous]
    [HttpGet("team/{teamId}/paged")]
    public async Task<IActionResult> GetByTeamPaged(string teamId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var posts = await _mediator.Send(new GetTeamPostsByTeamPagedQuery
            {
                TeamId = teamId,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);
            return Ok(posts);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team-posts/author/{userId}
    // A public byline, like a profile page: the userId here is the subject of the
    // listing, not the caller, so it is not one of the removed identity parameters.
    [AllowAnonymous]
    [HttpGet("author/{userId}")]
    public async Task<IActionResult> GetByAuthor(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var posts = await _mediator.Send(new GetTeamPostsByAuthorQuery { UserId = userId }, cancellationToken);
            return Ok(posts);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // POST: api/team-posts
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamPostCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetByPostId), new { postId = post.PostId }, post);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PATCH: api/team-posts/{postId}/title
    [HttpPatch("{postId}/title")]
    public async Task<IActionResult> UpdateTitle(string postId, [FromBody] UpdateTeamPostTitleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.PostId != postId)
                return BadRequest(new { message = "ID mismatch." });
 
            var post = await _mediator.Send(command, cancellationToken);
            AddETagHeader(GenerateETag(post.UpdatedAt ?? post.CreatedAt));
            return Ok(post);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // DELETE: api/team-posts/{postId}
    [HttpDelete("{postId}")]
    public async Task<IActionResult> Delete(string postId, [FromBody] DeleteTeamPostCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.PostId != postId)
                return BadRequest(new { message = "ID mismatch." });
 
            await _mediator.Send(command, cancellationToken);

            await _audit.RecordAsync(
                action: "DeleteTeamPost",
                targetType: "TeamPost",
                targetId: postId,
                details: $"Post {postId} unlinked from team {command.TeamId}.",
                category: CollaborationAuditCategories.Content,
                cancellationToken: cancellationToken);

            return NoContent();
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}