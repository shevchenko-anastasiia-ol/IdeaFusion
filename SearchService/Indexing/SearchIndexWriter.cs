using Npgsql;
using NpgsqlTypes;
using SearchService.Normalization;

namespace SearchService.Indexing;

/// <summary>A search_documents row: the source fields plus everything derived for matching.</summary>
public sealed record IndexRow(
    SourceDocument Source,
    string[] TagKeys,
    string[] Categories,
    string TagText,
    string RoleText,
    string NormText,
    string TranslitText);

/// <summary>Writes snapshots of the sources into searchdb.</summary>
public sealed class SearchIndexWriter(NpgsqlDataSource index)
{
    public Task<TagCatalog> LoadTagCatalogAsync(CancellationToken ct) => TagCatalog.LoadAsync(index, ct);

    public static IndexRow BuildRow(SourceDocument doc, TagCatalog catalog)
    {
        var keys = doc.Tags.Select(catalog.Key).Where(k => k.Length > 0).Distinct().ToArray();

        var categories = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var tag in doc.Tags.Concat(doc.CategoryHints))
            categories.UnionWith(catalog.Categories(tag));

        // The tag weight of the tsvector also carries the synonyms and categories of the tags,
        // so a post tagged "illustration" is found by "ілюстрація" and by "Арт".
        var tagText = string.Join(", ", doc.Tags
            .Concat(keys.SelectMany(catalog.Synonyms))
            .Concat(categories)
            .Distinct(StringComparer.OrdinalIgnoreCase));
        var norm = TextNormalizer.Normalize(string.Join(' ', doc.Title, doc.Subtitle, tagText, doc.Body));
        return new IndexRow(doc, keys, categories.ToArray(), tagText, RoleText(doc.Roles, catalog), norm, TextNormalizer.Transliterate(norm));
    }

    /// <summary>
    /// Text the fuzzy role filter compares with: each role keeps its own words in order, then their synonyms
    /// from the role directory; both scripts go in one string, so "дизайнер", "designer" and "dyzainer" all match.
    /// </summary>
    public static string RoleText(IEnumerable<string> roles, TagCatalog catalog)
    {
        var norm = string.Join(' ', roles.Select(r => string.Join(' ', TextNormalizer.Tokenize(r).Concat(catalog.RoleWords(r)).Distinct())));
        return norm.Length == 0 ? "" : $"{norm} {TextNormalizer.Transliterate(norm)}";
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
                tags text[], tag_keys text[], categories text[], tag_text text, roles text[], role_text text, image_url text, owner_id text, team_id text,
                created_at timestamptz, likes int, saves int, views int, open_vacancies int,
                norm_text text, translit_text text
            ) ON COMMIT DROP
            """, ct);

        await using (var writer = await conn.BeginBinaryImportAsync("""
            COPY incoming (id, kind, source_id, title, subtitle, body, tags, tag_keys, categories, tag_text, roles, role_text, image_url,
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
                await writer.WriteAsync(row.TagKeys, NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                await writer.WriteAsync(row.Categories, NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                await writer.WriteAsync(row.TagText, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(d.Roles.ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                await writer.WriteAsync(row.RoleText, NpgsqlDbType.Text, ct);
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
            INSERT INTO search_documents AS d (id, kind, source_id, title, subtitle, body, tags, tag_keys, categories, tag_text, roles, role_text,
                image_url, owner_id, team_id, created_at, likes, saves, views, open_vacancies, norm_text, translit_text)
            SELECT id, kind, source_id, title, subtitle, body, tags, tag_keys, categories, tag_text, roles, role_text,
                image_url, owner_id, team_id, created_at, likes, saves, views, open_vacancies, norm_text, translit_text
            FROM incoming
            ON CONFLICT (id) DO UPDATE SET
                title = EXCLUDED.title, subtitle = EXCLUDED.subtitle, body = EXCLUDED.body,
                tags = EXCLUDED.tags, tag_keys = EXCLUDED.tag_keys, categories = EXCLUDED.categories, tag_text = EXCLUDED.tag_text,
                roles = EXCLUDED.roles, role_text = EXCLUDED.role_text,
                image_url = EXCLUDED.image_url, owner_id = EXCLUDED.owner_id, team_id = EXCLUDED.team_id,
                created_at = EXCLUDED.created_at, likes = EXCLUDED.likes, saves = EXCLUDED.saves,
                views = EXCLUDED.views, open_vacancies = EXCLUDED.open_vacancies,
                norm_text = EXCLUDED.norm_text, translit_text = EXCLUDED.translit_text, indexed_at = now()
            WHERE (d.title, d.subtitle, d.body, d.tags, d.tag_keys, d.roles, d.categories, d.tag_text, d.role_text, d.image_url, d.owner_id, d.team_id,
                   d.created_at, d.likes, d.saves, d.views, d.open_vacancies)
                  IS DISTINCT FROM
                  (EXCLUDED.title, EXCLUDED.subtitle, EXCLUDED.body, EXCLUDED.tags, EXCLUDED.tag_keys, EXCLUDED.roles, EXCLUDED.categories, EXCLUDED.tag_text, EXCLUDED.role_text,
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

    /// <summary>
    /// Rebuilds the tags in use (search_tags) and the names that lead to them (search_tag_names):
    /// one tag per canonical key, labelled by its synonym group or by its most common spelling.
    /// </summary>
    public async Task RebuildTagsAsync(TagCatalog catalog, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);

        var docCount = new Dictionary<string, int>();
        var spellings = new Dictionary<string, Dictionary<string, int>>();
        await using (var cmd = new NpgsqlCommand("SELECT tags FROM search_documents WHERE cardinality(tags) > 0", conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var seen = new HashSet<string>();
                foreach (var tag in reader.GetFieldValue<string[]>(0))
                {
                    var key = catalog.Key(tag);
                    if (key.Length == 0) continue;
                    if (!spellings.TryGetValue(key, out var forms)) spellings[key] = forms = [];
                    forms[tag.Trim()] = forms.GetValueOrDefault(tag.Trim()) + 1;
                    if (seen.Add(key)) docCount[key] = docCount.GetValueOrDefault(key) + 1;
                }
            }
        }

        await using var tx = await conn.BeginTransactionAsync(ct);
        await Exec(conn, tx, "DELETE FROM search_tags", ct);
        await using (var writer = await conn.BeginBinaryImportAsync(
                         "COPY search_tags (key, label, doc_count) FROM STDIN (FORMAT BINARY)", ct))
        {
            foreach (var (key, count) in docCount)
            {
                var label = catalog.Label(key)
                            ?? spellings[key].OrderByDescending(f => f.Value).ThenBy(f => f.Key, StringComparer.Ordinal).First().Key;
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(key, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(label, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(count, NpgsqlDbType.Integer, ct);
            }
            await writer.CompleteAsync(ct);
        }
        await using (var writer = await conn.BeginBinaryImportAsync(
                         "COPY search_tag_names (name_key, tag_key) FROM STDIN (FORMAT BINARY)", ct))
        {
            foreach (var key in docCount.Keys)
            {
                var names = catalog.NameKeys(key)
                    .Concat(spellings[key].Keys.Select(TextNormalizer.TagKey))
                    .Where(n => n.Length > 0)
                    .Distinct();
                foreach (var name in names)
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(name, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(key, NpgsqlDbType.Text, ct);
                }
            }
            await writer.CompleteAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    /// <summary>Rebuilds the roles in use (search_roles) from vacancies, teams and people.</summary>
    public async Task RebuildRolesAsync(TagCatalog catalog, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);

        var roles = new Dictionary<string, RoleUse>();
        await using (var cmd = new NpgsqlCommand("SELECT kind, roles FROM search_documents WHERE cardinality(roles) > 0", conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var kind = reader.GetString(0);
                foreach (var role in reader.GetFieldValue<string[]>(1).Select(r => r.Trim()).Distinct())
                {
                    var key = string.Join(' ', TextNormalizer.Tokenize(role));
                    if (key.Length == 0) continue;
                    if (!roles.TryGetValue(key, out var r)) roles[key] = r = new RoleUse();
                    r.Spellings[role] = r.Spellings.GetValueOrDefault(role) + 1;
                    if (kind == DocumentKinds.Vacancy) r.Vacancies++;
                    else if (kind == DocumentKinds.Team) r.Teams++;
                    else r.People++;
                }
            }
        }

        await using var tx = await conn.BeginTransactionAsync(ct);
        await Exec(conn, tx, "DELETE FROM search_roles", ct);
        await using (var writer = await conn.BeginBinaryImportAsync(
                         "COPY search_roles (key, label, role_text, vacancies, teams, people) FROM STDIN (FORMAT BINARY)", ct))
        {
            foreach (var (key, r) in roles)
            {
                var label = r.Spellings.OrderByDescending(s => s.Value).ThenBy(s => s.Key, StringComparer.Ordinal).First().Key;
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(key, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(label, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(RoleText([label], catalog), NpgsqlDbType.Text, ct);
                await writer.WriteAsync(r.Vacancies, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(r.Teams, NpgsqlDbType.Integer, ct);
                await writer.WriteAsync(r.People, NpgsqlDbType.Integer, ct);
            }
            await writer.CompleteAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    private sealed class RoleUse
    {
        public Dictionary<string, int> Spellings { get; } = [];
        public int Vacancies, Teams, People;
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
