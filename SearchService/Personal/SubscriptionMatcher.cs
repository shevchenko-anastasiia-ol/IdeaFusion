using Dapper;
using Npgsql;
using SearchService.Search;

namespace SearchService.Personal;

/// <summary>
/// Subscriptions: after a sync cycle every saved search with notify on is run against the documents that
/// entered the index since it was last checked ("tell me when a vacancy for my role appears"), and each new
/// match becomes a notification for its owner. The user's own documents never notify them.
/// </summary>
public sealed class SubscriptionMatcher(NpgsqlDataSource index, SearchQueryService search, ILogger<SubscriptionMatcher> logger)
{
    /// <summary>At most this many notifications per saved search and cycle.</summary>
    public const int MaxPerCycle = 20;

    private sealed class Subscription
    {
        public Guid Id { get; init; }
        public string UserId { get; init; } = "";
        public string Query { get; init; } = "";
        public string Filters { get; init; } = "{}";
        public DateTime CheckedAt { get; init; }
    }

    /// <summary>Returns the number of notifications created.</summary>
    public async Task<int> RunAsync(CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        // Everything indexed before this instant is checked now; later documents wait for the next cycle.
        var now = await conn.ExecuteScalarAsync<DateTime>(new CommandDefinition("SELECT now()", cancellationToken: ct));
        var subscriptions = (await conn.QueryAsync<Subscription>(new CommandDefinition("""
            SELECT id, user_id AS UserId, query, filters::text AS Filters, checked_at AS CheckedAt
            FROM saved_searches
            WHERE notify AND EXISTS (SELECT 1 FROM search_documents d WHERE d.first_indexed_at > checked_at)
            """, cancellationToken: ct))).ToList();

        var created = 0;
        foreach (var s in subscriptions)
        {
            try
            {
                var request = SearchFilters.FromJson(s.Filters).ToRequest(s.Query) with
                {
                    Sort = SearchSort.New, PageSize = MaxPerCycle, IndexedAfter = s.CheckedAt, ExcludeOwnerId = s.UserId,
                };
                var hits = (await search.SearchAsync(request, ct)).Items;
                foreach (var hit in hits)
                {
                    created += await conn.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO search_notifications (user_id, saved_search_id, document_id, kind, title)
                        VALUES (@userId, @id, @documentId, @kind, @title)
                        ON CONFLICT (saved_search_id, document_id) DO NOTHING
                        """, new { userId = s.UserId, id = s.Id, documentId = hit.Id, kind = hit.Kind, title = hit.Title },
                        cancellationToken: ct));
                }
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE saved_searches SET checked_at = @now WHERE id = @id", new { now, id = s.Id }, cancellationToken: ct));
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // One saved search that cannot run must not stop the others.
                logger.LogWarning(ex, "Saved search {Id} could not be matched", s.Id);
            }
        }
        return created;
    }
}
