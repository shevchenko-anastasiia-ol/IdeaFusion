using ContentBLL.DTO.Comment;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;
 
    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }
 
    // GET: api/comment/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommentDto>> GetById(int id, CancellationToken ct)
    {
        var comment = await _commentService.GetByIdAsync(id, ct);
        if (comment is null)
            return NotFound(new ProblemDetails { Title = $"Коментар з id={id} не знайдено." });
 
        return Ok(comment);
    }
 
    // GET: api/comment/by-post/{postId}
    [HttpGet("by-post/{postId:int}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetByPost(int postId, CancellationToken ct)
    {
        var comments = await _commentService.GetByPostIdAsync(postId, ct);
        return Ok(comments);
    }
 
    // GET: api/comment/{parentCommentId}/replies
    [HttpGet("{parentCommentId:int}/replies")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetReplies(int parentCommentId, CancellationToken ct)
    {
        var replies = await _commentService.GetRepliesAsync(parentCommentId, ct);
        return Ok(replies);
    }
 
    // POST: api/comment
    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create([FromBody] CommentCreateDto dto, CancellationToken ct)
    {
        try
        {
            var comment = await _commentService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = comment.CommentId }, comment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message });
        }
    }
 
    // PUT: api/comment/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CommentDto>> Update(int id, [FromBody] CommentUpdateDto dto, CancellationToken ct)
    {
        try
        {
            var updated = await _commentService.UpdateAsync(id, dto, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
    }
 
    // DELETE: api/comment/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _commentService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
    }
}