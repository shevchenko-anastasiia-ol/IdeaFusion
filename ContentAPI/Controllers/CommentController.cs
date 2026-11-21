using ContentAPI.Services;
using ContentBLL.DTO.Comment;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.Security;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IContentAuditWriter _audit;

    public CommentController(ICommentService commentService, IContentAuditWriter audit)
    {
        _commentService = commentService;
        _audit = audit;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommentDto>> GetById(int id, CancellationToken ct)
    {
        var comment = await _commentService.GetByIdAsync(id, ct);
        if (comment is null)
            return NotFound(new ProblemDetails { Title = $"Коментар з id={id} не знайдено." });
        return Ok(comment);
    }

    [HttpGet("by-post/{postId:int}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetByPost(int postId, CancellationToken ct)
    {
        var comments = await _commentService.GetByPostIdAsync(postId, ct);
        return Ok(comments);
    }

    [HttpGet("{parentCommentId:int}/replies")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetReplies(int parentCommentId, CancellationToken ct)
    {
        var replies = await _commentService.GetRepliesAsync(parentCommentId, ct);
        return Ok(replies);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create([FromBody] CommentCreateDto dto, CancellationToken ct)
    {
        var comment = await _commentService.CreateAsync(dto, ct);

        // Letters to the post's author / the answered comment's author. Looked up here, while the
        // caller's token is at hand; sending happens in the background. Absent in unit tests.
        var mailer = HttpContext?.RequestServices?.GetService<ContentAPI.Notifications.ICommentMailer>();
        if (mailer is not null)
            await mailer.NotifyCreatedAsync(comment.CommentId, ct);

        return CreatedAtAction(nameof(GetById), new { id = comment.CommentId }, comment);
    }

    /// <summary>
    /// Edits a comment. The service rejects anybody who is not its author or a
    /// moderator.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CommentDto>> Update(int id, [FromBody] CommentUpdateDto dto, CancellationToken ct)
    {
        var updated = await _commentService.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _commentService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes a comment regardless of who wrote it. Moderators only, and always
    /// written to the audit trail.
    /// </summary>
    [HttpDelete("{id:int}/moderate")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModerateDelete(int id, CancellationToken ct)
    {
        // Fetch first so the audit entry can name the post and the author.
        var target = await _commentService.GetByIdAsync(id, ct);
        if (target is null)
            return NotFound(new ProblemDetails { Title = $"Коментар з id={id} не знайдено." });

        await _commentService.ModerateDeleteAsync(id, ct);

        await _audit.RecordAsync(
            action: "comment.moderate.delete",
            targetType: "comment",
            targetId: id.ToString(),
            targetName: Truncate(target.Body),
            details: $"postId={target.PostId}; authorPostAuthorId={target.Author?.PostAuthorId.ToString() ?? "none"}",
            ct: ct);

        return NoContent();
    }

    /// <summary>The audit sink stores a short target name, and a comment can be long.</summary>
    private static string Truncate(string? body)
        => body is null ? string.Empty : body.Length <= 120 ? body : body[..120];
}
