using ContentBLL.DTO.Like;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LikeController : ControllerBase
{
    private readonly ILikeService _likeService;

    public LikeController(ILikeService likeService)
    {
        _likeService = likeService;
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> Count([FromQuery] int postId, CancellationToken ct)
    {
        var count = await _likeService.CountByPostAsync(postId, ct);
        return Ok(count);
    }

    /// <summary>Whether the caller has liked the post. Read from the token.</summary>
    [HttpGet("exists")]
    public async Task<ActionResult<bool>> Exists([FromQuery] int postId, CancellationToken ct)
    {
        var exists = await _likeService.ExistsAsync(postId, ct);
        return Ok(exists);
    }

    [HttpPost]
    public async Task<ActionResult<LikeDto>> Add([FromBody] LikeCreateDto dto, CancellationToken ct)
    {
        var like = await _likeService.AddAsync(dto, ct);
        return Ok(like);
    }

    /// <summary>Removes the caller's own like. There is no userId to pass.</summary>
    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery] int postId, CancellationToken ct)
    {
        await _likeService.RemoveAsync(postId, ct);
        return NoContent();
    }
}
