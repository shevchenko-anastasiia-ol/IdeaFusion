using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostAuthorController : ControllerBase
{
    private readonly IPostService _postService;

    public PostAuthorController(IPostService postService)
    {
        _postService = postService;
    }

    /// <summary>
    /// Returns the content-side integer userId for the given username,
    /// creating a PostAuthor record if one does not yet exist.
    /// </summary>
    [HttpGet("by-username/{userName}")]
    public async Task<ActionResult<int>> GetByUserName(
        string userName,
        [FromQuery] string? avatarUrl,
        CancellationToken ct)
    {
        var userId = await _postService.EnsurePostAuthorAsync(userName, avatarUrl, ct);
        return Ok(userId);
    }
}