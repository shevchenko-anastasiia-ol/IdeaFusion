using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SearchService.Indexing;
using SearchService.Search;
using ServiceDefaults.Security;

namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public sealed class SearchController(
    SearchQueryService search,
    IndexSyncTrigger sync,
    NpgsqlDataSource indexDb) : ControllerBase
{
    /// <summary>Search posts, teams, vacancies and people. Typos, the wrong keyboard layout and transliteration are tolerated.</summary>
    /// <param name="q">The query; empty lists the newest documents.</param>
    /// <param name="kind">post, team, vacancy or user.</param>
    /// <param name="category">A category name, e.g. "Музика".</param>
    /// <param name="tag">Tag filter, repeatable: documents with any of the tags.</param>
    [HttpGet]
    public async Task<ActionResult<SearchResponse>> Search(
        [FromQuery] string? q, [FromQuery] string? kind, [FromQuery] string? category,
        [FromQuery(Name = "tag")] string[]? tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (kind is not null && !DocumentKinds.All.Contains(kind.Trim().ToLowerInvariant()))
            return BadRequest(new { error = $"kind must be one of: {string.Join(", ", DocumentKinds.All)}" });
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();

        return await search.SearchAsync(new SearchRequest
        {
            Query = q, Kind = kind, Category = category, Tags = tag ?? [], Page = page, PageSize = pageSize,
        }, ct);
    }

    /// <summary>Autocomplete: completions of the last word and the best few documents.</summary>
    [HttpGet("suggest")]
    public async Task<ActionResult<SuggestResponse>> Suggest([FromQuery] string? q, [FromQuery] int limit = 8, CancellationToken ct = default)
    {
        if (!sync.SchemaReady.IsCompleted) return IndexNotReady();
        return await search.SuggestAsync(q, limit, ct);
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

    private ObjectResult IndexNotReady() =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The search index is starting" });
}
