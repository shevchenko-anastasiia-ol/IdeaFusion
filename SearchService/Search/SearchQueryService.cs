using System.Globalization;
using System.Text;
using Dapper;
using Npgsql;
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
public sealed class SearchQueryService(NpgsqlDataSource index)
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

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var raw = Clip(request.Query);
        var kind = string.IsNullOrWhiteSpace(request.Kind) ? null : request.Kind.Trim().ToLowerInvariant();

        await using var conn = await index.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "SET LOCAL pg_trgm.word_similarity_threshold = " + WordSimilarityThreshold.ToString(CultureInfo.InvariantCulture),
            transaction: tx, cancellationToken: ct));

        var plan = await PlanAsync(conn, tx, raw, ct);
        var p = new DynamicParameters();
        var (query, match, score) = BuildMatch(plan, p);
        var filters = BuildFilters(request, p);
        p.Add("kind", kind);
        p.Add("limit", pageSize);
        p.Add("offset", (page - 1) * pageSize);

        var hitsSql = $"""
            WITH q AS (SELECT {query} AS tsq)
            SELECT d.id, d.kind, d.source_id AS SourceId, d.title, d.subtitle, left(d.body, 240) AS Snippet,
                   d.tags, d.categories, d.image_url AS ImageUrl, d.owner_id AS OwnerId, d.team_id AS TeamId,
                   d.created_at AS CreatedAt, d.likes, d.saves, d.views, d.open_vacancies AS OpenVacancies,
                   ({score})::float8 AS Score
            FROM search_documents d, q
            WHERE ({match}) {filters} AND (@kind::text IS NULL OR d.kind = @kind)
            ORDER BY Score DESC, d.created_at DESC, d.id
            LIMIT @limit OFFSET @offset
            """;
        var hits = (await conn.QueryAsync<SearchHit>(new CommandDefinition(hitsSql, p, tx, cancellationToken: ct))).ToList();

        var countsSql = $"""
            WITH q AS (SELECT {query} AS tsq)
            SELECT d.kind, count(*)::int
            FROM search_documents d, q
            WHERE ({match}) {filters}
            GROUP BY d.kind
            """;
        var counts = (await conn.QueryAsync<(string Kind, int N)>(new CommandDefinition(countsSql, p, tx, cancellationToken: ct)))
            .ToDictionary(c => c.Kind, c => c.N);

        await tx.CommitAsync(ct);

        return new SearchResponse
        {
            Query = raw,
            DidYouMean = plan.DidYouMean,
            Page = page,
            PageSize = pageSize,
            Total = kind is null ? counts.Values.Sum() : counts.GetValueOrDefault(kind),
            KindCounts = counts,
            Items = hits,
        };
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

        var documents = await SearchAsync(new SearchRequest { Query = raw, PageSize = 5 }, ct);
        return new SuggestResponse
        {
            Completions = terms.Select(t => head.Length == 0 ? t : $"{head} {t}").ToList(),
            Documents = documents.Items,
        };
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

    private static string BuildFilters(SearchRequest request, DynamicParameters p)
    {
        var sql = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            p.Add("category", request.Category.Trim());
            sql.Append(" AND d.categories @> ARRAY[@category]::text[]");
        }
        var tags = request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToArray();
        if (tags.Length > 0)
        {
            p.Add("tags", PgArray.Of(tags));
            sql.Append(" AND d.tags && @tags");
        }
        return sql.ToString();
    }

    private static string Clip(string? query)
    {
        var raw = (query ?? "").Trim();
        return raw.Length > MaxQueryLength ? raw[..MaxQueryLength] : raw;
    }
}
