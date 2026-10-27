using FluentAssertions;
using SearchService.Feed;
using SearchService.Indexing;
using SearchService.Search;
using Docs = SearchService.Tests.SampleDocuments;

namespace SearchService.Tests;

/// <summary>
/// Stage 8: the control set of queries with their expected results: typos, transliteration, the wrong
/// keyboard layout, word forms and filters. A change that breaks one of these breaks search quality.
/// </summary>
public sealed class ControlQueryTests(SearchDatabaseFixture db) : IClassFixture<SearchDatabaseFixture>
{
    public sealed record Control(string Why, string Query, string? Kind, string[] Expected, bool First = true)
    {
        public override string ToString() => $"{Why}: \"{Query}\"";
    }

    public static TheoryData<Control> Controls => new()
    {
        new("typo", "tesr", null, [Docs.TestPost.Id]),
        new("typo in Cyrillic", "писня", null, [Docs.Song.Id]),
        new("Cyrillic for a Latin word", "тест", "post", [Docs.TestPost.Id]),
        new("wrong layout, Latin word", "еуые", "post", [Docs.TestPost.Id]),
        new("wrong layout, Ukrainian word", "gscyz", null, [Docs.Song.Id]),
        new("transliterated Ukrainian", "knyzhky", null, [Docs.BookIllustration.Id]),
        new("transliterated, own spelling", "ilustratsiia", null, [Docs.BookIllustration.Id], First: false),
        new("word form", "ілюстраціями", null, [Docs.BookIllustration.Id], First: false),
        new("word form of a team", "аніматорів", null, [Docs.AnimationTeam.Id]),
        new("team in Latin", "komanda animatoriv", null, [Docs.AnimationTeam.Id]),
        new("vacancy by its body", "blender", null, [Docs.ModelerVacancy.Id]),
        new("person by name", "олена", "user", [Docs.Olena.Id]),
        new("tag synonym", "музика", null, [Docs.Song.Id]),
        new("category", "Арт", "post", [Docs.BookIllustration.Id, Docs.TestPost.Id], First: false),
        new("filter only", "", "vacancy", [Docs.ModelerVacancy.Id]),
        new("nothing related", "квантова фізика", null, []),
    };

    [SkippableTheory]
    [MemberData(nameof(Controls))]
    public async Task Control_query_finds_what_it_should(Control control)
    {
        Skip.If(db.SkipReason is not null, db.SkipReason);
        var service = new SearchQueryService(db.Index!, new TagCatalogCache(db.Index!));
        var ids = (await service.SearchAsync(new SearchRequest { Query = control.Query, Kind = control.Kind }, default))
            .Items.Select(i => i.Id).ToList();

        if (control.Expected.Length == 0)
        {
            ids.Should().BeEmpty();
            return;
        }
        ids.Should().Contain(control.Expected);
        if (control.First) ids[0].Should().Be(control.Expected[0]);
    }

    [SkippableFact]
    public async Task Metrics_report_zero_results_latency_and_top3_clicks()
    {
        Skip.If(db.SkipReason is not null, db.SkipReason);
        var metrics = new SearchMetrics(db.Index!, new FeedEventLog(db.Index!));
        await metrics.RecordQueryAsync("post", hasQuery: true, corrected: true, results: 0, TimeSpan.FromMilliseconds(40), default);
        await metrics.RecordQueryAsync(null, hasQuery: true, corrected: false, results: 3, TimeSpan.FromMilliseconds(20), default);
        await metrics.RecordQueryAsync(null, hasQuery: false, corrected: false, results: 0, TimeSpan.FromMilliseconds(10), default);
        await metrics.RecordClickAsync("post", 0, default);
        await metrics.RecordClickAsync("team", 5, default);

        var report = await metrics.ReportAsync(1, default);
        report.Queries.Should().BeGreaterThanOrEqualTo(3);
        report.ZeroResultRate.Should().BeApproximately(0.5, 0.0001, "only searches with a query count, and one of two found nothing");
        report.CorrectedRate.Should().BeApproximately(0.5, 0.0001);
        report.LatencyP50Ms.Should().Be(20);
        report.ClicksInTop3.Should().BeApproximately(0.5, 0.0001);
        report.IndexLagP95Seconds.Should().BeGreaterThanOrEqualTo(0);

        await metrics.PruneAsync(default);
        (await metrics.ReportAsync(1, default)).Queries.Should().Be(report.Queries, "recent rows are kept");
    }
}
