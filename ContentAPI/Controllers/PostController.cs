using ContentAPI.Services;
using ContentBLL.DTO.Post;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.Security;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IContentAuditWriter _audit;

    public PostController(IPostService postService, IContentAuditWriter audit)
    {
        _postService = postService;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetAll(CancellationToken ct)
    {
        var posts = await _postService.GetAllAsync(ct);
        return Ok(posts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostDto>> GetById(int id, CancellationToken ct)
    {
        var post = await _postService.GetByIdAsync(id, ct);
        if (post is null)
            return NotFound(new ProblemDetails { Title = $"Пост з id={id} не знайдено." });
        return Ok(post);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PostDto>>> Search([FromQuery] string q, CancellationToken ct)
    {
        var posts = await _postService.SearchAsync(q, ct);
        return Ok(posts);
    }

    [HttpGet("by-author/{postAuthorId:int}")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetByAuthor(int postAuthorId, CancellationToken ct)
    {
        var posts = await _postService.GetByAuthorAsync(postAuthorId, ct);
        return Ok(posts);
    }

    [HttpGet("by-userid/{userId:int}")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetByUserId(int userId, CancellationToken ct)
    {
        var posts = await _postService.GetByUserIdAsync(userId, ct);
        return Ok(posts);
    }

    [HttpGet("by-collaboration/{collaborationSnapshotId:int}")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetByCollaboration(int collaborationSnapshotId, CancellationToken ct)
    {
        var posts = await _postService.GetByCollaborationAsync(collaborationSnapshotId, ct);
        return Ok(posts);
    }

    [HttpGet("by-tag/{tagId:int}")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetByTag(int tagId, CancellationToken ct)
    {
        var posts = await _postService.GetByTagAsync(tagId, ct);
        return Ok(posts);
    }

    [HttpPost]
    public async Task<ActionResult<PostDto>> Create([FromForm] PostCreateDto dto, CancellationToken ct)
    {
        Console.WriteLine($"Create post requested: {dto.Title}");
        var post = await _postService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = post.PostId }, post);
    }

    /// <summary>
    /// Edits a post. The author comes from the token and the service rejects anybody
    /// who is not the author or a moderator.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PostDto>> Update(int id, [FromForm] PostUpdateDto dto, CancellationToken ct)
    {
        var updated = await _postService.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _postService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/archive")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Archive(int id, CancellationToken ct)
    {
        await _postService.ArchiveAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes a post regardless of who wrote it. Moderators only, and always
    /// written to the audit trail.
    /// </summary>
    [HttpDelete("{id:int}/moderate")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModerateDelete(int id, CancellationToken ct)
    {
        // Read the target before removing it: once it is soft-deleted the title is
        // gone, and the audit entry is much less useful without it.
        var target = await _postService.GetByIdAsync(id, ct);
        if (target is null)
            return NotFound(new ProblemDetails { Title = $"Пост з id={id} не знайдено." });

        await _postService.ModerateDeleteAsync(id, ct);

        await _audit.RecordAsync(
            action: "post.moderate.delete",
            targetType: "post",
            targetId: id.ToString(),
            targetName: target.Title,
            details: $"authorPostAuthorId={target.Author?.PostAuthorId.ToString() ?? "none"}",
            ct: ct);

        return NoContent();
    }
}
