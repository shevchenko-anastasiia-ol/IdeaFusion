using System.Globalization;
using System.Text;
using Dapper;
using Npgsql;
using SearchService.Indexing;
using SearchService.Normalization;

namespace SearchService.Search;

/// <summary>
/// Typo-tolerant search over search_documents. A query is matched in three ways at once:
/// <list type="number">
/// <item>every word is looked up in the index vocabulary (search_terms) as typed, in the other keyboard
/// layout and transliterated, and the closest known words are searched too ("tesr", "еуые" and "тест"
/// all reach "test");</item>
/// <item>full-text search with Ukrainian morphology ("ілюстраціями" finds "ілюстрація");</item>
/// <item>prefixes ("ілюс", "тест" for "тестування") and, for words the vocabulary does not know,
/// trigram word similarity with the document's head (title, subtitle, tags) in both scripts.</item>
/// </list>
/// </summary>
public sealed class SearchQueryService(NpgsqlDataSource index, TagCatalogCache catalogs)
{
    public const int MaxPageSize = 50;
    private const int MaxTokens = 8;
    private const int MaxQueryLength = 200;

    /// <summary>Minimal word similarity of the trigram leg (pg_trgm.word_similarity_threshold).</summary>
    private const double WordSimilarityThreshold = 0.5;

    /// <summary>
    /// Per search: the trigram threshold, and the page cost of an index that sits in memory or on SSD. With the
    /// default random_page_cost (4, spinning disks) Postgres reads the whole table for a common word instead of
    /// the full-text index, which on 50 000 documents is three times slower.
    /// </summary>
    private static readonly string SetSimilarityThreshold =
        "SET LOCAL pg_trgm.word_similarity_threshold = " + WordSimilarityThreshold.ToString(CultureInfo.InvariantCulture)
        + "; SET LOCAL random_page_cost = 1.1";

    /// <param name="Typed">The word as typed, normalized.</param>
    /// <param name="Swapped">The same keys in the other keyboard layout, normalized.</param>
    /// <param name="SearchForms">Words searched for this word: as typed plus the closest vocabulary words.</param>
    /// <param name="Correction">The vocabulary word to suggest instead, when the typed word looks misspelled.</param>
    /// <param name="Resolved">The vocabulary knows the word or a close one; otherwise it is matched by prefix and trigrams.</param>
    internal sealed record TokenPlan(string Typed, string? Swapped, IReadOnlyList<string> SearchForms, string? Correction, bool Resolved = true);

    internal sealed record QueryPlan(IReadOnlyList<TokenPlan> Tokens)
    {
        public string? DidYouMean =>
            Tokens.Any(t => t.Correction is not null)
                ? string.Join(' ', Tokens.Select(t => t.Correction ?? t.Typed))
                : null;

        /// <summary>What the trigram leg compares with documents: the corrected query.</summary>
        public string Phrase => string.Join(' ', Tokens.Select(t => t.Correction ?? t.Typed));

        /// <summary>
        /// Some word is unknown to the vocabulary (a prefix being typed, a rare misspelling), so the fuzzy trigram
        /// leg is needed. When every word resolves, full-text search over the vocabulary forms finds everything
        /// the trigram leg would, at a fraction of the cost.
        /// </summary>
        public bool NeedsFuzzy => Tokens.Any(t => !t.Resolved);
    }

    /// <param name="SameLexeme">The vocabulary word is an inflection of the typed word ("ілюстраціями" and "ілюстрація").</param>
    private sealed record Candidate(int Token, string Term, string Translit, int Dist, int DocFreq, bool SameLexeme);

    private const int TagFacetSize = 15;

    /// <summary>Known words this long also match as prefixes; shorter ones would match too much ("арт", "art").</summary>
    private const int MinPrefixLength = 4;

    /// <exception cref="InvalidCursorException">The cursor is malformed or belongs to another sort order.</exception>
    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var raw = Clip(request.Query);

        SearchCursor? cursor = null;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            cursor = SearchCursor.TryDecode(request.Cursor) ?? throw new InvalidCursorException("The cursor is not valid");
            if (cursor.Sort != request.Sort) throw new InvalidCursorException("The cursor belongs to another sort order");
        }
        var page = cursor is null ? Math.Max(1, request.Page) : 1;
        // When the listing started; carried by the cursor so that all its pages share it.
        var asOf = cursor?.AsOf ?? DateTime.UtcNow;

        await using var conn = await index.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            SetSimilarityThreshold,
            transaction: tx, cancellationToken: ct));

        var plan = await PlanAsync(conn, tx, raw, ct);
        var p = new DynamicParameters();
        var (query, exact, match, score) = BuildMatch(plan, p, fuzzy: !request.NoFuzzy);
        var filters = BuildFilters(request, await catalogs.GetAsync(ct), p);
        var where = $"({match})" + string.Concat(filters.Values.Select(f => " AND " + f));

        var sortKey = request.Sort switch
        {
            SearchSort.Relevance when plan.Tokens.Count > 0 => score,
            // Stored with each document (see SearchIndexWriter.PopularityKey) and indexed. A listing walks that
            // index; with words, "+ 0" keeps Postgres from walking it hoping to meet a rare word soon, and it
            // sorts the full-text matches instead.
            SearchSort.Popular => plan.Tokens.Count > 0 ? "d.popularity_key + 0" : "d.popularity_key",
            _ => "0",
        };
        p.Add("limit", pageSize + 1); // one more to know whether a next page exists
        p.Add("offset", (page - 1) * pageSize);
        p.Add("ck", cursor?.Key);
        p.Add("cc", cursor?.CreatedAt);
        p.Add("ci", cursor?.Id);

        var hitsSql = $"""
            WITH q AS (SELECT {query} AS tsq, {exact} AS exact),
            ranked AS (
                SELECT d.id, d.kind, d.source_id AS SourceId, d.title, d.subtitle, left(d.body, 240) AS Snippet,
                       d.tags, d.categories, d.image_url AS ImageUrl, d.owner_id AS OwnerId, d.team_id AS TeamId,
                       d.created_at AS CreatedAt, d.likes, d.saves, d.views, d.open_vacancies AS OpenVacancies,
                       ({score})::float8 AS Score, ({sortKey})::float8 AS SortKey
                FROM search_documents d, q
                WHERE {where}
            )
            SELECT * FROM ranked
            WHERE @ck::float8 IS NULL OR (SortKey, CreatedAt, id) < (@ck::float8, @cc::timestamptz, @ci::text)
            ORDER BY SortKey DESC, CreatedAt DESC, id DESC
            LIMIT @limit OFFSET @offset
            """;
        // Facets run at the same time on a connection of their own, so a search costs the slower of the two.
        var facetsTask = request.SkipFacets
            ? Task.FromResult(new SearchFacets())
            : FacetsOnOwnConnectionAsync(query, match, filters, new DynamicParameters(p), ct);
        var hits = (await conn.QueryAsync<SearchHit>(new CommandDefinition(hitsSql, p, tx, cancellationToken: ct))).ToList();

        string? next = null;
        if (hits.Count > pageSize)
        {
            hits.RemoveAt(hits.Count - 1);
            var last = hits[^1];
            next = new SearchCursor(request.Sort, last.SortKey, last.CreatedAt, last.Id, asOf).Encode();
        }

        await tx.CommitAsync(ct);
        var facets = await facetsTask;

        var kind = filters.ContainsKey("kind") ? request.Kind!.Trim().ToLowerInvariant() : null;
        return new SearchResponse
        {
            Query = raw,
            DidYouMean = plan.DidYouMean,
            Page = page,
            PageSize = pageSize,
            Sort = request.Sort,
            Total = request.SkipFacets ? hits.Count : kind is null ? facets.Kinds.Values.Sum() : facets.Kinds.GetValueOrDefault(kind),
            Facets = facets,
            Items = hits,
            NextCursor = next,
        };
    }

    private static readonly string[] FacetFilters = ["kind", "category", "tags"];

    private async Task<SearchFacets> FacetsOnOwnConnectionAsync(string query, string match,
        IReadOnlyDictionary<string, string> filters, DynamicParameters p, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SetSimilarityThreshold, transaction: tx, cancellationToken: ct));
        var facets = await FacetsAsync(conn, tx, query, match, filters, p, ct);
        await tx.CommitAsync(ct);
        return facets;
    }

    private sealed class FacetRow
    {
        public string Facet { get; init; } = "";
        public string Key { get; init; } = "";
        public string? Label { get; init; }
        public int N { get; init; }
    }

    /// <summary>
    /// Kind, category and tag counts; each ignores its own filter (disjunctive facets). The matching documents
    /// are read once, with a flag per facet filter, and all three facets are counted from that one pass.
    /// </summary>
    private static async Task<SearchFacets> FacetsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string query,
        string match, IReadOnlyDictionary<string, string> filters, DynamicParameters p, CancellationToken ct)
    {
        string Flag(string name) => filters.GetValueOrDefault(name, "TRUE");
        var where = $"({match})" + string.Concat(filters.Where(f => !FacetFilters.Contains(f.Key)).Select(f => " AND " + f.Value));
        var sql = $"""
            WITH q AS (SELECT {query} AS tsq),
            m AS MATERIALIZED (
                SELECT d.kind, d.categories, d.tag_keys,
                       ({Flag("kind")}) AS k_ok, ({Flag("category")}) AS c_ok, ({Flag("tags")}) AS t_ok
                FROM search_documents d, q
                WHERE {where}
            )
            SELECT 'kind' AS Facet, m.kind AS Key, NULL::text AS Label, count(*)::int AS N
            FROM m WHERE m.c_ok AND m.t_ok GROUP BY m.kind
            UNION ALL
            SELECT 'category', c, NULL, count(*)::int
            FROM m, unnest(m.categories) AS c WHERE m.k_ok AND m.t_ok GROUP BY c
            UNION ALL
            (SELECT 'tag', k, coalesce(t.label, k), count(*)::int AS n
             FROM m CROSS JOIN unnest(m.tag_keys) AS k
             LEFT JOIN search_tags t ON t.key = k
             WHERE m.k_ok AND m.c_ok
             GROUP BY k, t.label ORDER BY n DESC, k
             LIMIT {TagFacetSize})
            """;
        var rows = (await conn.QueryAsync<FacetRow>(new CommandDefinition(sql, p, tx, cancellationToken: ct))).ToList();
        return new SearchFacets
        {
            Kinds = rows.Where(r => r.Facet == "kind").ToDictionary(r => r.Key, r => r.N),
            Categories = rows.Where(r => r.Facet == "category").OrderByDescending(r => r.N).ThenBy(r => r.Key, StringComparer.Ordinal)
                .Select(r => new FacetValue(r.Key, r.N)).ToList(),
            Tags = rows.Where(r => r.Facet == "tag").OrderByDescending(r => r.N).ThenBy(r => r.Key, StringComparer.Ordinal)
                .Select(r => new TagFacet(r.Key, r.Label ?? r.Key, r.N)).ToList(),
        };
    }

    /// <summary>Completions of the last word from the vocabulary, matching tags and the most popular few matching documents.</summary>
    public async Task<SuggestResponse> SuggestAsync(string? query, int limit, CancellationToken ct)
    {
        var raw = Clip(query);
        var words = TextNormalizer.Tokenize(raw);
        if (words.Length == 0) return new SuggestResponse();

        var last = words[^1];
        var swapped = TextNormalizer.Normalize(TextNormalizer.SwapLayout(raw.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1]));
        var head = string.Join(' ', words[..^1]);
        limit = Math.Clamp(limit, 1, 20);

        // The three parts are independent, so they run at the same time on their own connections.
        var termsTask = CompletionsAsync(last, swapped, limit, ct);
        var tagsTask = TagsAsync(raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[^1], 5, ct);
        var documentsTask = SuggestDocumentsAsync(raw, words.Length > 1 ? last : null, ct);
        await Task.WhenAll(termsTask, tagsTask, documentsTask);

        return new SuggestResponse
        {
            Completions = (await termsTask).Select(t => head.Length == 0 ? t : $"{head} {t}").ToList(),
            Documents = await documentsTask,
            Tags = await tagsTask,
        };
    }

    private async Task<IEnumerable<string>> CompletionsAsync(string last, string swapped, int limit, CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        return await conn.QueryAsync<string>(new CommandDefinition("""
            SELECT term FROM search_terms
            WHERE term LIKE @prefix || '%' OR translit LIKE @latin || '%'
               OR (@swapped <> '' AND term LIKE @swapped || '%')
            ORDER BY (term = @prefix) DESC, doc_freq DESC, length(term), term
            LIMIT @limit
            """, new { prefix = last, latin = TextNormalizer.Transliterate(last), swapped, limit }, cancellationToken: ct));
    }

    /// <summary>The most popular few documents with all the words, or, failing that, with the word being typed.</summary>
    private async Task<IReadOnlyList<SearchHit>> SuggestDocumentsAsync(string raw, string? lastWord, CancellationToken ct)
    {
        SearchRequest Request(string q) => new() { Query = q, PageSize = 5, SkipFacets = true, NoFuzzy = true, Sort = SearchSort.Popular };
        var documents = await SearchAsync(Request(raw), ct);
        if (documents.Items.Count == 0 && lastWord is not null)
            documents = await SearchAsync(Request(lastWord), ct);
        return documents.Items;
    }

    /// <summary>
    /// Tag autocomplete. Matches the tag's own spellings and its synonyms by prefix, then by trigram
    /// similarity (so "ilustr", "ілюс" and "ілюстарція" all offer "illustration"), more used tags first.
    /// An empty query returns the most used tags.
    /// </summary>
    public async Task<IReadOnlyList<TagSuggestion>> TagsAsync(string? query, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 50);
        var keys = new[] { TextNormalizer.TagKey(query), TextNormalizer.TagKey(TextNormalizer.SwapLayout(query)) }
            .Where(k => k.Length > 0).Distinct().ToArray();

        await using var conn = await index.OpenConnectionAsync(ct);
        if (keys.Length == 0)
        {
            return (await conn.QueryAsync<TagSuggestion>(new CommandDefinition(
                "SELECT key, label, doc_count AS documents FROM search_tags ORDER BY doc_count DESC, label LIMIT @limit",
                new { limit }, cancellationToken: ct))).ToList();
        }

        return (await conn.QueryAsync<TagSuggestion>(new CommandDefinition("""
            SELECT t.key, t.label, t.doc_count AS documents
            FROM search_tags t
            JOIN (SELECT n.tag_key,
                         bool_or(n.name_key LIKE q.k || '%') AS prefix,
                         max(similarity(n.name_key, q.k))     AS sim
                  FROM unnest(@keys) AS q(k)
                  JOIN search_tag_names n ON n.name_key LIKE q.k || '%' OR n.name_key % q.k
                  GROUP BY n.tag_key) m ON m.tag_key = t.key
            ORDER BY m.prefix DESC, m.sim DESC, t.doc_count DESC, t.label
            LIMIT @limit
            """, new { keys = PgArray.Of(keys), limit }, cancellationToken: ct))).ToList();
    }

    /// <summary>Categories with the number of indexed documents in each, including empty ones.</summary>
    public async Task<IReadOnlyList<CategoryCount>> CategoriesAsync(CancellationToken ct)
    {
        await using var conn = await index.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CategoryCount>(new CommandDefinition("""
            SELECT c.category AS Category, count(d.id)::int AS Documents
            FROM (SELECT DISTINCT category FROM tag_categories) c
            LEFT JOIN search_documents d ON d.categories @> ARRAY[c.category]
            GROUP BY c.category
            ORDER BY Documents DESC, c.category
            """, cancellationToken: ct));
        return rows.ToList();
    }

    /// <summary>
    /// Role autocomplete for the role filter: roles in use whose words or their synonyms resemble the query
    /// in either script or keyboard layout. An empty query lists the most asked-for roles.
    /// </summary>
    public async Task<IReadOnlyList<RoleSuggestion>> RolesAsync(string? query, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 50);
        var forms = new[] { TextNormalizer.Normalize(query), TextNormalizer.Normalize(TextNormalizer.SwapLayout(query)) }
            .Where(f => f.Length > 0).Distinct().ToList();
        forms.AddRange(forms.Select(TextNormalizer.Transliterate).ToList());

        await using var conn = await index.OpenConnectionAsync(ct);
        const string columns = "label AS Label, vacancies AS Vacancies, teams AS Teams, people AS People";
        if (forms.Count == 0)
        {
            return (await conn.QueryAsync<RoleSuggestion>(new CommandDefinition(
                $"SELECT {columns} FROM search_roles ORDER BY vacancies DESC, teams + people DESC, label LIMIT @limit",
                new { limit }, cancellationToken: ct))).ToList();
        }

        return (await conn.QueryAsync<RoleSuggestion>(new CommandDefinition($"""
            SELECT {columns}
            FROM search_roles r
            CROSS JOIN LATERAL (SELECT max(word_similarity(f, r.role_text)) AS sim,
                                       bool_or(r.role_text LIKE f || '%' OR r.role_text LIKE '% ' || f || '%') AS prefix
                                FROM unnest(@forms) AS f) m
            WHERE m.prefix OR m.sim >= 0.5
            ORDER BY m.prefix DESC, m.sim DESC, r.vacancies DESC, r.teams + r.people DESC, r.label
            LIMIT @limit
            """, new { forms = PgArray.Of(forms.Distinct().ToArray()), limit }, cancellationToken: ct))).ToList();
    }

    /// <summary>Splits the query into words and finds the closest vocabulary words for each.</summary>
    internal static async Task<QueryPlan> PlanAsync(NpgsqlConnection conn, NpgsqlTransaction? tx, string raw, CancellationToken ct)
    {
        var words = SplitQuery(raw);
        if (words.Count == 0) return new QueryPlan([]);

        // One row per (word, form): the typed form and the other-layout form.
        var idx = new List<int>();
        var forms = new List<string>();
        var latins = new List<string>();
        var isTyped = new List<bool>();
        for (var i = 0; i < words.Count; i++)
        {
            var (typed, swapped) = words[i];
            if (!IsCorrectable(typed)) continue;
            foreach (var form in swapped is null ? [typed] : new[] { typed, swapped })
            {
                idx.Add(i);
                forms.Add(form);
                latins.Add(TextNormalizer.Transliterate(form));
                isTyped.Add(form == typed);
            }
        }

        var candidates = idx.Count == 0
            ? []
            : (await conn.QueryAsync<Candidate>(new CommandDefinition("""
                SELECT q.i AS Token, c.term AS Term, c.translit AS Translit, c.dist AS Dist, c.doc_freq AS DocFreq,
                       q.typed AND to_tsvector('ideafusion', c.term) @@ plainto_tsquery('ideafusion', q.form) AS SameLexeme
                FROM unnest(@idx, @forms, @latins, @typed) AS q(i, form, latin, typed)
                CROSS JOIN LATERAL (
                    SELECT t.term, t.translit, t.doc_freq,
                           least(levenshtein(t.term, q.form), levenshtein(t.translit, q.latin)) AS dist
                    FROM search_terms t
                    WHERE t.term % q.form OR t.translit % q.latin
                    ORDER BY dist, t.doc_freq DESC
                    LIMIT 3
                ) c
                """, new { idx = PgArray.Of(idx), forms = PgArray.Of(forms), latins = PgArray.Of(latins), typed = PgArray.Of(isTyped) }, tx,
                cancellationToken: ct))).ToList();

        var tokens = new List<TokenPlan>(words.Count);
        for (var i = 0; i < words.Count; i++)
        {
            var (typed, swapped) = words[i];
            var mine = candidates.Where(c => c.Token == i).ToList();
            // Short words and numbers are not corrected; full-text search alone handles them.
            tokens.Add(IsCorrectable(typed) ? Resolve(typed, swapped, mine) : new TokenPlan(typed, swapped, [typed], null));
        }
        return new QueryPlan(tokens);
    }

    private static TokenPlan Resolve(string typed, string? swapped, IReadOnlyList<Candidate> candidates)
    {
        var searchForms = new List<string> { typed };
        if (candidates.Count == 0) return new TokenPlan(typed, swapped, searchForms, null, Resolved: false);

        var bestDist = candidates.Min(c => c.Dist);
        if (bestDist > AllowedDistance(typed)) return new TokenPlan(typed, swapped, searchForms, null, Resolved: false);

        // Every equally close word is searched: "test" typed and both "test" and "тест" in the index.
        var best = candidates.Where(c => c.Dist == bestDist)
            .GroupBy(c => c.Term).Select(g => g.First())
            .OrderByDescending(c => c.DocFreq).ToList();
        foreach (var c in best)
            if (!searchForms.Contains(c.Term)) searchForms.Add(c.Term);

        // A word the index knows, in this or the other script, is not a typo: "тест" for "test" is not corrected.
        var typedLatin = TextNormalizer.Transliterate(typed);
        // Nor is an inflection of a known word: "ілюстраціями" while the index has "ілюстрація".
        var known = best.Any(c => c.Term == typed || c.Translit == typedLatin) || candidates.Any(c => c.SameLexeme);
        return new TokenPlan(typed, swapped, searchForms, known ? null : best[0].Term);
    }

    /// <summary>Edits allowed between a typed word and a vocabulary word: about 40% of its length, at least one.</summary>
    internal static int AllowedDistance(string typed) =>
        Math.Max(1, (int)(TextNormalizer.Transliterate(typed).Length * 0.4));

    private static bool IsCorrectable(string word) => word.Length >= 3 && !word.All(char.IsDigit);

    /// <summary>Normalized words of the query, each with its other-layout form when that is a single different word.</summary>
    internal static List<(string Typed, string? Swapped)> SplitQuery(string raw)
    {
        var result = new List<(string, string?)>();
        foreach (var rawWord in raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var typed = TextNormalizer.Tokenize(rawWord);
            if (typed.Length == 0)
            {
                // Only punctuation in this layout, but maybe letters in the other one (";'" is "жє").
                var only = TextNormalizer.Tokenize(TextNormalizer.SwapLayout(rawWord));
                if (only.Length == 1 && only[0].Length >= 2) result.Add((only[0], null));
                continue;
            }
            if (typed.Length == 1)
            {
                var swapped = TextNormalizer.Tokenize(TextNormalizer.SwapLayout(rawWord));
                result.Add((typed[0], swapped.Length == 1 && swapped[0] != typed[0] ? swapped[0] : null));
            }
            else
            {
                result.AddRange(typed.Select(w => (w, (string?)null)));
            }
        }
        return result.Take(MaxTokens).ToList();
    }

    /// <summary>
    /// SQL fragments of the query: the tsquery (every word must match in one of its forms or prefixes),
    /// the same without prefixes (exact words rank first), the match condition and the relevance score.
    /// Words go in as parameters.
    /// </summary>
    private static (string Query, string Exact, string Match, string Score) BuildMatch(QueryPlan plan, DynamicParameters p, bool fuzzy = true)
    {
        if (plan.Tokens.Count == 0) return ("NULL::tsquery", "NULL::tsquery", "TRUE", "0");

        var tsq = new StringBuilder();
        var exact = new StringBuilder();
        for (var i = 0; i < plan.Tokens.Count; i++)
        {
            var token = plan.Tokens[i];
            var words = new List<string>();
            for (var j = 0; j < token.SearchForms.Count; j++)
            {
                p.Add($"f{i}_{j}", token.SearchForms[j]);
                words.Add($"plainto_tsquery('ideafusion', @f{i}_{j})");
            }
            var all = new List<string>(words);
            if (!token.Resolved || token.Typed.Length >= MinPrefixLength)
            {
                // A word also matches as a prefix: an unknown one is often still being typed ("ілюс" reaches
                // "ілюстрація"), and a longer known one reaches its derivatives ("тест" finds "тестування",
                // which morphology keeps as a lemma of its own). Normalized words hold only letters and digits,
                // so they are safe as a tsquery lexeme.
                var prefixes = (token.Resolved ? [token.Typed] : new[] { token.Typed, token.Swapped }).OfType<string>().ToList();
                for (var j = 0; j < prefixes.Count; j++)
                {
                    p.Add($"x{i}_{j}", prefixes[j]);
                    all.Add($"to_tsquery('simple', @x{i}_{j} || ':*')");
                }
            }
            if (i > 0)
            {
                tsq.Append(" && ");
                exact.Append(" && ");
            }
            tsq.Append('(').AppendJoin(" || ", all).Append(')');
            exact.Append('(').AppendJoin(" || ", words).Append(')');
        }

        var phrase = plan.Phrase;
        p.Add("phrase", phrase);
        p.Add("latin", TextNormalizer.Transliterate(phrase));

        // Full-text rank in [0, 1) dominates, with a bonus when the words match exactly rather than as prefixes;
        // similarity orders fuzzy-only matches; popularity breaks near-ties. The trigram leg compares with the
        // short head only, and only when a word is unknown to the vocabulary.
        const string rank = "ts_rank_cd(d.fts, q.tsq, 32) + CASE WHEN d.fts @@ q.exact THEN 0.3 ELSE 0 END";
        const string popularity = "0.02 * ln(1 + d.likes + 2 * d.saves)";
        if (!fuzzy || !plan.NeedsFuzzy)
            return (tsq.ToString(), exact.ToString(), "d.fts @@ q.tsq", $"{rank} + {popularity}");

        const string match = "d.fts @@ q.tsq OR @phrase <% d.head_norm OR @latin <% d.head_translit";
        const string score = $"""
            {rank}
            + 0.5 * greatest(word_similarity(@phrase, d.head_norm), word_similarity(@latin, d.head_translit))
            + {popularity}
            """;
        return (tsq.ToString(), exact.ToString(), match, score);
    }

    /// <summary>The request's filters as named SQL conditions, so that a facet can leave its own one out.</summary>
    private static Dictionary<string, string> BuildFilters(SearchRequest request, TagCatalog catalog, DynamicParameters p)
    {
        var filters = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(request.Kind))
        {
            p.Add("kind", request.Kind.Trim().ToLowerInvariant());
            filters["kind"] = "d.kind = @kind";
        }
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            p.Add("category", request.Category.Trim());
            filters["category"] = "d.categories @> ARRAY[@category]::text[]";
        }
        // Any spelling or synonym of a tag selects the same documents: "#3D", "3-д" and "3d" are one tag.
        var tags = request.Tags.Select(catalog.Key).Where(k => k.Length > 0).Distinct().ToArray();
        if (tags.Length > 0)
        {
            p.Add("tags", PgArray.Of(tags));
            filters["tags"] = request.TagMode == TagMatch.All ? "d.tag_keys @> @tags" : "d.tag_keys && @tags";
        }
        if (request.From is { } from)
        {
            p.Add("from", DateTime.SpecifyKind(from, from.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : from.Kind).ToUniversalTime());
            filters["from"] = "d.created_at >= @from";
        }
        if (request.To is { } to)
        {
            p.Add("to", DateTime.SpecifyKind(to, to.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : to.Kind).ToUniversalTime());
            filters["to"] = "d.created_at < @to";
        }
        if (request.MinLikes is > 0)
        {
            p.Add("minLikes", request.MinLikes.Value);
            filters["minLikes"] = "d.likes >= @minLikes";
        }
        if (request.HasOpenVacancies)
            filters["openVacancies"] = "(d.kind <> 'team' OR d.open_vacancies > 0)";

        var role = TextNormalizer.Normalize(request.Role);
        if (role.Length > 0)
        {
            // Fuzzy, like the query, and widened by the role directory: "аніматор" matches "Аніматор 2D",
            // "ilustrator" matches "Illustrator", "дизайнер" matches "Motion Designer". A team also matches
            // by the roles of its open vacancies: "a team looking for an illustrator".
            p.Add("role", role);
            p.Add("roleLatin", TextNormalizer.Transliterate(role));
            // IN-subqueries rather than a per-row EXISTS: each runs once through the trigram index on role_text.
            filters["role"] = """
                (d.id IN (SELECT r.id FROM search_documents r WHERE @role <% r.role_text OR @roleLatin <% r.role_text)
                 OR (d.kind = 'team' AND d.team_id IN (
                     SELECT v.team_id FROM search_documents v
                     WHERE v.kind = 'vacancy' AND (@role <% v.role_text OR @roleLatin <% v.role_text))))
                """;
        }
        if (!string.IsNullOrWhiteSpace(request.AuthorId))
        {
            p.Add("author", request.AuthorId.Trim());
            filters["author"] = "d.owner_id = @author";
        }
        if (!string.IsNullOrWhiteSpace(request.TeamId))
        {
            p.Add("team", request.TeamId.Trim());
            filters["team"] = "d.team_id = @team";
        }
        if (request.IndexedAfter is { } after)
        {
            p.Add("indexedAfter", DateTime.SpecifyKind(after, DateTimeKind.Utc));
            filters["indexedAfter"] = "d.first_indexed_at > @indexedAfter";
        }
        if (!string.IsNullOrWhiteSpace(request.ExcludeOwnerId))
        {
            p.Add("excludeOwner", request.ExcludeOwnerId);
            filters["excludeOwner"] = "d.owner_id IS DISTINCT FROM @excludeOwner";
        }
        return filters;
    }

    private static string Clip(string? query)
    {
        var raw = (query ?? "").Trim();
        return raw.Length > MaxQueryLength ? raw[..MaxQueryLength] : raw;
    }
}
