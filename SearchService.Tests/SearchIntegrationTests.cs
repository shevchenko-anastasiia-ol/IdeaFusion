using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SearchService.Data;
using SearchService.Indexing;
using SearchService.Search;

namespace SearchService.Tests;

/// <summary>
/// A throwaway database on a Postgres built from infrastructure/postgres (hunspell uk_UA). Point
/// SEARCH_TEST_POSTGRES at a server, e.g.
/// <c>docker run -d -p 55432:5432 -e POSTGRES_PASSWORD=t ideafusion-postgres</c> and
/// <c>SEARCH_TEST_POSTGRES="Host=localhost;Port=55432;Username=postgres;Password=t"</c>.
/// Without it the tests are skipped.
/// </summary>
public sealed class SearchDatabaseFixture : IAsyncLifetime
{
    private readonly string? _server = Environment.GetEnvironmentVariable("SEARCH_TEST_POSTGRES");
    private readonly string _database = "search_test_" + Guid.NewGuid().ToString("N")[..8];

    public NpgsqlDataSource? Index { get; private set; }
    public string? SkipReason => _server is null ? "SEARCH_TEST_POSTGRES is not set" : null;

    public async Task InitializeAsync()
    {
        if (_server is null) return;
        await using (var admin = new NpgsqlConnection(_server))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE {_database}", admin);
            await create.ExecuteNonQueryAsync();
        }

        Index = NpgsqlDataSource.Create(new NpgsqlConnectionStringBuilder(_server) { Database = _database }.ConnectionString);
        await SearchMigrations.ApplyAsync(Index, NullLogger.Instance, default);
        // A second run must be a no-op.
        await SearchMigrations.ApplyAsync(Index, NullLogger.Instance, default);

        var writer = new SearchIndexWriter(Index);
        var catalog = await writer.LoadTagCatalogAsync(default);
        foreach (var group in SampleDocuments.All.GroupBy(d => d.Kind))
            await writer.SyncKindAsync(group.Key, group.Select(d => SearchIndexWriter.BuildRow(d, catalog)).ToList(), default);
        await writer.RebuildTermsAsync(default);
        await writer.RebuildTagsAsync(catalog, default);
        await writer.RebuildRolesAsync(catalog, default);
    }

    public async Task DisposeAsync()
    {
        if (_server is null || Index is null) return;
        await Index.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        // SEARCH_TEST_KEEP_DB=1 leaves the database for profiling (e.g. after the benchmark).
        if (Environment.GetEnvironmentVariable("SEARCH_TEST_KEEP_DB") == "1") return;
        await using var admin = new NpgsqlConnection(_server);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {_database} WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}

internal static class SampleDocuments
{
    private static readonly DateTime Day = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    public static readonly SourceDocument TestPost = new()
    {
        Kind = DocumentKinds.Post, SourceId = "1", Title = "Test post about illustration workflow",
        Body = "How I sketch and ink a page", Tags = ["illustration", "wip"], Subtitle = "anna", CreatedAt = Day,
    };
    public static readonly SourceDocument BookIllustration = new()
    {
        Kind = DocumentKinds.Post, SourceId = "2", Title = "Ілюстрація для дитячої книжки",
        Body = "Малюю ілюстрації аквареллю для видавництва", Tags = ["illustration"], Subtitle = "Олена Коваленко",
        CreatedAt = Day.AddDays(1), Likes = 10, OwnerId = "8f1d6a8e-0000-0000-0000-000000000001",
    };
    public static readonly SourceDocument Song = new()
    {
        Kind = DocumentKinds.Post, SourceId = "3", Title = "Нова пісня", Body = "Записали трек з друзями у гаражі",
        Tags = ["music"], CreatedAt = Day.AddDays(2),
    };
    public static readonly SourceDocument BrushTesting = new()
    {
        Kind = DocumentKinds.Post, SourceId = "4", Title = "Тестування нового пензля", Body = "Порівняння текстур",
        Tags = ["digital"], CreatedAt = Day.AddDays(3),
    };
    public static readonly SourceDocument AnimationTeam = new()
    {
        Kind = DocumentKinds.Team, SourceId = "650000000000000000000001", Title = "Команда аніматорів",
        Body = "Шукаємо людей для короткого мультфільму", Tags = ["animation"], CategoryHints = ["Анімація"], Roles = ["Аніматор 2D"],
        CreatedAt = Day, OpenVacancies = 1, TeamId = "650000000000000000000001",
    };
    public static readonly SourceDocument ModelerVacancy = new()
    {
        Kind = DocumentKinds.Vacancy, SourceId = "650000000000000000000002", Title = "3D художник",
        Subtitle = "Команда аніматорів", Body = "Моделювання персонажів у Blender", Tags = ["3d"], CreatedAt = Day,
        Roles = ["3D художник"], TeamId = "650000000000000000000001",
    };
    public static readonly SourceDocument Olena = new()
    {
        Kind = DocumentKinds.User, SourceId = "8f1d6a8e-0000-0000-0000-000000000001", Title = "Олена Коваленко",
        Subtitle = "olena_k · Ілюстраторка", Body = "Ілюстраторка", CreatedAt = Day,
        Roles = ["Ілюстраторка"], OwnerId = "8f1d6a8e-0000-0000-0000-000000000001",
    };

    public static readonly SourceDocument PoetsTeam = new()
    {
        Kind = DocumentKinds.Team, SourceId = "650000000000000000000003", Title = "Гурток поетів",
        Body = "Читаємо вірші щочетверга", Tags = ["poetry"], CreatedAt = Day, OpenVacancies = 0,
    };

    public static readonly SourceDocument[] All = [TestPost, BookIllustration, Song, BrushTesting, AnimationTeam, ModelerVacancy, Olena, PoetsTeam];
}

public sealed class SearchIntegrationTests(SearchDatabaseFixture db) : IClassFixture<SearchDatabaseFixture>
{
    private SearchQueryService Service()
    {
        Skip.If(db.SkipReason is not null, db.SkipReason);
        return new SearchQueryService(db.Index!, new TagCatalogCache(db.Index!));
    }

    private Task<SearchResponse> Search(string q, string? kind = null, string? category = null) =>
        Service().SearchAsync(new SearchRequest { Query = q, Kind = kind, Category = category }, default);

    private static IEnumerable<string> Ids(SearchResponse r) => r.Items.Select(i => i.Id);

    [SkippableFact]
    public async Task Typo_finds_the_word_and_suggests_the_correction()
    {
        var r = await Search("tesr");
        r.Items.Should().NotBeEmpty();
        r.Items[0].Id.Should().Be(SampleDocuments.TestPost.Id);
        r.DidYouMean.Should().Be("test");
    }

    [SkippableFact]
    public async Task Cyrillic_spelling_of_a_Latin_word_finds_it_without_a_correction()
    {
        var r = await Search("тест");
        Ids(r).Should().Contain(SampleDocuments.TestPost.Id).And.Contain(SampleDocuments.BrushTesting.Id);
        r.DidYouMean.Should().BeNull();
    }

    [SkippableFact]
    public async Task Wrong_keyboard_layout_is_retyped()
    {
        var r = await Search("еуые");
        Ids(r).Should().Contain(SampleDocuments.TestPost.Id);
        r.DidYouMean.Should().Be("test");

        var back = await Search("ntcnedfyyz"); // "тестування" typed with the Latin layout
        Ids(back).Should().Contain(SampleDocuments.BrushTesting.Id);
    }

    [SkippableFact]
    public async Task Transliterated_query_with_mistakes_finds_Ukrainian_text()
    {
        var r = await Search("ilustracia");
        Ids(r).Should().Contain(SampleDocuments.BookIllustration.Id);
    }

    [SkippableFact]
    public async Task Inflected_Ukrainian_word_matches_through_the_dictionary()
    {
        var r = await Search("ілюстраціями");
        Ids(r).Should().Contain(SampleDocuments.BookIllustration.Id);
        r.DidYouMean.Should().BeNull();

        var team = await Search("аніматор");
        Ids(team).Should().Contain(SampleDocuments.AnimationTeam.Id);
    }

    [SkippableFact]
    public async Task Category_name_finds_documents_by_their_tags()
    {
        var r = await Search("музика");
        Ids(r).Should().Contain(SampleDocuments.Song.Id);
    }

    [SkippableFact]
    public async Task Category_filter_keeps_only_that_category()
    {
        var r = await Search("", category: "Арт");
        Ids(r).Should().BeEquivalentTo([SampleDocuments.TestPost.Id, SampleDocuments.BookIllustration.Id]);
    }

    [SkippableFact]
    public async Task People_are_found_by_name_in_either_script()
    {
        (await Search("олена", kind: "user")).Items.Should().ContainSingle(i => i.Id == SampleDocuments.Olena.Id);
        (await Search("olena kovalenko", kind: "user")).Items.Should().ContainSingle(i => i.Id == SampleDocuments.Olena.Id);
        (await Search("коваленко")).Facets.Kinds.Should().ContainKey("user");
    }

    [SkippableFact]
    public async Task Kind_counts_cover_all_kinds_while_items_follow_the_filter()
    {
        var r = await Search("аніматорів", kind: "vacancy");
        r.Items.Should().OnlyContain(i => i.Kind == "vacancy");
        r.Facets.Kinds.Should().ContainKey("team").And.ContainKey("vacancy");
        r.Total.Should().Be(r.Facets.Kinds["vacancy"]);
    }

    [SkippableFact]
    public async Task Unrelated_query_finds_nothing()
    {
        var r = await Search("квантова фізика");
        r.Items.Should().BeEmpty();
        r.Total.Should().Be(0);
    }

    [SkippableFact]
    public async Task Empty_query_lists_newest_first()
    {
        var r = await Search("", kind: "post");
        r.Items.First().Id.Should().Be(SampleDocuments.BrushTesting.Id);
        r.Total.Should().Be(4);
    }

    [SkippableFact]
    public async Task Suggest_completes_the_last_word()
    {
        var s = await Service().SuggestAsync("нова ілюс", 5, default);
        s.Completions.Should().Contain(c => c.StartsWith("нова ілюстр"));
        s.Documents.Should().NotBeEmpty();
    }

    private Task<SearchResponse> ByTags(TagMatch mode, params string[] tags) =>
        Service().SearchAsync(new SearchRequest { Tags = tags, TagMode = mode, Kind = "post" }, default);

    [SkippableFact]
    public async Task Tag_filter_accepts_any_spelling_or_synonym()
    {
        var expected = new[] { SampleDocuments.TestPost.Id, SampleDocuments.BookIllustration.Id };
        Ids(await ByTags(TagMatch.Any, "illustration")).Should().BeEquivalentTo(expected);
        Ids(await ByTags(TagMatch.Any, "#Illustration")).Should().BeEquivalentTo(expected);
        Ids(await ByTags(TagMatch.Any, "ілюстрація")).Should().BeEquivalentTo(expected);
    }

    [SkippableFact]
    public async Task Tag_filter_all_requires_every_tag_and_any_one_of_them()
    {
        Ids(await ByTags(TagMatch.All, "ілюстрація", "WIP")).Should().Equal(SampleDocuments.TestPost.Id);
        Ids(await ByTags(TagMatch.Any, "music", "wip")).Should().BeEquivalentTo([SampleDocuments.Song.Id, SampleDocuments.TestPost.Id]);
        (await ByTags(TagMatch.All, "music", "wip")).Total.Should().Be(0);
    }

    [SkippableFact]
    public async Task Query_word_finds_documents_tagged_with_its_synonym()
    {
        // TestPost is in English and tagged "illustration"; the query is the Ukrainian synonym.
        Ids(await Search("ілюстрація")).Should().Contain(SampleDocuments.TestPost.Id);
    }

    [SkippableTheory]
    [InlineData("ілюс")]
    [InlineData("ilustr")]
    [InlineData("#Illus")]
    [InlineData("шдгіек")] // "ilustr" typed with the Ukrainian layout
    public async Task Tag_autocomplete_offers_the_tag_by_any_name(string typed)
    {
        var tags = await Service().TagsAsync(typed, 5, default);
        tags.Should().NotBeEmpty();
        tags[0].Key.Should().Be("illustration");
        tags[0].Documents.Should().Be(2);
    }

    [SkippableFact]
    public async Task Tag_autocomplete_normalizes_3d_spellings_and_lists_popular_tags_without_a_query()
    {
        (await Service().TagsAsync("3-д", 5, default)).Should().Contain(t => t.Key == "3d");
        var popular = await Service().TagsAsync("", 3, default);
        popular[0].Key.Should().Be("illustration");
    }

    [SkippableFact]
    public async Task Resync_removes_documents_the_source_no_longer_has()
    {
        Skip.If(db.SkipReason is not null, db.SkipReason);
        var writer = new SearchIndexWriter(db.Index!);
        var catalog = await writer.LoadTagCatalogAsync(default);
        var temporary = SampleDocuments.Olena with { SourceId = "8f1d6a8e-0000-0000-0000-000000000002", Title = "Тимчасовий Користувач" };

        await writer.SyncKindAsync("user", [SearchIndexWriter.BuildRow(SampleDocuments.Olena, catalog), SearchIndexWriter.BuildRow(temporary, catalog)], default);
        (await Search("тимчасовий", kind: "user")).Total.Should().Be(1);

        var changed = await writer.SyncKindAsync("user", [SearchIndexWriter.BuildRow(SampleDocuments.Olena, catalog)], default);
        changed.Should().Be(1);
        (await Search("тимчасовий", kind: "user")).Total.Should().Be(0);

        // Nothing changed: nothing is rewritten.
        (await writer.SyncKindAsync("user", [SearchIndexWriter.BuildRow(SampleDocuments.Olena, catalog)], default)).Should().Be(0);
    }
}
