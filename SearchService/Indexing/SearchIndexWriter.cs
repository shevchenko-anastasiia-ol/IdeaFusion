using Npgsql;
using NpgsqlTypes;
using SearchService.Normalization;

namespace SearchService.Indexing;

/// <summary>A search_documents row: the source fields plus everything derived for matching.</summary>
public sealed record IndexRow(
    SourceDocument Source,
    string[] Categories,
    string TagText,
    string NormText,
    string TranslitText);

/// <summary>Writes snapshots of the sources into searchdb.</summary>
public sealed class SearchIndexWriter(NpgsqlDataSource index)
{
    /// <summary>tag → categories, from tag_categories. Keys are lower case.</summary>
    public async Task<IReadOnlyDictionary<string, string[]>> LoadTagCategoriesAsync(CancellationToken ct)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = index.CreateCommand("SELECT tag, category FROM tag_categories ORDER BY category");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var tag = reader.GetString(0);
            if (!map.TryGetValue(tag, out var list)) map[tag] = list = [];
            list.Add(reader.GetString(1));
        }
        return map.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    public static IndexRow BuildRow(SourceDocument doc, IReadOnlyDictionary<string, string[]> tagCategories)
    {
        var allCategories = tagCategories.Values.SelectMany(c => c).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var categories = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var tag in doc.Tags.Concat(doc.CategoryHints))
        {
            var key = tag.Trim();
            if (tagCategories.TryGetValue(key, out var mapped)) categories.UnionWith(mapped);
            // A team's own category may already be a category name ("Музика").
            else if (allCategories.TryGetValue(key, out var name)) categories.Add(name);
        }

        var tagText = string.Join(", ", doc.Tags.Concat(categories));
        var norm = TextNormalizer.Normalize(string.Join(' ', doc.Title, doc.Subtitle, tagText, doc.Body));
        return new IndexRow(doc, categories.ToArray(), tagText, norm, TextNormalizer.Transliterate(norm));
    }

    /// <summary>
    /// Makes the documents of <paramref name="kind"/> equal to <paramref name="rows"/>:
    /// inserts new ones, updates changed ones, removes the ones the source no longer has.
    /// Returns the number of documents that changed.
    /// </summary>
    public async Task<int> SyncKindAsync(string kind, IReadOnlyList<IndexRow> rows, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await Exec(conn, tx, """
            CREATE TEMP TABLE incoming (
                id text, kind text, source_id text, title text, subtitle text, body text,
                tags text[], categories text[], tag_text text, image_url text, owner_id text, team_id text,
                created_at timestamptz, likes int, saves int, views int, open_vacancies int,
                norm_text text, translit_text text
            ) ON COMMIT DROP
            """, ct);

        await using (var writer = await conn.BeginBinaryImportAsync("""
            COPY incoming (id, kind, source_id, title, subtitle, body, tags, categories, tag_text, image_url,
                           owner_id, team_id, created_at, likes, saves, views, open_vacancies, norm_text, translit_text)
            FROM STDIN (FORMAT BINARY)
            """, ct))
        {
            foreach (var row in rows.DistinctBy(r => r.Source.Id))
            {
                var d = row.Source;
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(d.Id, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(d.Kind, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(d.SourceId, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(d.Title, NpgsqlDbType.Text, ct);
                await WriteNullable(writer, d.Subtitle, ct);
                await WriteNullable(writer, d.Body, ct);
                await writer.WriteAsync(d.Tags.ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                await writer.WriteAsync(row.Categories, NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                await writer.WriteAsync(row.TagText, NpgsqlDbType.Text, ct);
                await WriteNullable(writer, d.ImageUrl, ct);
                await WriteNullable(writer, d.OwnerId, ct);
                await WriteNullable(writer, d.TeamId, ct);
                await writer.WriteAsync(DateTime.SpecifyKind(d.CreatedAt, DateTimeKind.Utc), NpgsqlDbType.TimestampTz, ct);
                await writer.WriteAsync(d.Likes, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(d.Saves, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(d.Views, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(d.OpenVacancies, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(row.NormText, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(row.TranslitText, NpgsqlDbType.Text, ct);
            }
            await writer.CompleteAsync(ct);
        }

        // Unchanged rows are left alone, so their tsvector is not recomputed every cycle.
        var upserted = await Exec(conn, tx, """
            INSERT INTO search_documents AS d (id, kind, source_id, title, subtitle, body, tags, categories, tag_text,
                image_url, owner_id, team_id, created_at, likes, saves, views, open_vacancies, norm_text, translit_text)
            SELECT id, kind, source_id, title, subtitle, body, tags, categories, tag_text,
                image_url, owner_id, team_id, created_at, likes, saves, views, open_vacancies, norm_text, translit_text
            FROM incoming
            ON CONFLICT (id) DO UPDATE SET
                title = EXCLUDED.title, subtitle = EXCLUDED.subtitle, body = EXCLUDED.body,
                tags = EXCLUDED.tags, categories = EXCLUDED.categories, tag_text = EXCLUDED.tag_text,
                image_url = EXCLUDED.image_url, owner_id = EXCLUDED.owner_id, team_id = EXCLUDED.team_id,
                created_at = EXCLUDED.created_at, likes = EXCLUDED.likes, saves = EXCLUDED.saves,
                views = EXCLUDED.views, open_vacancies = EXCLUDED.open_vacancies,
                norm_text = EXCLUDED.norm_text, translit_text = EXCLUDED.translit_text, indexed_at = now()
            WHERE (d.title, d.subtitle, d.body, d.tags, d.categories, d.tag_text, d.image_url, d.owner_id, d.team_id,
                   d.created_at, d.likes, d.saves, d.views, d.open_vacancies)
                  IS DISTINCT FROM
                  (EXCLUDED.title, EXCLUDED.subtitle, EXCLUDED.body, EXCLUDED.tags, EXCLUDED.categories, EXCLUDED.tag_text,
                   EXCLUDED.image_url, EXCLUDED.owner_id, EXCLUDED.team_id, EXCLUDED.created_at, EXCLUDED.likes,
                   EXCLUDED.saves, EXCLUDED.views, EXCLUDED.open_vacancies)
            """, ct);

        var deleted = await Exec(conn, tx, """
            DELETE FROM search_documents d
            WHERE d.kind = @kind AND NOT EXISTS (SELECT 1 FROM incoming i WHERE i.id = d.id)
            """, ct, ("kind", kind));

        await Exec(conn, tx, """
            INSERT INTO index_sources (kind, synced_at, documents, last_error, failed_at)
            VALUES (@kind, now(), @n, NULL, NULL)
            ON CONFLICT (kind) DO UPDATE SET synced_at = now(), documents = @n, last_error = NULL, failed_at = NULL
            """, ct, ("kind", kind), ("n", rows.Count));

        await tx.CommitAsync(ct);
        return upserted + deleted;
    }

    public async Task RecordFailureAsync(string kind, string error, CancellationToken ct)
    {
        await using var cmd = index.CreateCommand("""
            INSERT INTO index_sources (kind, last_error, failed_at) VALUES (@kind, @error, now())
            ON CONFLICT (kind) DO UPDATE SET last_error = @error, failed_at = now()
            """);
        cmd.Parameters.AddWithValue("kind", kind);
        cmd.Parameters.AddWithValue("error", error);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Rebuilds the spelling vocabulary from the words of all indexed documents.</summary>
    public async Task RebuildTermsAsync(CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);

        var terms = new List<(string Term, int DocFreq)>();
        await using (var cmd = new NpgsqlCommand("""
            SELECT w, count(*)::int
            FROM (SELECT DISTINCT d.id, w
                  FROM search_documents d, unnest(string_to_array(d.norm_text, ' ')) AS w) words
            WHERE length(w) BETWEEN 2 AND 40 AND w !~ '^[0-9]+$'
            GROUP BY w
            """, conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) terms.Add((reader.GetString(0), reader.GetInt32(1)));

        await using var tx = await conn.BeginTransactionAsync(ct);
        await Exec(conn, tx, "DELETE FROM search_terms", ct);
        await using (var writer = await conn.BeginBinaryImportAsync(
                         "COPY search_terms (term, translit, doc_freq) FROM STDIN (FORMAT BINARY)", ct))
        {
            foreach (var (term, freq) in terms)
            {
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(term, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(TextNormalizer.Transliterate(term), NpgsqlDbType.Text, ct);
                await writer.WriteAsync(freq, NpgsqlDbType.Integer, ct);
            }
            await writer.CompleteAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    private static Task WriteNullable(NpgsqlBinaryImporter writer, string? value, CancellationToken ct) =>
        value is null ? writer.WriteNullAsync(ct) : writer.WriteAsync(value, NpgsqlDbType.Text, ct);

    private static async Task<int> Exec(NpgsqlConnection conn, NpgsqlTransaction tx, string sql, CancellationToken ct,
        params (string Name, object Value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
