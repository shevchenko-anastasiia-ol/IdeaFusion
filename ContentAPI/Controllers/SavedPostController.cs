using ContentBLL.DTO.SavedPost;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SavedPostController : ControllerBase
{
    private readonly ISavedPostService _savedPostService;
 
    public SavedPostController(ISavedPostService savedPostService)
    {
        _savedPostService = savedPostService;
    }
 
    // GET: api/savedpost/by-user/{userId}
    [HttpGet("by-user/{userId:int}")]
    public async Task<ActionResult<IEnumerable<SavedPostDto>>> GetByUser(int userId, CancellationToken ct)
    {
        var saved = await _savedPostService.GetByUserIdAsync(userId, ct);
        return Ok(saved);
    }
 
    // GET: api/savedpost/exists?postId=1&userId=2
    [HttpGet("exists")]
    public async Task<ActionResult<bool>> Exists([FromQuery] int postId, [FromQuery] int userId, CancellationToken ct)
    {
        var exists = await _savedPostService.ExistsAsync(postId, userId, ct);
        return Ok(exists);
    }
 
    // POST: api/savedpost
    [HttpPost]
    public async Task<ActionResult<SavedPostDto>> Save([FromBody] SavedPostCreateDto dto, CancellationToken ct)
    {
        try
        {
            var saved = await _savedPostService.SaveAsync(dto, ct);
            return Ok(saved);
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
 
    // DELETE: api/savedpost?postId=1&userId=2
    [HttpDelete]
    public async Task<IActionResult> Unsave([FromQuery] int postId, [FromQuery] int userId, CancellationToken ct)
    {
        try
        {
            await _savedPostService.UnsaveAsync(postId, userId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
    }
}