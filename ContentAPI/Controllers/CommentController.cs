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
        return CreatedAtAction(nameof(GetById), new { id = comment.CommentId }, comment);
    }
 
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CommentDto>> Update(int id, [FromBody] CommentUpdateDto dto, CancellationToken ct)
    {
        var updated = await _commentService.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }
 
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _commentService.DeleteAsync(id, ct);
        return NoContent();
    }
}