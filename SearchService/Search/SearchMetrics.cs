using Dapper;
using Npgsql;
using SearchService.Feed;

namespace SearchService.Search;

public sealed class SearchQualityReport
{
    public int Days { get; init; }
    public int Queries { get; init; }

    /// <summary>Share of searches with a query that found nothing.</summary>
    public double ZeroResultRate { get; init; }

    /// <summary>Share of searches with a query where "did you mean" was offered.</summary>
    public double CorrectedRate { get; init; }

    public double LatencyP50Ms { get; init; }
    public double LatencyP95Ms { get; init; }
    public int Clicks { get; init; }

    /// <summary>Share of result clicks that went to the first three places.</summary>
    public double ClicksInTop3 { get; init; }

    /// <summary>From a post's creation to its arrival in the index, over posts created in the period.</summary>
    public double IndexLagP50Seconds { get; init; }
    public double IndexLagP95Seconds { get; init; }

    public IReadOnlyList<FeedVariantStats> Feed { get; init; } = [];
}

/// <summary>
/// Search quality metrics: zero-result rate, latency, clicks in the top three and indexing lag.
/// Stores no user ids and no query text.
/// </summary>
public sealed class SearchMetrics(NpgsqlDataSource index, FeedEventLog feed)
{
    public static readonly TimeSpan Retention = TimeSpan.FromDays(90);

    public async Task RecordQueryAsync(string? kind, bool hasQuery, bool corrected, int results, TimeSpan duration, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO search_queries (kind, has_query, corrected, results, duration_ms)
            VALUES (@kind, @hasQuery, @corrected, @results, @ms)
            """, new { kind, hasQuery, corrected, results, ms = (int)Math.Ceiling(duration.TotalMilliseconds) }, cancellationToken: ct));
    }

    public async Task RecordClickAsync(string kind, int position, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO search_clicks (kind, position) VALUES (@kind, @position)", new { kind, position }, cancellationToken: ct));
    }

    /// <summary>Drops metric rows older than <see cref="Retention"/>.</summary>
    public async Task PruneAsync(CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("""
            DELETE FROM search_queries WHERE at < now() - @retention;
            DELETE FROM search_clicks WHERE at < now() - @retention;
            DELETE FROM feed_events WHERE at < now() - @retention;
            """, new { retention = Retention }, cancellationToken: ct));
    }

    public async Task<SearchQualityReport> ReportAsync(int days, CancellationToken ct)
    {
        var period = TimeSpan.FromDays(days);
        await using var conn = await index.OpenConnectionAsync(ct);
        var q = await conn.QuerySingleAsync<SearchQualityReport>(new CommandDefinition("""
            SELECT count(*)::int AS Queries,
                   coalesce(avg(CASE WHEN results = 0 THEN 1.0 ELSE 0 END) FILTER (WHERE has_query), 0)::float8 AS ZeroResultRate,
                   coalesce(avg(CASE WHEN corrected THEN 1.0 ELSE 0 END) FILTER (WHERE has_query), 0)::float8 AS CorrectedRate,
                   coalesce(percentile_cont(0.5) WITHIN GROUP (ORDER BY duration_ms), 0)::float8 AS LatencyP50Ms,
                   coalesce(percentile_cont(0.95) WITHIN GROUP (ORDER BY duration_ms), 0)::float8 AS LatencyP95Ms,
                   (SELECT count(*)::int FROM search_clicks WHERE at > now() - @period) AS Clicks,
                   coalesce((SELECT avg(CASE WHEN position < 3 THEN 1.0 ELSE 0 END) FROM search_clicks WHERE at > now() - @period), 0)::float8 AS ClicksInTop3,
                   coalesce((SELECT percentile_cont(0.5) WITHIN GROUP (ORDER BY extract(epoch FROM first_indexed_at - created_at))
                             FROM search_documents WHERE kind = 'post' AND created_at > now() - @period AND first_indexed_at >= created_at), 0)::float8 AS IndexLagP50Seconds,
                   coalesce((SELECT percentile_cont(0.95) WITHIN GROUP (ORDER BY extract(epoch FROM first_indexed_at - created_at))
                             FROM search_documents WHERE kind = 'post' AND created_at > now() - @period AND first_indexed_at >= created_at), 0)::float8 AS IndexLagP95Seconds
            FROM search_queries
            WHERE at > now() - @period
            """, new { period }, cancellationToken: ct));

        return new SearchQualityReport
        {
            Days = days, Queries = q.Queries,
            ZeroResultRate = Math.Round(q.ZeroResultRate, 4), CorrectedRate = Math.Round(q.CorrectedRate, 4),
            LatencyP50Ms = q.LatencyP50Ms, LatencyP95Ms = q.LatencyP95Ms,
            Clicks = q.Clicks, ClicksInTop3 = Math.Round(q.ClicksInTop3, 4),
            IndexLagP50Seconds = Math.Round(q.IndexLagP50Seconds, 1), IndexLagP95Seconds = Math.Round(q.IndexLagP95Seconds, 1),
            Feed = await feed.StatsAsync(period, ct),
        };
    }
}
