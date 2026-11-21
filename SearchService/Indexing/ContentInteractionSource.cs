using Dapper;
using Npgsql;

namespace SearchService.Indexing;

/// <param name="UserId">Keycloak sub of the user who acted.</param>
/// <param name="DocumentId">search_documents id of the post.</param>
/// <param name="Kind">view, like or save.</param>
public sealed record Interaction(string UserId, string DocumentId, string Kind, DateTime At);

public interface IInteractionSource
{
    Task<IReadOnlyList<Interaction>> ReadAllAsync(CancellationToken ct);
}

/// <summary>
/// Likes, saves and views from contentdb (read-only) of the last <see cref="Window"/>. Content keys them by
/// its own integer user id; postauthors maps that id to the Keycloak sub (externalid).
/// </summary>
public sealed class ContentInteractionSource(NpgsqlDataSource contentDb) : IInteractionSource
{
    public static readonly TimeSpan Window = TimeSpan.FromDays(180);

    private const string Sql = """
        SELECT a.externalid AS UserId, 'post:' || x.postid AS DocumentId, x.kind AS Kind, max(x.at) AS At
        FROM (SELECT userid, postid, 'like' AS kind, createdat AS at FROM likes
              UNION ALL
              SELECT userid, postid, 'save', savedat FROM savedposts
              UNION ALL
              SELECT userid, postid, 'view', viewedat FROM postviews WHERE userid IS NOT NULL) x
        JOIN postauthors a ON a.userid = x.userid
        WHERE a.externalid IS NOT NULL AND x.at > now() - @window
        GROUP BY a.externalid, x.postid, x.kind
        """;

    private sealed class Row
    {
        public string UserId { get; set; } = "";
        public string DocumentId { get; set; } = "";
        public string Kind { get; set; } = "";
        public DateTime At { get; set; }
    }

    public async Task<IReadOnlyList<Interaction>> ReadAllAsync(CancellationToken ct)
    {
        await using var conn = await contentDb.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>(new CommandDefinition(Sql, new { window = Window }, cancellationToken: ct));
        return rows.Select(r => new Interaction(r.UserId, r.DocumentId, r.Kind, r.At)).ToList();
    }
}
