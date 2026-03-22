using Collaboration.Application.Commands.TeamPost;
using Collaboration.Application.Queries.TeamPost;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Collaboration.API.Controllers;

[Route("api/team-posts")]
public class TeamPostController : BaseApiController
{
    public TeamPostController(IMediator mediator) : base(mediator) { }
 
    // GET: api/team-posts/{postId}
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
            return NoContent();
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}