using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchService.Feed;
using SearchService.Indexing;
using ServiceDefaults.Extensions;
using ServiceDefaults.Security;

namespace SearchService.Controllers;

/// <summary>The personal "For you" feed.</summary>
[ApiController]
[Route("api/search/feed")]
[Authorize]
public sealed class FeedController(FeedService feed, FeedEventLog events, IndexSyncTrigger sync) : ControllerBase
{
    /// <summary>Recommended posts with the reason for each. Pass asOf from the first page to the next ones.</summary>
    [HttpGet]
    public async Task<ActionResult<FeedResponse>> Get(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] DateTime? asOf = null, CancellationToken ct = default)
    {
        if (Unavailable() is { } error) return error;
        return await feed.FeedAsync(User.TryGetUserId()!.Value.ToString(), page, pageSize, asOf, ct);
    }

    /// <summary>Logs what the feed showed and what was opened.</summary>
    [HttpPost("events")]
    public async Task<IActionResult> Events([FromBody] FeedEventBatch batch, CancellationToken ct)
    {
        if (Unavailable() is { } error) return error;
        if (string.IsNullOrWhiteSpace(batch.Variant) || batch.Variant.Length > 40)
            return BadRequest(new { error = "variant is required" });
        if (batch.Events is not { Count: > 0 and <= FeedEventLog.MaxBatch })
            return BadRequest(new { error = $"1-{FeedEventLog.MaxBatch} events per request" });
        var written = await events.WriteAsync(User.TryGetUserId()!.Value.ToString(), batch, ct);
        return Ok(new { written });
    }

    /// <summary>Impressions, clicks and click rate per variant over the last <paramref name="days"/> days.</summary>
    [HttpGet("stats")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    public async Task<ActionResult<IReadOnlyList<FeedVariantStats>>> Stats([FromQuery] int days = 7, CancellationToken ct = default)
    {
        if (Unavailable() is { } error) return error;
        return Ok(await events.StatsAsync(TimeSpan.FromDays(Math.Clamp(days, 1, 90)), ct));
    }

    private ActionResult? Unavailable()
    {
        if (!sync.SchemaReady.IsCompleted)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The search index is starting" });
        return User.TryGetUserId() is null ? Forbid() : null;
    }
}
