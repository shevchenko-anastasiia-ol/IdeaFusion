using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostViewController : ControllerBase
{
    private readonly IPostViewService _postViewService;
 
    public PostViewController(IPostViewService postViewService)
    {
        _postViewService = postViewService;
    }
 
    // POST: api/postview/record?postId=1&userId=2
    [HttpPost("record")]
    public async Task<IActionResult> Record([FromQuery] int postId, [FromQuery] int? userId, CancellationToken ct)
    {
        await _postViewService.RecordAsync(postId, userId, ct);
        return NoContent();
    }
 
    // GET: api/postview/count?postId=1
    [HttpGet("count")]
    public async Task<ActionResult<int>> Count([FromQuery] int postId, CancellationToken ct)
    {
        var count = await _postViewService.CountByPostAsync(postId, ct);
        return Ok(count);
    }
}