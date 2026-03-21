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
 
    // GET: api/like/count?postId=1
    [HttpGet("count")]
    public async Task<ActionResult<int>> Count([FromQuery] int postId, CancellationToken ct)
    {
        var count = await _likeService.CountByPostAsync(postId, ct);
        return Ok(count);
    }
 
    // GET: api/like/exists?postId=1&userId=2
    [HttpGet("exists")]
    public async Task<ActionResult<bool>> Exists([FromQuery] int postId, [FromQuery] int userId, CancellationToken ct)
    {
        var exists = await _likeService.ExistsAsync(postId, userId, ct);
        return Ok(exists);
    }
 
    // POST: api/like
    [HttpPost]
    public async Task<ActionResult<LikeDto>> Add([FromBody] LikeCreateDto dto, CancellationToken ct)
    {
        try
        {
            var like = await _likeService.AddAsync(dto, ct);
            return Ok(like);
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
 
    // DELETE: api/like?postId=1&userId=2
    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery] int postId, [FromQuery] int userId, CancellationToken ct)
    {
        try
        {
            await _likeService.RemoveAsync(postId, userId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message });
        }
    }
}