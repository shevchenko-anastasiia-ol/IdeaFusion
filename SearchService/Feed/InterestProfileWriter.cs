using Npgsql;
using NpgsqlTypes;
using SearchService.Indexing;

namespace SearchService.Feed;

/// <summary>
/// Copies the interactions into searchdb and rebuilds every user's interest profile from them:
/// each like (×3), save (×4), view (×1) and own post (×2) adds its weight, halved every
/// <see cref="HalfLifeDays"/> days of age, to the post's tags, categories and author.
/// </summary>
public sealed class InterestProfileWriter(NpgsqlDataSource index)
{
    public const double HalfLifeDays = 30;

    /// <summary>Replaces the interactions with <paramref name="interactions"/> and rebuilds user_interest.</summary>
    public async Task SyncAsync(IReadOnlyList<Interaction> interactions, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await using (var cmd = new NpgsqlCommand("DELETE FROM user_interactions", conn, tx))
            await cmd.ExecuteNonQueryAsync(ct);
        await using (var writer = await conn.BeginBinaryImportAsync(
                         "COPY user_interactions (user_id, document_id, kind, at) FROM STDIN (FORMAT BINARY)", ct))
        {
            foreach (var i in interactions.DistinctBy(i => (i.UserId, i.DocumentId, i.Kind)))
            {
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(i.UserId, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(i.DocumentId, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(i.Kind, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(DateTime.SpecifyKind(i.At, DateTimeKind.Utc), NpgsqlDbType.TimestampTz, ct);
            }
            await writer.CompleteAsync(ct);
        }

        // Own posts tell what a person makes, interactions what they like; an author is never their own interest.
        await using (var cmd = new NpgsqlCommand("""
            DELETE FROM user_interest;

            WITH signals AS (
                SELECT i.user_id, i.document_id, i.at,
                       CASE i.kind WHEN 'save' THEN 4 WHEN 'like' THEN 3 ELSE 1 END AS w
                FROM user_interactions i
                UNION ALL
                SELECT d.owner_id, d.id, d.created_at, 2
                FROM search_documents d
                WHERE d.kind = 'post' AND d.owner_id IS NOT NULL
            )
            INSERT INTO user_interest (user_id, feature, weight)
            SELECT s.user_id, f.feature,
                   sum(s.w * exp(-ln(2) * greatest(extract(epoch FROM now() - s.at), 0) / 86400 / @halfLife))
            FROM signals s
            JOIN search_documents d ON d.id = s.document_id
            CROSS JOIN LATERAL (
                SELECT 'tag:' || k FROM unnest(d.tag_keys) AS k
                UNION ALL
                SELECT 'category:' || c FROM unnest(d.categories) AS c
                UNION ALL
                SELECT 'author:' || d.owner_id WHERE d.owner_id IS NOT NULL AND d.owner_id <> s.user_id
            ) AS f(feature)
            GROUP BY s.user_id, f.feature;
            """, conn, tx))
        {
            cmd.Parameters.AddWithValue("halfLife", HalfLifeDays);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await using (var cmd = new NpgsqlCommand("""
            INSERT INTO index_sources (kind, synced_at, documents, last_error, failed_at)
            VALUES ('interaction', now(), @n, NULL, NULL)
            ON CONFLICT (kind) DO UPDATE SET synced_at = now(), documents = @n, last_error = NULL, failed_at = NULL
            """, conn, tx))
        {
            cmd.Parameters.AddWithValue("n", interactions.Count);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
}
