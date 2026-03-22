using ContentBLL.DTO.Like;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
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
 
    [HttpGet("exists")]
    public async Task<ActionResult<bool>> Exists([FromQuery] int postId, [FromQuery] int userId, CancellationToken ct)
    {
        var exists = await _likeService.ExistsAsync(postId, userId, ct);
        return Ok(exists);
    }
 
    [HttpPost]
    public async Task<ActionResult<LikeDto>> Add([FromBody] LikeCreateDto dto, CancellationToken ct)
    {
        var like = await _likeService.AddAsync(dto, ct);
        return Ok(like);
    }
 
    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery] int postId, [FromQuery] int userId, CancellationToken ct)
    {
        await _likeService.RemoveAsync(postId, userId, ct);
        return NoContent();
    }
}