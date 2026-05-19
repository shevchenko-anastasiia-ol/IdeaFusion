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
 
    [HttpGet("count")]
    public async Task<ActionResult<int>> Count([FromQuery] int postId, CancellationToken ct)
    {
        var count = await _savedPostService.CountByPostAsync(postId, ct);
        return Ok(count);
    }

    [HttpGet("by-user/{userId:int}")]
    public async Task<ActionResult<IEnumerable<SavedPostDto>>> GetByUser(int userId, CancellationToken ct)
    {
        var saved = await _savedPostService.GetByUserIdAsync(userId, ct);
        return Ok(saved);
    }
 
    [HttpGet("exists")]
    public async Task<ActionResult<bool>> Exists([FromQuery] int postId, [FromQuery] int userId, CancellationToken ct)
    {
        var exists = await _savedPostService.ExistsAsync(postId, userId, ct);
        return Ok(exists);
    }
 
    [HttpPost]
    public async Task<ActionResult<SavedPostDto>> Save([FromBody] SavedPostCreateDto dto, CancellationToken ct)
    {
        var saved = await _savedPostService.SaveAsync(dto, ct);
        return Ok(saved);
    }
 
    [HttpDelete]
    public async Task<IActionResult> Unsave([FromQuery] int postId, [FromQuery] int userId, CancellationToken ct)
    {
        await _savedPostService.UnsaveAsync(postId, userId, ct);
        return NoContent();
    }
}