using Dapper;
using Npgsql;

namespace SearchService.Indexing;

/// <summary>Published posts from contentdb (read-only), with tags, author, team and reaction counts.</summary>
public sealed class ContentPostSource(NpgsqlDataSource contentDb) : ISearchSource
{
    public string Kind => DocumentKinds.Post;

    // status is stored as text; old rows hold the enum number, so "0" is Published too.
    private const string Sql = """
        SELECT p.postid                       AS PostId,
               p.title                        AS Title,
               p.description                  AS Description,
               p.createdat                    AS CreatedAt,
               a.externalid                   AS AuthorExternalId,
               a.username                     AS AuthorName,
               c.externalid                   AS TeamExternalId,
               c.name                         AS TeamName,
               COALESCE(t.tags, '{}')::text[] AS Tags,
               COALESCE(l.n, 0)::int          AS Likes,
               COALESCE(s.n, 0)::int          AS Saves,
               COALESCE(v.n, 0)::int          AS Views
        FROM posts p
        LEFT JOIN postauthors a            ON a.postauthorid = p.postauthorid
        LEFT JOIN collaborationsnapshots c ON c.collaborationsnapshotid = p.collaborationsnapshotid
        LEFT JOIN (SELECT pt.postid, array_agg(tg.name ORDER BY tg.name) AS tags
                   FROM posttags pt JOIN tags tg ON tg.tagid = pt.tagid
                   GROUP BY pt.postid) t   ON t.postid = p.postid
        LEFT JOIN (SELECT postid, count(*) AS n FROM likes GROUP BY postid) l      ON l.postid = p.postid
        LEFT JOIN (SELECT postid, count(*) AS n FROM savedposts GROUP BY postid) s ON s.postid = p.postid
        LEFT JOIN (SELECT postid, count(*) AS n FROM postviews GROUP BY postid) v  ON v.postid = p.postid
        WHERE NOT p.isdeleted AND p.status IN ('Published', '0')
        """;

    // Settable properties rather than a positional record: Dapper's constructor matching needs
    // exact column types, which differ between the EF migrations (varchar vs text, int vs bigint).
    private sealed class Row
    {
        public int PostId { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AuthorExternalId { get; set; }
        public string? AuthorName { get; set; }
        public string? TeamExternalId { get; set; }
        public string? TeamName { get; set; }
        public string[] Tags { get; set; } = [];
        public int Likes { get; set; }
        public int Saves { get; set; }
        public int Views { get; set; }
    }

    public async Task<IReadOnlyList<SourceDocument>> ReadAllAsync(CancellationToken ct)
    {
        await using var conn = await contentDb.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>(new CommandDefinition(Sql, cancellationToken: ct));
        return rows.Select(r => new SourceDocument
        {
            Kind = Kind,
            SourceId = r.PostId.ToString(),
            Title = r.Title,
            Subtitle = TextParts.Join(TextParts.PublicName(r.AuthorName), r.TeamName),
            Body = r.Description,
            Tags = r.Tags,
            OwnerId = r.AuthorExternalId,
            TeamId = r.TeamExternalId,
            CreatedAt = r.CreatedAt,
            Likes = r.Likes,
            Saves = r.Saves,
            Views = r.Views,
        }).ToList();
    }
}
