using ContentBLL.DTO.Post;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;
 
    public PostController(IPostService postService)
    {
        _postService = postService;
    }
 
    // GET: api/post
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetAll(CancellationToken ct)
    {
        var posts = await _postService.GetAllAsync(ct);
        return Ok(posts);
    }
 
    // GET: api/post/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostDto>> GetById(int id, CancellationToken ct)
    {
        var post = await _postService.GetByIdAsync(id, ct);
        if (post is null)
            return NotFound(new ProblemDetails { Title = $"Пост з id={id} не знайдено." });
 
        return Ok(post);
    }
 
    // GET: api/post/by-author/{postAuthorId}
    [HttpGet("by-author/{postAuthorId:int}")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetByAuthor(int postAuthorId, CancellationToken ct)
    {
        var posts = await _postService.GetByAuthorAsync(postAuthorId, ct);
        return Ok(posts);
    }
 
    // GET: api/post/by-collaboration/{collaborationSnapshotId}
    [HttpGet("by-collaboration/{collaborationSnapshotId:int}")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetByCollaboration(int collaborationSnapshotId, CancellationToken ct)
    {
        var posts = await _postService.GetByCollaborationAsync(collaborationSnapshotId, ct);
        return Ok(posts);
    }
 
    // GET: api/post/by-tag/{tagId}
    [HttpGet("by-tag/{tagId:int}")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetByTag(int tagId, CancellationToken ct)
    {
        var posts = await _postService.GetByTagAsync(tagId, ct);
        return Ok(posts);
    }
 
    // POST: api/post
    [HttpPost]
    public async Task<ActionResult<PostDto>> Create([FromBody] PostCreateDto dto, CancellationToken ct)
    {
        try
        {
            var post = await _postService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = post.PostId }, post);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
    }
 
    // PUT: api/post/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PostDto>> Update(int id, [FromBody] PostUpdateDto dto, CancellationToken ct)
    {
        try
        {
            var updated = await _postService.UpdateAsync(id, dto, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
    }
 
    // DELETE: api/post/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _postService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
    }
 
    // PATCH: api/post/{id}/archive
    [HttpPatch("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id, CancellationToken ct)
    {
        try
        {
            await _postService.ArchiveAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
    }
}