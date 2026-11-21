using ContentBLL.DTO.SavedPost;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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

    /// <summary>
    /// The caller's own saved posts. Replaces the old by-user read, which let any
    /// caller list anybody else's.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<SavedPostDto>>> GetMine(CancellationToken ct)
    {
        var saved = await _savedPostService.GetMineAsync(ct);
        return Ok(saved);
    }

    /// <summary>Whether the caller has saved the post. Read from the token.</summary>
    [HttpGet("exists")]
    public async Task<ActionResult<bool>> Exists([FromQuery] int postId, CancellationToken ct)
    {
        var exists = await _savedPostService.ExistsAsync(postId, ct);
        return Ok(exists);
    }

    [HttpPost]
    public async Task<ActionResult<SavedPostDto>> Save([FromBody] SavedPostCreateDto dto, CancellationToken ct)
    {
        var saved = await _savedPostService.SaveAsync(dto, ct);
        return Ok(saved);
    }

    /// <summary>Removes the caller's own saved post. There is no userId to pass.</summary>
    [HttpDelete]
    public async Task<IActionResult> Unsave([FromQuery] int postId, CancellationToken ct)
    {
        await _savedPostService.UnsaveAsync(postId, ct);
        return NoContent();
    }
}
