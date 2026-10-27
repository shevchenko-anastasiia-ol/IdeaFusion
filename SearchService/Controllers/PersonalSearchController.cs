using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchService.Indexing;
using SearchService.Personal;
using ServiceDefaults.Extensions;

namespace SearchService.Controllers;

/// <summary>The caller's own search history, saved searches and subscription notifications.</summary>
[ApiController]
[Route("api/search")]
[Authorize]
public sealed class PersonalSearchController(PersonalSearchStore store, IndexSyncTrigger sync) : ControllerBase
{
    /// <summary>Recent searches, newest first; <paramref name="q"/> narrows them to queries starting with it.</summary>
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<HistoryEntry>>> History([FromQuery] string? q, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (Unavailable() is { } error) return error;
        return Ok(await store.HistoryAsync(UserId, q, limit, ct));
    }

    /// <summary>Clears the whole history.</summary>
    [HttpDelete("history")]
    public async Task<IActionResult> ClearHistory(CancellationToken ct)
    {
        if (Unavailable() is { } error) return error;
        await store.DeleteHistoryAsync(UserId, null, ct);
        return NoContent();
    }

    [HttpDelete("history/{id:long}")]
    public async Task<IActionResult> DeleteHistoryEntry(long id, CancellationToken ct)
    {
        if (Unavailable() is { } error) return error;
        return await store.DeleteHistoryAsync(UserId, id, ct) > 0 ? NoContent() : NotFound();
    }

    [HttpGet("saved")]
    public async Task<ActionResult<IReadOnlyList<SavedSearch>>> Saved(CancellationToken ct)
    {
        if (Unavailable() is { } error) return error;
        return Ok(await store.SavedAsync(UserId, ct));
    }

    [HttpGet("saved/{id:guid}")]
    public async Task<ActionResult<SavedSearch>> GetSaved(Guid id, CancellationToken ct)
    {
        if (Unavailable() is { } error) return error;
        return await store.GetSavedAsync(UserId, id, ct) is { } saved ? saved : NotFound();
    }

    /// <summary>Saves a search. With notify the owner gets a notification for every new document it finds.</summary>
    [HttpPost("saved")]
    public Task<ActionResult<SavedSearch>> CreateSaved([FromBody] SavedSearchInput input, CancellationToken ct) => Save(null, input, ct);

    [HttpPut("saved/{id:guid}")]
    public Task<ActionResult<SavedSearch>> UpdateSaved(Guid id, [FromBody] SavedSearchInput input, CancellationToken ct) => Save(id, input, ct);

    [HttpDelete("saved/{id:guid}")]
    public async Task<IActionResult> DeleteSaved(Guid id, CancellationToken ct)
    {
        if (Unavailable() is { } error) return error;
        return await store.DeleteSavedAsync(UserId, id, ct) ? NoContent() : NotFound();
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<IReadOnlyList<SearchNotification>>> Notifications(
        [FromQuery] bool unreadOnly = false, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        if (Unavailable() is { } error) return error;
        return Ok(await store.NotificationsAsync(UserId, unreadOnly, limit, ct));
    }

    /// <summary>Marks notifications as read: the given ids, or all of them when the body has none.</summary>
    [HttpPost("notifications/read")]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest? request, CancellationToken ct)
    {
        if (Unavailable() is { } error) return error;
        var marked = await store.MarkReadAsync(UserId, request?.Ids ?? [], ct);
        return Ok(new { marked });
    }

    public sealed record MarkReadRequest(long[]? Ids);

    private async Task<ActionResult<SavedSearch>> Save(Guid? id, SavedSearchInput input, CancellationToken ct)
    {
        if (Unavailable() is { } error) return error;
        var name = input.Name?.Trim() ?? "";
        if (name.Length == 0 || name.Length > PersonalSearchStore.MaxNameLength)
            return BadRequest(new { error = $"name must be 1-{PersonalSearchStore.MaxNameLength} characters" });
        if (input.Filters?.Kind is { } kind && !DocumentKinds.All.Contains(kind.Trim().ToLowerInvariant()))
            return BadRequest(new { error = $"kind must be one of: {string.Join(", ", DocumentKinds.All)}" });
        if (input.Query is { Length: > 200 })
            return BadRequest(new { error = "query is too long" });

        var (result, savedId) = await store.SaveAsync(UserId, id, input with { Name = name }, ct);
        return result switch
        {
            SaveResult.NotFound => NotFound(),
            SaveResult.DuplicateName => Conflict(new { error = "A saved search with this name already exists" }),
            SaveResult.LimitReached => Conflict(new { error = $"At most {PersonalSearchStore.SavedLimit} saved searches" }),
            _ when id is null => CreatedAtAction(nameof(GetSaved), new { id = savedId }, await store.GetSavedAsync(UserId, savedId, ct)),
            _ => (await store.GetSavedAsync(UserId, savedId, ct))!,
        };
    }

    private string UserId => User.TryGetUserId()?.ToString() ?? "";

    private ActionResult? Unavailable()
    {
        if (!sync.SchemaReady.IsCompleted)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The search index is starting" });
        return User.TryGetUserId() is null ? Forbid() : null;
    }
}
