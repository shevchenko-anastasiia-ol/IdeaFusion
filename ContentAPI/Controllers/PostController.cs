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
        var post = await _postService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = post.PostId }, post);
    }
 
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PostDto>> Update(int id, [FromForm] PostUpdateDto dto, CancellationToken ct)
    {
        var updated = await _postService.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }
 
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _postService.DeleteAsync(id, ct);
        return NoContent();
    }
 
    [HttpPatch("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id, CancellationToken ct)
    {
        await _postService.ArchiveAsync(id, ct);
        return NoContent();
    }
}