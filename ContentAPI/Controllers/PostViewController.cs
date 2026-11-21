using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostViewController : ControllerBase
{
    private readonly IPostViewService _postViewService;
 
    public PostViewController(IPostViewService postViewService)
    {
        _postViewService = postViewService;
    }
 
    // POST: api/postview/record?postId=1 — the viewer is taken from the token
    [HttpPost("record")]
    public async Task<IActionResult> Record([FromQuery] int postId, CancellationToken ct)
    {
        await _postViewService.RecordAsync(postId, ct);
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
