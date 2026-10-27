using Dapper;
using Npgsql;

namespace SearchService.Feed;

/// <param name="DocumentId">The post as the feed returned it (post:42).</param>
/// <param name="Type">impression or click.</param>
/// <param name="Position">0-based place in the feed.</param>
public sealed record FeedEvent(string DocumentId, string Type, int Position);

/// <param name="Variant">The feed's variant from <see cref="FeedResponse.Variant"/>.</param>
public sealed record FeedEventBatch(string Variant, IReadOnlyList<FeedEvent> Events);

public sealed class FeedVariantStats
{
    public string Variant { get; init; } = "";
    public int Impressions { get; init; }
    public int Clicks { get; init; }
    public double ClickRate => Impressions == 0 ? 0 : Math.Round((double)Clicks / Impressions, 4);

    /// <summary>Share of clicks that went to the first three places.</summary>
    public double ClicksInTop3 { get; init; }
}

/// <summary>Impressions and clicks of the feed, so that variants (content, cold start, later ML) can be compared.</summary>
public sealed class FeedEventLog(NpgsqlDataSource index)
{
    public const int MaxBatch = 100;
    public static readonly string[] Types = ["impression", "click"];

    public async Task<int> WriteAsync(string userId, FeedEventBatch batch, CancellationToken ct)
    {
        var events = batch.Events.Where(e => Types.Contains(e.Type) && e.DocumentId.Length is > 0 and <= 100 && e.Position >= 0).ToList();
        if (events.Count == 0) return 0;

        await using var conn = await index.OpenConnectionAsync(ct);
        return await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO feed_events (user_id, document_id, event, position, variant)
            VALUES (@userId, @DocumentId, @Type, @Position, @variant)
            """, events.Select(e => new { userId, e.DocumentId, e.Type, e.Position, variant = batch.Variant }), cancellationToken: ct));
    }

    public async Task<IReadOnlyList<FeedVariantStats>> StatsAsync(TimeSpan period, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        return (await conn.QueryAsync<FeedVariantStats>(new CommandDefinition("""
            SELECT variant AS Variant,
                   count(*) FILTER (WHERE event = 'impression')::int AS Impressions,
                   count(*) FILTER (WHERE event = 'click')::int AS Clicks,
                   coalesce(round((count(*) FILTER (WHERE event = 'click' AND position < 3))::numeric
                                  / nullif(count(*) FILTER (WHERE event = 'click'), 0), 4), 0)::float8 AS ClicksInTop3
            FROM feed_events
            WHERE at > now() - @period
            GROUP BY variant ORDER BY variant
            """, new { period }, cancellationToken: ct))).ToList();
    }
}
