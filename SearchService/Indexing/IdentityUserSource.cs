using Dapper;
using Npgsql;

namespace SearchService.Indexing;

/// <summary>People from identityservicedb (read-only); locked-out accounts are not searchable.</summary>
public sealed class IdentityUserSource(NpgsqlDataSource identityDb) : ISearchSource
{
    public string Kind => DocumentKinds.User;

    private const string Sql = """
        SELECT "Id"             AS Id,
               "UserName"       AS UserName,
               "FullName"       AS FullName,
               "Specialization" AS Specialization,
               "AvatarUrl"      AS AvatarUrl,
               "CreatedAt"      AS CreatedAt
        FROM "AspNetUsers"
        WHERE "LockoutEnd" IS NULL OR "LockoutEnd" < now()
        """;

    private sealed class Row
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Specialization { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public async Task<IReadOnlyList<SourceDocument>> ReadAllAsync(CancellationToken ct)
    {
        await using var conn = await identityDb.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Row>(new CommandDefinition(Sql, cancellationToken: ct));
        var docs = new List<SourceDocument>();
        foreach (var r in rows)
        {
            var name = TextParts.PublicName(r.UserName);
            var fullName = string.IsNullOrWhiteSpace(r.FullName) ? null : r.FullName.Trim();
            if (fullName is null && name is null) continue;

            docs.Add(new SourceDocument
            {
                Kind = Kind,
                SourceId = r.Id.ToString(),
                Title = fullName ?? name!,
                // The user name is searchable too ("olena_k"), the specialization is the role people look for.
                Subtitle = TextParts.Join(fullName is null ? null : name, r.Specialization),
                Body = r.Specialization,
                Roles = string.IsNullOrWhiteSpace(r.Specialization) ? [] : [r.Specialization],
                ImageUrl = r.AvatarUrl,
                OwnerId = r.Id.ToString(),
                CreatedAt = r.CreatedAt,
            });
        }
        return docs;
    }
}
