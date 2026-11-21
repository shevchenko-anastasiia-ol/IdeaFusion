using Dapper;
using Npgsql;
using SearchService.Search;

namespace SearchService.Personal;

public sealed class HistoryEntry
{
    public long Id { get; init; }
    public string Query { get; init; } = "";
    public SearchFilters Filters { get; init; } = new();
    public int ResultsCount { get; init; }
    public DateTime SearchedAt { get; init; }
}

public sealed class SavedSearch
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Query { get; init; } = "";
    public SearchFilters Filters { get; init; } = new();
    public bool Notify { get; init; }
    public int Unread { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record SavedSearchInput(string Name, string? Query, SearchFilters? Filters, bool Notify);

public sealed class SearchNotification
{
    public long Id { get; init; }
    public Guid SavedSearchId { get; init; }
    public string SavedSearchName { get; init; } = "";
    public string DocumentId { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public bool Read { get; init; }
}

public enum SaveResult { Saved, NotFound, DuplicateName, LimitReached }

/// <summary>
/// A user's search history, saved searches and the notifications of their subscriptions.
/// Every method takes the user id and touches only that user's rows.
/// </summary>
public sealed class PersonalSearchStore(NpgsqlDataSource index)
{
    public const int HistoryLimit = 50;
    public static readonly TimeSpan HistoryMaxAge = TimeSpan.FromDays(90);
    public const int SavedLimit = 50;
    public const int MaxNameLength = 100;

    private sealed class Row
    {
        public long Id { get; init; }
        public Guid Guid { get; init; }
        public string Name { get; init; } = "";
        public string Query { get; init; } = "";
        public string Filters { get; init; } = "{}";
        public int ResultsCount { get; init; }
        public bool Notify { get; init; }
        public int Unread { get; init; }
        public DateTime At { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    /// <summary>
    /// Remembers a search. Repeating the same search (same normalized query and filters) moves it to the top
    /// instead of adding a row; only the newest <see cref="HistoryLimit"/> entries of the last 90 days are kept.
    /// </summary>
    public async Task RecordAsync(string userId, string? query, SearchFilters filters, int results, CancellationToken ct)
    {
        var q = (query ?? "").Trim();
        await using var conn = await index.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO search_history (user_id, query, filters, fingerprint, results_count)
            VALUES (@userId, @q, @filters::jsonb, @fingerprint, @results)
            ON CONFLICT (user_id, fingerprint) DO UPDATE
                SET query = EXCLUDED.query, results_count = EXCLUDED.results_count, searched_at = now();

            DELETE FROM search_history h
            WHERE h.user_id = @userId
              AND (h.searched_at < now() - @maxAge
                   OR h.id NOT IN (SELECT id FROM search_history WHERE user_id = @userId
                                   ORDER BY searched_at DESC, id DESC LIMIT @limit));
            """, new
        {
            userId, q, filters = filters.ToJson(), fingerprint = SearchFilters.Fingerprint(q, filters), results,
            maxAge = HistoryMaxAge, limit = HistoryLimit,
        }, cancellationToken: ct));
    }

    /// <summary>History, newest first; with a prefix only the entries whose query starts with it (for the search box).</summary>
    public async Task<IReadOnlyList<HistoryEntry>> HistoryAsync(string userId, string? prefix, int limit, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>(new CommandDefinition("""
            SELECT id, query, filters::text AS Filters, results_count AS ResultsCount, searched_at AS At
            FROM search_history
            WHERE user_id = @userId AND searched_at >= now() - @maxAge
              AND (@prefix = '' OR lower(query) LIKE @prefix || '%')
            ORDER BY searched_at DESC, id DESC
            LIMIT @limit
            """, new { userId, prefix = EscapeLike((prefix ?? "").Trim().ToLowerInvariant()), maxAge = HistoryMaxAge, limit = Math.Clamp(limit, 1, HistoryLimit) },
            cancellationToken: ct));
        return rows.Select(r => new HistoryEntry
        {
            Id = r.Id, Query = r.Query, Filters = SearchFilters.FromJson(r.Filters), ResultsCount = r.ResultsCount, SearchedAt = r.At,
        }).ToList();
    }

    /// <summary>Deletes one entry, or the whole history when <paramref name="id"/> is null. Returns the number deleted.</summary>
    public async Task<int> DeleteHistoryAsync(string userId, long? id, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        return await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM search_history WHERE user_id = @userId AND (@id::bigint IS NULL OR id = @id)",
            new { userId, id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<SavedSearch>> SavedAsync(string userId, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>(new CommandDefinition("""
            SELECT s.id AS Guid, s.name, s.query, s.filters::text AS Filters, s.notify,
                   (SELECT count(*)::int FROM search_notifications n WHERE n.saved_search_id = s.id AND n.read_at IS NULL) AS Unread,
                   s.created_at AS At, s.updated_at AS UpdatedAt
            FROM saved_searches s
            WHERE s.user_id = @userId
            ORDER BY s.updated_at DESC, s.name
            """, new { userId }, cancellationToken: ct));
        return rows.Select(ToSaved).ToList();
    }

    public async Task<SavedSearch?> GetSavedAsync(string userId, Guid id, CancellationToken ct) =>
        (await SavedAsync(userId, ct)).FirstOrDefault(s => s.Id == id);

    /// <summary>Creates a saved search, or updates <paramref name="id"/>. Turning notify on starts the subscription from now.</summary>
    public async Task<(SaveResult Result, Guid Id)> SaveAsync(string userId, Guid? id, SavedSearchInput input, CancellationToken ct)
    {
        var name = input.Name.Trim();
        var args = new
        {
            userId, id, name, query = (input.Query ?? "").Trim(),
            filters = (input.Filters ?? new SearchFilters()).ToJson(), notify = input.Notify, limit = SavedLimit,
        };
        await using var conn = await index.OpenConnectionAsync(ct);
        try
        {
            if (id is null)
            {
                var created = await conn.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition("""
                    INSERT INTO saved_searches (user_id, name, query, filters, notify)
                    SELECT @userId, @name, @query, @filters::jsonb, @notify
                    WHERE (SELECT count(*) FROM saved_searches WHERE user_id = @userId) < @limit
                    RETURNING id
                    """, args, cancellationToken: ct));
                return created is { } newId ? (SaveResult.Saved, newId) : (SaveResult.LimitReached, Guid.Empty);
            }

            var updated = await conn.ExecuteAsync(new CommandDefinition("""
                UPDATE saved_searches
                SET name = @name, query = @query, filters = @filters::jsonb, updated_at = now(),
                    checked_at = CASE WHEN @notify AND NOT notify THEN now() ELSE checked_at END,
                    notify = @notify
                WHERE id = @id AND user_id = @userId
                """, args, cancellationToken: ct));
            return updated == 0 ? (SaveResult.NotFound, id.Value) : (SaveResult.Saved, id.Value);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return (SaveResult.DuplicateName, id ?? Guid.Empty);
        }
    }

    public async Task<bool> DeleteSavedAsync(string userId, Guid id, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        return await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM saved_searches WHERE id = @id AND user_id = @userId", new { id, userId }, cancellationToken: ct)) > 0;
    }

    public async Task<IReadOnlyList<SearchNotification>> NotificationsAsync(string userId, bool unreadOnly, int limit, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<SearchNotification>(new CommandDefinition("""
            SELECT n.id, n.saved_search_id AS SavedSearchId, s.name AS SavedSearchName, n.document_id AS DocumentId,
                   n.kind, n.title, n.created_at AS CreatedAt, n.read_at IS NOT NULL AS Read
            FROM search_notifications n
            JOIN saved_searches s ON s.id = n.saved_search_id
            WHERE n.user_id = @userId AND (NOT @unreadOnly OR n.read_at IS NULL)
            ORDER BY n.created_at DESC, n.id DESC
            LIMIT @limit
            """, new { userId, unreadOnly, limit = Math.Clamp(limit, 1, 100) }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <summary>Marks the given notifications (all of them when <paramref name="ids"/> is empty) as read.</summary>
    public async Task<int> MarkReadAsync(string userId, long[] ids, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        return await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE search_notifications SET read_at = now()
            WHERE user_id = @userId AND read_at IS NULL AND (cardinality(@ids) = 0 OR id = ANY(@ids))
            """, new { userId, ids = PgArray.Of(ids) }, cancellationToken: ct));
    }

    private static SavedSearch ToSaved(Row r) => new()
    {
        Id = r.Guid, Name = r.Name, Query = r.Query, Filters = SearchFilters.FromJson(r.Filters), Notify = r.Notify,
        Unread = r.Unread, CreatedAt = r.At, UpdatedAt = r.UpdatedAt,
    };

    private static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
