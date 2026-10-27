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
/// <item>trigram word similarity on the normalized and on the transliterated text, for words the
/// vocabulary does not know.</item>
/// </list>
/// </summary>
public sealed class SearchQueryService(NpgsqlDataSource index, TagCatalogCache catalogs)
{
    public const int MaxPageSize = 50;
    private const int MaxTokens = 8;
    private const int MaxQueryLength = 200;

    /// <summary>Minimal word similarity of the trigram leg (pg_trgm.word_similarity_threshold).</summary>
    private const double WordSimilarityThreshold = 0.5;

    /// <param name="Typed">The word as typed, normalized.</param>
    /// <param name="Swapped">The same keys in the other keyboard layout, normalized.</param>
    /// <param name="SearchForms">Words searched for this word: as typed plus the closest vocabulary words.</param>
    /// <param name="Correction">The vocabulary word to suggest instead, when the typed word looks misspelled.</param>
    internal sealed record TokenPlan(string Typed, string? Swapped, IReadOnlyList<string> SearchForms, string? Correction);

    internal sealed record QueryPlan(IReadOnlyList<TokenPlan> Tokens)
    {
        public string? DidYouMean =>
            Tokens.Any(t => t.Correction is not null)
                ? string.Join(' ', Tokens.Select(t => t.Correction ?? t.Typed))
                : null;

        /// <summary>What the trigram leg compares with documents: the corrected query.</summary>
        public string Phrase => string.Join(' ', Tokens.Select(t => t.Correction ?? t.Typed));
    }

    /// <param name="SameLexeme">The vocabulary word is an inflection of the typed word ("ілюстраціями" and "ілюстрація").</param>
    private sealed record Candidate(int Token, string Term, string Translit, int Dist, int DocFreq, bool SameLexeme);

    private const int TagFacetSize = 15;

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
        // The popularity decay is computed against one instant for all pages of a listing.
        var asOf = cursor?.AsOf ?? DateTime.UtcNow;

        await using var conn = await index.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "SET LOCAL pg_trgm.word_similarity_threshold = " + WordSimilarityThreshold.ToString(CultureInfo.InvariantCulture),
            transaction: tx, cancellationToken: ct));

        var plan = await PlanAsync(conn, tx, raw, ct);
        var p = new DynamicParameters();
        var (query, match, score) = BuildMatch(plan, p);
        var filters = BuildFilters(request, await catalogs.GetAsync(ct), p);
        string Where(string? except = null) =>
            $"({match})" + string.Concat(filters.Where(f => f.Key != except).Select(f => " AND " + f.Value));

        var sortKey = request.Sort switch
        {
            SearchSort.Relevance when plan.Tokens.Count > 0 => score,
            SearchSort.Popular => """
                (d.views + 3 * d.likes + 4 * d.saves + 2 * d.open_vacancies)
                * exp(-ln(2) * greatest(extract(epoch FROM (@asOf - d.created_at)), 0) / 86400 / 14)
                """,
            _ => "0",
        };
        p.Add("asOf", asOf);
        p.Add("limit", pageSize + 1); // one more to know whether a next page exists
        p.Add("offset", (page - 1) * pageSize);
        p.Add("ck", cursor?.Key);
        p.Add("cc", cursor?.CreatedAt);
        p.Add("ci", cursor?.Id);

        var hitsSql = $"""
            WITH q AS (SELECT {query} AS tsq),
            ranked AS (
                SELECT d.id, d.kind, d.source_id AS SourceId, d.title, d.subtitle, left(d.body, 240) AS Snippet,
                       d.tags, d.categories, d.image_url AS ImageUrl, d.owner_id AS OwnerId, d.team_id AS TeamId,
                       d.created_at AS CreatedAt, d.likes, d.saves, d.views, d.open_vacancies AS OpenVacancies,
                       ({score})::float8 AS Score, ({sortKey})::float8 AS SortKey
                FROM search_documents d, q
                WHERE {Where()}
            )
            SELECT * FROM ranked
            WHERE @ck::float8 IS NULL OR (SortKey, CreatedAt, id) < (@ck::float8, @cc::timestamptz, @ci::text)
            ORDER BY SortKey DESC, CreatedAt DESC, id DESC
            LIMIT @limit OFFSET @offset
            """;
        var hits = (await conn.QueryAsync<SearchHit>(new CommandDefinition(hitsSql, p, tx, cancellationToken: ct))).ToList();

        string? next = null;
        if (hits.Count > pageSize)
        {
            hits.RemoveAt(hits.Count - 1);
            var last = hits[^1];
            next = new SearchCursor(request.Sort, last.SortKey, last.CreatedAt, last.Id, asOf).Encode();
        }

        var facets = await FacetsAsync(conn, tx, query, Where, p, ct);
        await tx.CommitAsync(ct);

        var kind = filters.ContainsKey("kind") ? request.Kind!.Trim().ToLowerInvariant() : null;
        return new SearchResponse
        {
            Query = raw,
            DidYouMean = plan.DidYouMean,
            Page = page,
            PageSize = pageSize,
            Sort = request.Sort,
            Total = kind is null ? facets.Kinds.Values.Sum() : facets.Kinds.GetValueOrDefault(kind),
            Facets = facets,
            Items = hits,
            NextCursor = next,
        };
    }

    /// <summary>Kind, category and tag counts; each ignores its own filter (disjunctive facets).</summary>
    private static async Task<SearchFacets> FacetsAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string query,
        Func<string?, string> where, DynamicParameters p, CancellationToken ct)
    {
        var sql = $"""
            WITH q AS (SELECT {query} AS tsq)
            SELECT d.kind, count(*)::int FROM search_documents d, q WHERE {where("kind")} GROUP BY d.kind;

            WITH q AS (SELECT {query} AS tsq)
            SELECT c, count(*)::int AS n
            FROM search_documents d, q, unnest(d.categories) AS c
            WHERE {where("category")}
            GROUP BY c ORDER BY n DESC, c;

            WITH q AS (SELECT {query} AS tsq)
            SELECT k, coalesce(t.label, k), count(*)::int AS n
            FROM search_documents d, q, unnest(d.tag_keys) AS k
            LEFT JOIN search_tags t ON t.key = k
            WHERE {where("tags")}
            GROUP BY k, t.label ORDER BY n DESC, k
            LIMIT {TagFacetSize};
            """;
        await using var grid = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, tx, cancellationToken: ct));
        var kinds = (await grid.ReadAsync<(string Kind, int N)>()).ToDictionary(x => x.Kind, x => x.N);
        var categories = (await grid.ReadAsync<(string Value, int Count)>()).Select(x => new FacetValue(x.Value, x.Count)).ToList();
        var tags = (await grid.ReadAsync<(string Key, string Label, int Count)>()).Select(x => new TagFacet(x.Key, x.Label, x.Count)).ToList();
        return new SearchFacets { Kinds = kinds, Categories = categories, Tags = tags };
    }

    /// <summary>Completions of the last word from the vocabulary and the best few documents.</summary>
    public async Task<SuggestResponse> SuggestAsync(string? query, int limit, CancellationToken ct)
    {
        var raw = Clip(query);
        var words = TextNormalizer.Tokenize(raw);
        if (words.Length == 0) return new SuggestResponse();

        var last = words[^1];
        var swapped = TextNormalizer.Normalize(TextNormalizer.SwapLayout(raw.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1]));
        var head = string.Join(' ', words[..^1]);
        limit = Math.Clamp(limit, 1, 20);

        await using var conn = await index.OpenConnectionAsync(ct);
        var terms = await conn.QueryAsync<string>(new CommandDefinition("""
            SELECT term FROM search_terms
            WHERE term LIKE @prefix || '%' OR translit LIKE @latin || '%'
               OR (@swapped <> '' AND term LIKE @swapped || '%')
            ORDER BY (term = @prefix) DESC, doc_freq DESC, length(term), term
            LIMIT @limit
            """, new { prefix = last, latin = TextNormalizer.Transliterate(last), swapped, limit }, cancellationToken: ct));

        var tags = await TagsAsync(raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[^1], 5, ct);
        var documents = await SearchAsync(new SearchRequest { Query = raw, PageSize = 5 }, ct);
        return new SuggestResponse
        {
            Completions = terms.Select(t => head.Length == 0 ? t : $"{head} {t}").ToList(),
            Documents = documents.Items,
            Tags = tags,
        };
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
            tokens.Add(Resolve(typed, swapped, mine));
        }
        return new QueryPlan(tokens);
    }

    private static TokenPlan Resolve(string typed, string? swapped, IReadOnlyList<Candidate> candidates)
    {
        var searchForms = new List<string> { typed };
        if (candidates.Count == 0) return new TokenPlan(typed, swapped, searchForms, null);

        var bestDist = candidates.Min(c => c.Dist);
        if (bestDist > AllowedDistance(typed)) return new TokenPlan(typed, swapped, searchForms, null);

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
    /// SQL fragments of the query: the tsquery (every word must match in one of its forms),
    /// the match condition and the relevance score. Words go in as parameters.
    /// </summary>
    private static (string Query, string Match, string Score) BuildMatch(QueryPlan plan, DynamicParameters p)
    {
        if (plan.Tokens.Count == 0) return ("NULL::tsquery", "TRUE", "0");

        var tsq = new StringBuilder();
        for (var i = 0; i < plan.Tokens.Count; i++)
        {
            if (i > 0) tsq.Append(" && ");
            tsq.Append('(');
            var forms = plan.Tokens[i].SearchForms;
            for (var j = 0; j < forms.Count; j++)
            {
                if (j > 0) tsq.Append(" || ");
                var name = $"f{i}_{j}";
                p.Add(name, forms[j]);
                tsq.Append($"plainto_tsquery('ideafusion', @{name})");
            }
            tsq.Append(')');
        }

        var phrase = plan.Phrase;
        p.Add("phrase", phrase);
        p.Add("latin", TextNormalizer.Transliterate(phrase));

        const string match = "d.fts @@ q.tsq OR @phrase <% d.norm_text OR @latin <% d.translit_text";
        // Full-text rank in [0, 1) dominates; similarity orders fuzzy-only matches; popularity breaks near-ties.
        const string score = """
            ts_rank_cd(d.fts, q.tsq, 32)
            + 0.5 * greatest(word_similarity(@phrase, d.norm_text), word_similarity(@latin, d.translit_text))
            + 0.02 * ln(1 + d.likes + 2 * d.saves)
            """;
        return (tsq.ToString(), match, score);
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
            filters["role"] = """
                (@role <% d.role_text OR @roleLatin <% d.role_text
                 OR (d.kind = 'team' AND EXISTS (
                     SELECT 1 FROM search_documents v
                     WHERE v.kind = 'vacancy' AND v.team_id = d.team_id
                       AND (@role <% v.role_text OR @roleLatin <% v.role_text))))
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
        return filters;
    }

    private static string Clip(string? query)
    {
        var raw = (query ?? "").Trim();
        return raw.Length > MaxQueryLength ? raw[..MaxQueryLength] : raw;
    }
}
