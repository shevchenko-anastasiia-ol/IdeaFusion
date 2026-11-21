using ContentBLL.DTO.Post;
using ContentBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostAuthorController : ControllerBase
{
    private readonly IPostAuthorResolver _authorResolver;

    public PostAuthorController(IPostAuthorResolver authorResolver)
    {
        _authorResolver = authorResolver;
    }

    /// <summary>
    /// The content-side ids for whoever holds the token. This replaces the old
    /// by-username lookup, so a client can no longer ask for somebody else's ids.
    /// </summary>
    /// <remarks>
    /// The row is created here when the caller does not have one yet, which is
    /// harmless: the row is keyed off the validated subject, not off anything the
    /// client sent. <see cref="PostAuthorDto.Created"/> reports when that happened.
    /// </remarks>
    [HttpGet("me")]
    [ProducesResponseType(typeof(PostAuthorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PostAuthorDto>> GetMe(CancellationToken ct)
    {
        var resolved = await _authorResolver.ResolveForWriteAsync(ct: ct);
        return Ok(ToDto(resolved));
    }

    /// <summary>
    /// Refreshes the cached avatar for the caller. Scoped to the caller's own row,
    /// which is why the avatar is not an argument on the read endpoints any more.
    /// </summary>
    [HttpPatch("me/avatar")]
    [ProducesResponseType(typeof(PostAuthorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PostAuthorDto>> UpdateOwnAvatar(
        [FromQuery] string? avatarUrl,
        CancellationToken ct)
    {
        var resolved = await _authorResolver.ResolveForWriteAsync(avatarUrl, ct);
        return Ok(ToDto(resolved));
    }

    /// <summary>
    /// Looks an author up by display name. Read-only: it no longer creates a row,
    /// because a name is something a client can type and a subject is not.
    /// </summary>
    [HttpGet("by-username/{userName}")]
    [ProducesResponseType(typeof(PostAuthorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostAuthorDto>> GetByUserName(string userName, CancellationToken ct)
    {
        var resolved = await _authorResolver.FindByUserNameAsync(userName, ct);
        if (resolved is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = $"Автор з іменем '{userName}' не знайдено.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(ToDto(resolved));
    }

    private static PostAuthorDto ToDto(ResolvedPostAuthor resolved) => new()
    {
        PostAuthorId = resolved.PostAuthorId,
        UserId = resolved.UserId,
        ExternalId = resolved.ExternalId == Guid.Empty ? null : resolved.ExternalId,
        UserName = resolved.UserName,
        AvatarUrl = resolved.AvatarUrl,
        Created = resolved.Created
    };
}
