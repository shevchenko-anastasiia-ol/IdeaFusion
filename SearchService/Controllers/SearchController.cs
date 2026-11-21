using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SearchService.Indexing;
using SearchService.Personal;
using SearchService.Search;
using ServiceDefaults.Extensions;
using ServiceDefaults.Security;

namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public sealed class SearchController(
    SearchQueryService search,
    IndexSyncTrigger sync,
    NpgsqlDataSource indexDb,
    PersonalSearchStore personal,
    SearchMetrics metrics,
    ILogger<SearchController> logger) : ControllerBase
{
    /// <summary>Search posts, teams, vacancies and people. Typos, the wrong keyboard layout and transliteration are tolerated.</summary>
    [HttpGet]
    public async Task<ActionResult<SearchResponse>> Search([FromQuery] SearchQueryParameters query, CancellationToken ct = default)
    {
        if (query.Kind is not null && !DocumentKinds.All.Contains(query.Kind.Trim().ToLowerInvariant()))
            return BadRequest(new { error = $"kind must be one of: {string.Join(", ", DocumentKinds.All)}" });
        if (query.From is { } from && query.To is { } to && from >= to)
            return BadRequest(new { error = "from must be earlier than to" });
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();

        // The searcher's own profile is never a result: not in the hits, not in the "people" count.
        var request = query.ToRequest() with { ViewerId = User.TryGetUserId()?.ToString() };
        var started = System.Diagnostics.Stopwatch.StartNew();
        SearchResponse response;
        try
        {
            response = await search.SearchAsync(request, ct);
        }
        catch (InvalidCursorException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        await RememberAsync(request, response, ct);
        await MeasureAsync(request, response, started.Elapsed, ct);
        return response;
    }

    /// <summary>Autocomplete: completions of the last word and the best few documents.</summary>
    [HttpGet("suggest")]
    public async Task<ActionResult<SuggestResponse>> Suggest([FromQuery] string? q, [FromQuery] int limit = 8, CancellationToken ct = default)
    {
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();
        return await search.SuggestAsync(q, limit, ct, User.TryGetUserId()?.ToString());
    }

    /// <summary>Tag autocomplete by any spelling or synonym; an empty q lists the most used tags.</summary>
    [HttpGet("tags")]
    public async Task<ActionResult<IReadOnlyList<TagSuggestion>>> Tags([FromQuery] string? q, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();
        return Ok(await search.TagsAsync(q, limit, ct));
    }

    /// <summary>Role autocomplete for the role filter, by any spelling or synonym; an empty q lists the most asked-for roles.</summary>
    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleSuggestion>>> Roles([FromQuery] string? q, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();
        return Ok(await search.RolesAsync(q, limit, ct));
    }

    /// <summary>Categories (derived from tags) with document counts, for the filter list.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryCount>>> Categories(CancellationToken ct)
    {
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();
        return Ok(await search.CategoriesAsync(ct));
    }

    /// <summary>State of the index per source.</summary>
    [HttpGet("status")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();
        await using var conn = await indexDb.OpenConnectionAsync(ct);
        var sources = await conn.QueryAsync(new CommandDefinition(
            "SELECT kind, synced_at AS \"syncedAt\", documents, last_error AS \"lastError\", failed_at AS \"failedAt\" FROM index_sources ORDER BY kind",
            cancellationToken: ct));
        var terms = await conn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT count(*)::int FROM search_terms", cancellationToken: ct));
        return Ok(new { sources, terms, lastSyncMs = sync.LastSyncDuration?.TotalMilliseconds });
    }

    /// <summary>Re-read all sources now instead of waiting for the next cycle.</summary>
    [HttpPost("reindex")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    public IActionResult Reindex()
    {
        sync.Request();
        return Accepted();
    }

    /// <summary>
    /// Adds a new search to the caller's history: the first page only, and only when something was asked for.
    /// History is a convenience, so a failure here never fails the search; the query itself is not logged.
    /// </summary>
    private async Task RememberAsync(SearchRequest request, SearchResponse response, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(request.Cursor) || request.Page > 1 || User.TryGetUserId() is not { } userId) return;
        var filters = SearchFilters.Of(request);
        if (string.IsNullOrWhiteSpace(request.Query) && filters.IsEmpty) return;
        try
        {
            await personal.RecordAsync(userId.ToString(), request.Query, filters, response.Total, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Search history could not be saved: {Error}", ex.GetType().Name);
        }
    }

    /// <summary>Counts a new search (not its further pages) for the quality metrics.</summary>
    private async Task MeasureAsync(SearchRequest request, SearchResponse response, TimeSpan duration, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(request.Cursor) || request.Page > 1) return;
        try
        {
            await metrics.RecordQueryAsync(request.Kind?.Trim().ToLowerInvariant(), !string.IsNullOrWhiteSpace(request.Query),
                response.DidYouMean is not null, response.Total, duration, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Search metrics could not be saved: {Error}", ex.GetType().Name);
        }
    }

    /// <summary>A result was opened from the search page; the position is 0-based.</summary>
    [HttpPost("clicks")]
    public async Task<IActionResult> Click([FromBody] SearchClick click, CancellationToken ct)
    {
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();
        if (!DocumentKinds.All.Contains(click.Kind) || click.Position is < 0 or > 10_000)
            return BadRequest(new { error = "kind and a non-negative position are required" });
        await metrics.RecordClickAsync(click.Kind, click.Position, ct);
        return NoContent();
    }

    public sealed record SearchClick(string Kind, int Position);

    /// <summary>Quality metrics over the last <paramref name="days"/> days: zero results, latency, clicks, indexing lag, feed.</summary>
    [HttpGet("metrics")]
    [Authorize(Policy = IdeaFusionPolicies.RequireModerator)]
    public async Task<ActionResult<SearchQualityReport>> Metrics([FromQuery] int days = 7, CancellationToken ct = default)
    {
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();
        return await metrics.ReportAsync(Math.Clamp(days, 1, 90), ct);
    }

    private ObjectResult IndexNotReady() =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The search index is starting" });
}
