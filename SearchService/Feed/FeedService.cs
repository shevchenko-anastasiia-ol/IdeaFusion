using System.Text;
using Dapper;
using Npgsql;
using SearchService.Indexing;
using SearchService.Search;

namespace SearchService.Feed;

public sealed class FeedReason
{
    /// <summary>tag, category, author, specialization or popular.</summary>
    public string Kind { get; init; } = "popular";
    public string? Value { get; init; }
    public string? Label { get; init; }

    /// <summary>Ready-made explanation, e.g. "Бо вас цікавить #3D".</summary>
    public string Text => Kind switch
    {
        "tag" => $"Бо вас цікавить #{Label}",
        "category" => $"Бо вас цікавить «{Label}»",
        "author" => $"Бо вам подобаються роботи {Label}",
        "specialization" => "Пасує до вашої спеціалізації",
        _ => "Популярне за тиждень",
    };
}

public sealed class FeedItem
{
    public required SearchHit Post { get; init; }
    public required FeedReason Reason { get; init; }
    public double Score { get; init; }
}

public sealed class FeedResponse
{
    /// <summary>content-v1 with an interest profile, cold-start without one; logged with feed events.</summary>
    public required string Variant { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasMore { get; init; }

    /// <summary>Pass back with the next page so that freshness is computed against the same moment.</summary>
    public DateTime AsOf { get; init; }

    public IReadOnlyList<FeedItem> Items { get; init; } = [];
}

/// <summary>
/// The "For you" feed, content-based. A candidate post scores by how well its tags, categories and author
/// match the reader's interest profile, plus popularity and freshness. Own posts and posts the reader has
/// already seen are left out, and no author appears more than twice in a row. A reader without a profile
/// (cold start) gets fresh popular posts, lifted when they fit the reader's specialization.
/// </summary>
public sealed class FeedService(NpgsqlDataSource index, TagCatalogCache catalogs)
{
    public const string ContentVariant = "content-v1";
    public const string ColdStartVariant = "cold-start";
    public const int MaxPageSize = 50;
    public const int MaxPage = 10;
    public const int MaxSameAuthorInARow = 2;

    /// <summary>Freshness halves every week, so posts of the last seven days lead the cold start.</summary>
    private const double FreshHalfLifeDays = 7;

    private sealed class Candidate
    {
        public string Id { get; init; } = "";
        public string Kind { get; init; } = "";
        public string SourceId { get; init; } = "";
        public string Title { get; init; } = "";
        public string? Subtitle { get; init; }
        public string? Snippet { get; init; }
        public string[] Tags { get; init; } = [];
        public string[] Categories { get; init; } = [];
        public string? ImageUrl { get; init; }
        public string? OwnerId { get; init; }
        public string? TeamId { get; init; }
        public DateTime CreatedAt { get; init; }
        public int Likes { get; init; }
        public int Saves { get; init; }
        public int Views { get; init; }
        public double Score { get; init; }
        public double Interest { get; init; }
        public bool Spec { get; init; }
        public string? Reason { get; init; }
        public string? ReasonLabel { get; init; }
    }

    public async Task<FeedResponse> FeedAsync(string userId, int page, int pageSize, DateTime? asOf, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, MaxPage);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var now = asOf?.ToUniversalTime() ?? DateTime.UtcNow;

        await using var conn = await index.OpenConnectionAsync(ct);
        var hasProfile = await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM user_interest WHERE user_id = @userId)", new { userId }, cancellationToken: ct));

        // The reader's specialization, widened by the role directory, as an OR of full-text queries.
        var roles = await conn.QueryFirstOrDefaultAsync<string[]>(new CommandDefinition(
            "SELECT roles FROM search_documents WHERE id = @id", new { id = $"{DocumentKinds.User}:{userId}" }, cancellationToken: ct)) ?? [];
        var catalog = await catalogs.GetAsync(ct);
        var specWords = roles.SelectMany(catalog.RoleWords).Where(w => w.Length >= 3).Distinct().Take(16).ToArray();
        var p = new DynamicParameters(new { userId, asOf = now, freshHalfLife = FreshHalfLifeDays, limit = Math.Min(500, (page + 2) * pageSize * 2) });
        var specQuery = new StringBuilder();
        for (var i = 0; i < specWords.Length; i++)
        {
            p.Add($"s{i}", specWords[i]);
            specQuery.Append(i == 0 ? "" : " || ").Append($"plainto_tsquery('ideafusion', @s{i})");
        }
        var spec = specWords.Length == 0 ? "false" : $"d.fts @@ ({specQuery})";

        var sql = $"""
            WITH profile AS (
                SELECT feature,
                       weight / max(weight) OVER (PARTITION BY split_part(feature, ':', 1))
                       * CASE split_part(feature, ':', 1) WHEN 'tag' THEN 1.0 WHEN 'author' THEN 0.8 ELSE 0.4 END AS w
                FROM user_interest WHERE user_id = @userId
            ),
            scored AS (
                SELECT d.id, d.kind, d.source_id AS SourceId, d.title, d.subtitle, left(d.body, 240) AS Snippet,
                       d.tags, d.categories, d.image_url AS ImageUrl, d.owner_id AS OwnerId, d.team_id AS TeamId,
                       d.created_at AS CreatedAt, d.likes, d.saves, d.views,
                       m.interest, m.reason, ({spec}) AS spec,
                       2.0 * m.interest / (1 + m.interest)
                       + CASE WHEN {spec} THEN 0.5 ELSE 0 END
                       + 0.15 * ln(1 + d.views + 3 * d.likes + 4 * d.saves)
                       + exp(-ln(2) * greatest(extract(epoch FROM @asOf - d.created_at), 0) / 86400 / @freshHalfLife) AS score
                FROM search_documents d
                CROSS JOIN LATERAL (
                    SELECT coalesce(sum(pr.w), 0) AS interest, (array_agg(pr.feature ORDER BY pr.w DESC, pr.feature))[1] AS reason
                    FROM profile pr
                    WHERE pr.feature IN (SELECT 'tag:' || k FROM unnest(d.tag_keys) AS k
                                         UNION ALL SELECT 'category:' || c FROM unnest(d.categories) AS c
                                         UNION ALL SELECT 'author:' || d.owner_id)
                ) m
                WHERE d.kind = 'post'
                  AND d.created_at <= @asOf
                  AND d.owner_id IS DISTINCT FROM @userId
                  AND NOT EXISTS (SELECT 1 FROM user_interactions i WHERE i.user_id = @userId AND i.document_id = d.id)
            )
            SELECT s.*,
                   CASE split_part(s.reason, ':', 1)
                       WHEN 'tag' THEN coalesce(t.label, substr(s.reason, 5))
                       WHEN 'category' THEN substr(s.reason, 10)
                       WHEN 'author' THEN (SELECT u.title FROM search_documents u WHERE u.id = 'user:' || substr(s.reason, 8))
                   END AS ReasonLabel
            FROM scored s
            LEFT JOIN search_tags t ON s.reason LIKE 'tag:%' AND t.key = substr(s.reason, 5)
            ORDER BY s.score DESC, s.CreatedAt DESC, s.id DESC
            LIMIT @limit
            """;
        var candidates = (await conn.QueryAsync<Candidate>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();

        var ordered = Diversify(candidates, c => c.OwnerId);
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(c => new FeedItem
        {
            Post = new SearchHit
            {
                Id = c.Id, Kind = c.Kind, SourceId = c.SourceId, Title = c.Title, Subtitle = c.Subtitle, Snippet = c.Snippet,
                Tags = c.Tags, Categories = c.Categories, ImageUrl = c.ImageUrl, OwnerId = c.OwnerId, TeamId = c.TeamId,
                CreatedAt = c.CreatedAt, Likes = c.Likes, Saves = c.Saves, Views = c.Views, Score = c.Score,
            },
            Reason = ReasonOf(c),
            Score = c.Score,
        }).ToList();

        return new FeedResponse
        {
            Variant = hasProfile ? ContentVariant : ColdStartVariant,
            Page = page,
            PageSize = pageSize,
            HasMore = ordered.Count > page * pageSize && page < MaxPage,
            AsOf = now,
            Items = items,
        };
    }

    private static FeedReason ReasonOf(Candidate c)
    {
        if (c.Interest > 0 && c.Reason is { } feature && c.ReasonLabel is { Length: > 0 } label)
        {
            var colon = feature.IndexOf(':');
            return new FeedReason { Kind = feature[..colon], Value = feature[(colon + 1)..], Label = label };
        }
        return new FeedReason { Kind = c.Spec ? "specialization" : "popular" };
    }

    /// <summary>
    /// Keeps the order but never lets one author fill more than <see cref="MaxSameAuthorInARow"/> places in a row:
    /// the next best post of another author moves up. When only one author is left, their posts follow anyway.
    /// </summary>
    internal static List<T> Diversify<T>(IReadOnlyList<T> ranked, Func<T, string?> author)
    {
        var rest = ranked.ToList();
        var result = new List<T>(rest.Count);
        while (rest.Count > 0)
        {
            var streak = result.Count >= MaxSameAuthorInARow
                         && result.TakeLast(MaxSameAuthorInARow).Select(author).Distinct().Count() == 1
                ? author(result[^1])
                : null;
            var next = streak is null ? 0 : rest.FindIndex(x => author(x) != streak);
            if (next < 0) next = 0;
            result.Add(rest[next]);
            rest.RemoveAt(next);
        }
        return result;
    }
}
