using ContentAPI.CopyDetection;
using ContentDAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDefaults.Interfaces;
using ServiceDefaults.Security;

namespace ContentAPI.Controllers;

/// <summary>
/// Image copy detection for authors (a check before publishing) and moderators (the queue of
/// possible copies). Results are advice, never sanctions: the author sees what their image
/// resembles and why, the moderator decides.
/// </summary>
[ApiController]
[Route("api/copycheck")]
[Authorize]
public class CopyCheckController : ControllerBase
{
    private readonly IImageCopyClient _client;
    private readonly IAuthorshipFilter _authorship;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ContentDbContext _db;
    private readonly ILogger<CopyCheckController> _logger;
    private readonly CopyCheckQueue? _queue;

    public CopyCheckController(
        IImageCopyClient client,
        IAuthorshipFilter authorship,
        ICurrentUserAccessor currentUser,
        ContentDbContext db,
        ILogger<CopyCheckController> logger,
        CopyCheckQueue? queue = null)
    {
        _client = client;
        _authorship = authorship;
        _currentUser = currentUser;
        _db = db;
        _logger = logger;
        _queue = queue;
    }

    /// <summary>
    /// Checks and indexes every published post, oldest first, in the background. Needed once
    /// after the copy detection is switched on (older posts are not in the index yet) and after
    /// a new model version; safe to repeat.
    /// </summary>
    [HttpPost("reindex")]
    [Authorize(Policy = IdeaFusionPolicies.RequireAdministrator)]
    public IActionResult Reindex()
    {
        if (_queue is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails { Title = "Перевірку на копії вимкнено в конфігурації." });
        if (!_queue.TryEnqueue(new CopyJob(CopyJobKind.ReindexAll, 0)))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails { Title = "Черга перевірок переповнена, спробуйте пізніше." });

        _logger.LogInformation("Copy index rebuild requested by {User}", _currentUser.UserName);
        return Accepted(new { status = "queued" });
    }

    /// <summary>
    /// Checks an image before it is published. The author's own works and those of the team
    /// the post is for are excluded. Returns the best match, its zone and the matched area.
    /// </summary>
    [HttpPost("preview")]
    [Authorize(Policy = IdeaFusionPolicies.RequireAuthor)]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<CopyCheckResult>> Preview(IFormFile file, [FromForm] int? collaborationSnapshotId, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Файл не передано." });
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Title = "Перевіряються лише зображення." });

        var uploader = _currentUser.UserId?.ToString() ?? "";
        var excludeUsers = new[] { uploader, $"username:{_currentUser.UserName}" };
        var excludeTeams = new List<string>();
        if (collaborationSnapshotId is int snapshotId)
        {
            var snapshot = await _db.CollaborationSnapshots.AsNoTracking()
                .FirstOrDefaultAsync(s => s.CollaborationSnapshotId == snapshotId, ct);
            if (snapshot is not null)
                excludeTeams.Add(snapshot.ExternalId ?? $"collaboration:{snapshot.CollaborationId}");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _client.CheckFileAsync(stream, file.FileName, file.ContentType, excludeUsers, excludeTeams, ct);
            return Ok(await _authorship.ExcludeOwnWorksAsync(result, uploader, ct));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // The check is advisory: when the ML service is down, publishing still works.
            _logger.LogWarning(ex, "Copy detection service unavailable for a preview check");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails { Title = "Перевірка на копії тимчасово недоступна." });
        }
    }

    /// <summary>Moderation queue: possible copies found among published posts.</summary>
    [HttpGet("moderation")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    public async Task<ActionResult<IReadOnlyList<CopyCheckRecord>>> Queue(
        [FromQuery] string[]? zone, [FromQuery] string? status = "open",
        [FromQuery] int limit = 50, [FromQuery] int offset = 0, CancellationToken ct = default)
    {
        var zones = zone is { Length: > 0 } ? zone : new[] { CopyZones.Red, CopyZones.Yellow };
        return Ok(await _client.ListChecksAsync(zones, status, Math.Clamp(limit, 1, 500), Math.Max(offset, 0), ct));
    }

    [HttpGet("moderation/{id:int}")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    public async Task<ActionResult<CopyCheckRecord>> Get(int id, CancellationToken ct)
    {
        var check = await _client.GetCheckAsync(id, ct);
        return check is null ? NotFound() : Ok(check);
    }

    /// <summary>Side-by-side picture: the upload, the matched work and the matched area.</summary>
    [HttpGet("moderation/{id:int}/explanation")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    public async Task<IActionResult> Explanation(int id, CancellationToken ct)
    {
        var png = await _client.GetExplanationAsync(id, ct);
        return png is null ? NotFound() : File(png, "image/png");
    }

    public sealed record DecisionBody(string Decision);

    /// <summary>"confirmed_copy" (the moderator then acts on the post as usual), "dismissed" or "open".</summary>
    [HttpPost("moderation/{id:int}/decision")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    public async Task<ActionResult<CopyCheckRecord>> Decide(int id, [FromBody] DecisionBody body, CancellationToken ct)
    {
        if (body.Decision is not ("confirmed_copy" or "dismissed" or "open"))
            return BadRequest(new ProblemDetails { Title = "Рішення: confirmed_copy, dismissed або open." });

        var moderator = _currentUser.UserName ?? _currentUser.UserId?.ToString() ?? "moderator";
        var check = await _client.DecideAsync(id, new CopyCheckDecision { Decision = body.Decision, Moderator = moderator }, ct);
        return check is null ? NotFound() : Ok(check);
    }
}
