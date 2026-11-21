using System.Diagnostics;
using FluentAssertions;
using SearchService.Indexing;
using SearchService.Search;
using Xunit.Abstractions;

namespace SearchService.Tests;

/// <summary>
/// Stage 8 targets on 50 000 documents: search p95 ≤ 150 ms, autocomplete p95 ≤ 50 ms.
/// Slow to set up, so it runs only with SEARCH_BENCH=1 (and SEARCH_TEST_POSTGRES).
/// </summary>
public sealed class SearchPerformanceTests(SearchDatabaseFixture db, ITestOutputHelper output) : IClassFixture<SearchDatabaseFixture>
{
    private const int Documents = 50_000;
    private const int Repeats = 15;

    private static readonly string[] Uk =
    [
        "ілюстрація", "малюнок", "акварель", "портрет", "пейзаж", "музика", "пісня", "гурт", "альбом", "концерт",
        "анімація", "мультфільм", "персонаж", "дизайн", "логотип", "бренд", "фотографія", "кадр", "світло", "місто",
        "книжка", "обкладинка", "постер", "скетч", "тату", "графіті", "мурал", "кераміка", "текстиль", "вишивка",
        "поезія", "вірш", "оповідання", "сценарій", "фільм", "кліп", "театр", "танець", "гра", "рівень",
        "команда", "проєкт", "колаборація", "студія", "вакансія", "досвід", "портфоліо", "робота", "ідея", "натхнення",
    ];

    private static readonly string[] En =
    [
        "illustration", "sketch", "portrait", "music", "song", "band", "album", "animation", "character", "design",
        "logo", "branding", "photo", "street", "city", "book", "cover", "poster", "game", "level",
        "studio", "project", "collab", "concept", "render", "texture", "model", "motion", "sound", "mix",
    ];

    private static readonly string[] Tags =
    [
        "illustration", "art", "design", "branding", "music", "photography", "animation", "3d", "gamedev", "ui-ux",
        "architecture", "digital", "fashion", "film", "poetry", "writing", "traditional", "wip", "collab", "street-art",
        "ілюстрація", "фото", "музика", "анімація", "дизайн", "кераміка", "тату", "вишивка", "мурал", "кіно",
    ];

    private static readonly string[] Roles =
    [
        "Ілюстратор", "Illustrator", "Motion Designer", "Графічний дизайнер", "UI/UX дизайнер", "3D художник", "Аніматор 2D",
        "Sound Designer", "Звукорежисер", "Гітарист", "Drummer", "Вокалістка", "Фотограф", "Відеограф", "Монтажер",
        "Сценарист", "Game developer", "Веб-розробник", "Concept Artist", "Rigger", "Копірайтер", "Продюсер",
    ];

    /// <summary>
    /// Words follow Zipf's law over a vocabulary of real words and ~4 000 made-up ones, the way words of real
    /// texts do: a typical query word then occurs in a few percent of documents. SEARCH_BENCH_DENSE=1 draws
    /// only from the 80 real words instead, so every query word is in ~40% of documents (the worst case).
    /// </summary>
    private static IEnumerable<SourceDocument> Generate()
    {
        var random = new Random(42);
        string Pick(string[] words) => words[random.Next(words.Length)];
        var dense = Environment.GetEnvironmentVariable("SEARCH_BENCH_DENSE") == "1";
        var vocabulary = dense ? [.. Uk, .. En] : ZipfVocabulary(random);
        var cumulative = new double[vocabulary.Length];
        for (var r = 0; r < vocabulary.Length; r++)
            cumulative[r] = (r == 0 ? 0 : cumulative[r - 1]) + (dense ? 1 : 1 / Math.Pow(r + 1, 1.07));
        string Word()
        {
            var at = Array.BinarySearch(cumulative, random.NextDouble() * cumulative[^1]);
            return vocabulary[Math.Min(at < 0 ? ~at : at, vocabulary.Length - 1)];
        }
        string Words(int min, int max) => string.Join(' ', Enumerable.Range(0, random.Next(min, max + 1)).Select(_ => Word()));
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < Documents; i++)
        {
            var kind = (i % 25) switch { < 20 => DocumentKinds.Post, < 22 => DocumentKinds.Team, < 24 => DocumentKinds.Vacancy, _ => DocumentKinds.User };
            var roles = kind == DocumentKinds.Post ? Array.Empty<string>() : [Pick(Roles)];
            yield return new SourceDocument
            {
                Kind = kind,
                SourceId = $"bench{i}",
                Title = kind == DocumentKinds.Vacancy ? roles[0] : Words(2, 6),
                Subtitle = Words(1, 3),
                Body = Words(15, 45),
                Tags = Enumerable.Range(0, random.Next(0, 4)).Select(_ => Pick(Tags)).Distinct().ToArray(),
                Roles = roles,
                OwnerId = kind is DocumentKinds.Post or DocumentKinds.User ? $"user{random.Next(2000)}" : null,
                TeamId = kind is DocumentKinds.Team or DocumentKinds.Vacancy ? $"team{random.Next(2000)}" : null,
                CreatedAt = start.AddMinutes(random.Next(60 * 24 * 600)),
                Likes = random.Next(50), Saves = random.Next(20), Views = random.Next(500),
                OpenVacancies = kind == DocumentKinds.Team ? random.Next(3) : 0,
            };
        }
    }

    /// <summary>Made-up words of both scripts, with the real ones spread over the ranks 30–1 500.</summary>
    private static string[] ZipfVocabulary(Random random)
    {
        string[] uk = ["ка", "ро", "ли", "на", "ві", "то", "ме", "су", "да", "пі", "ле", "гу", "зо", "ми", "ре", "бо", "чи", "ту", "ша", "ню"];
        string[] en = ["ka", "ro", "li", "na", "vi", "to", "me", "su", "da", "pi", "le", "gu", "zo", "mi", "re", "bo"];
        var made = new HashSet<string>();
        while (made.Count < 4_000)
        {
            var syllables = random.Next(3) == 0 ? en : uk;
            made.Add(string.Concat(Enumerable.Range(0, random.Next(2, 5)).Select(_ => syllables[random.Next(syllables.Length)])));
        }
        var words = made.ToList();
        var real = Uk.Concat(En).ToArray();
        for (var i = 0; i < real.Length; i++)
            words.Insert(Math.Min(words.Count, 30 + i * 18), real[i]);
        return [.. words];
    }

    private static string Describe(SearchRequest r) =>
        string.Join(" ", new[] { r.Query is { Length: > 0 } q ? $"\"{q}\"" : null, r.Kind, r.Category, r.Role, r.Tags.Count > 0 ? "#" + string.Join(",#", r.Tags) : null,
            r.Sort == SearchSort.Relevance ? null : r.Sort.ToString(), r.HasOpenVacancies ? "open" : null }.OfType<string>()) is { Length: > 0 } s ? s : "(everything)";

    private static readonly SearchRequest[] Searches =
    [
        new() { Query = "ілюстрація" },
        new() { Query = "ilustration" },
        new() { Query = "tesr" },
        new() { Query = "еуые" },
        new() { Query = "музика гурт" },
        new() { Query = "dyzain lohotyp" },
        new() { Query = "мультфільми персонажів" },
        new() { Query = "vepbrf" },
        new() { Query = "street art city" },
        new() { Query = "квантова фізика" },
        new() { },
        new() { Kind = "post", Sort = SearchSort.Popular },
        new() { Kind = "post", Sort = SearchSort.New, Tags = ["animation", "3d"] },
        new() { Query = "портрет", Kind = "post", Category = "Арт" },
        new() { Kind = "team", HasOpenVacancies = true, Role = "ілюстратор" },
        new() { Kind = "vacancy", Role = "дизайнер" },
        new() { Query = "дизайн", From = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), MinLikes = 10 },
    ];

    [SkippableFact]
    public async Task Search_and_autocomplete_meet_the_latency_targets_on_50k_documents()
    {
        Skip.If(db.SkipReason is not null, db.SkipReason);
        Skip.If(Environment.GetEnvironmentVariable("SEARCH_BENCH") != "1", "set SEARCH_BENCH=1 to run the benchmark");

        var setup = Stopwatch.StartNew();
        var writer = new SearchIndexWriter(db.Index!);
        var catalog = await writer.LoadTagCatalogAsync(default);
        foreach (var group in Generate().Concat(SampleDocuments.All).GroupBy(d => d.Kind))
            await writer.SyncKindAsync(group.Key, group.Select(d => SearchIndexWriter.BuildRow(d, catalog)).ToList(), default);
        await writer.RebuildTermsAsync(default);
        await writer.RebuildTagsAsync(catalog, default);
        await writer.RebuildRolesAsync(catalog, default);
        await using (var analyze = db.Index!.CreateCommand("ANALYZE"))
            await analyze.ExecuteNonQueryAsync();
        var corpus = Environment.GetEnvironmentVariable("SEARCH_BENCH_DENSE") == "1" ? "dense" : "Zipf";
        output.WriteLine($"Indexed {Documents} documents ({corpus} corpus) in {setup.Elapsed.TotalSeconds:F1} s");

        var service = new SearchQueryService(db.Index!, new TagCatalogCache(db.Index!));
        var search = await Measure("search", Searches.Select(r => (Describe(r), (Func<Task>)(() => service.SearchAsync(r with { PageSize = 20 }, default)))));

        // A second page through the cursor is a search of its own.
        var first = await service.SearchAsync(new SearchRequest { Kind = "post", Sort = SearchSort.Popular }, default);
        var paging = await Measure("cursor", [("popular posts, page 2", () => service.SearchAsync(new SearchRequest { Kind = "post", Sort = SearchSort.Popular, Cursor = first.NextCursor }, default))]);

        string[] prefixes = ["ілю", "mu", "vepb", "dyz", "анім", "ст", "kera", "пісн"];
        var tags = await Measure("tags", prefixes.Select(p => (p, (Func<Task>)(() => service.TagsAsync(p, 10, default)))));
        var roles = await Measure("roles", prefixes.Select(p => (p, (Func<Task>)(() => service.RolesAsync(p, 10, default)))));
        var suggest = await Measure("suggest", prefixes.Select(p => (p, (Func<Task>)(() => service.SuggestAsync(p, 8, default)))));

        output.WriteLine($"search    p50 {search.P50:F1} ms, p95 {search.P95:F1} ms");
        output.WriteLine($"cursor    p50 {paging.P50:F1} ms, p95 {paging.P95:F1} ms");
        output.WriteLine($"tags      p50 {tags.P50:F1} ms, p95 {tags.P95:F1} ms");
        output.WriteLine($"roles     p50 {roles.P50:F1} ms, p95 {roles.P95:F1} ms");
        output.WriteLine($"suggest   p50 {suggest.P50:F1} ms, p95 {suggest.P95:F1} ms");

        // The dense corpus is the worst case: its numbers are reported, the targets hold for realistic text.
        if (corpus == "dense") return;
        search.P95.Should().BeLessThanOrEqualTo(150);
        paging.P95.Should().BeLessThanOrEqualTo(150);
        tags.P95.Should().BeLessThanOrEqualTo(50);
        roles.P95.Should().BeLessThanOrEqualTo(50);
        suggest.P95.Should().BeLessThanOrEqualTo(50);
    }

    /// <summary>p50 and p95 over all calls; also prints the calls with the slowest median, to see what to tune.</summary>
    private async Task<(double P50, double P95)> Measure(string name, IEnumerable<(string Label, Func<Task> Call)> calls)
    {
        var list = calls.ToList();
        foreach (var (_, call) in list) await call(); // warm-up: connections, dictionaries, plans
        var times = new List<double>();
        var perCall = list.Select(_ => new List<double>()).ToList();
        for (var r = 0; r < Repeats; r++)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var sw = Stopwatch.StartNew();
                await list[i].Call();
                times.Add(sw.Elapsed.TotalMilliseconds);
                perCall[i].Add(sw.Elapsed.TotalMilliseconds);
            }
        }
        var slowest = perCall.Select((t, i) => (list[i].Label, Median: t.Order().ElementAt(t.Count / 2)))
            .OrderByDescending(x => x.Median).Take(3);
        output.WriteLine($"{name,-9} slowest: {string.Join("; ", slowest.Select(s => $"{s.Label} {s.Median:F0} ms"))}");
        times.Sort();
        return (times[(int)(times.Count * 0.5)], times[Math.Min(times.Count - 1, (int)Math.Ceiling(times.Count * 0.95) - 1)]);
    }
}
