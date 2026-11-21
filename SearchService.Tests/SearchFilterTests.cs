using FluentAssertions;
using SearchService.Indexing;
using SearchService.Search;
using Docs = SearchService.Tests.SampleDocuments;

namespace SearchService.Tests;

/// <summary>Stage 3: combined filters, sorting, facets and keyset pagination. Own database (class fixture).</summary>
public sealed class SearchFilterTests(SearchDatabaseFixture db) : IClassFixture<SearchDatabaseFixture>
{
    private SearchQueryService Service()
    {
        Skip.If(db.SkipReason is not null, db.SkipReason);
        return new SearchQueryService(db.Index!, new TagCatalogCache(db.Index!));
    }

    private Task<SearchResponse> Search(SearchRequest request) => Service().SearchAsync(request, default);

    private static string[] Ids(SearchResponse r) => r.Items.Select(i => i.Id).ToArray();

    [SkippableFact]
    public async Task Role_filter_is_fuzzy_and_works_in_both_scripts()
    {
        // The team matches too: it has an open vacancy for this role.
        Ids(await Search(new() { Role = "художник" })).Should().BeEquivalentTo([Docs.ModelerVacancy.Id, Docs.AnimationTeam.Id]);
        Ids(await Search(new() { Role = "khudozhnyk", Kind = "vacancy" })).Should().Equal(Docs.ModelerVacancy.Id);
        Ids(await Search(new() { Role = "аніматор" })).Should().Equal(Docs.AnimationTeam.Id);
        Ids(await Search(new() { Role = "ілюстраторка", Kind = "user" })).Should().Equal(Docs.Olena.Id);
    }

    [SkippableFact]
    public async Task Open_vacancies_filter_drops_teams_without_them_only()
    {
        Ids(await Search(new() { Kind = "team", HasOpenVacancies = true })).Should().Equal(Docs.AnimationTeam.Id);
        (await Search(new() { HasOpenVacancies = true })).Facets.Kinds.Should().ContainKey("post");
    }

    [SkippableFact]
    public async Task Date_range_includes_from_and_excludes_to()
    {
        var r = await Search(new() { Kind = "post", From = Docs.BookIllustration.CreatedAt, To = Docs.BrushTesting.CreatedAt });
        Ids(r).Should().BeEquivalentTo([Docs.BookIllustration.Id, Docs.Song.Id]);
    }

    [SkippableFact]
    public async Task Author_and_team_filters_cover_every_kind()
    {
        Ids(await Search(new() { AuthorId = Docs.Olena.SourceId })).Should().BeEquivalentTo([Docs.BookIllustration.Id, Docs.Olena.Id]);
        Ids(await Search(new() { TeamId = Docs.AnimationTeam.SourceId })).Should().BeEquivalentTo([Docs.AnimationTeam.Id, Docs.ModelerVacancy.Id]);
        Ids(await Search(new() { MinLikes = 5 })).Should().Equal(Docs.BookIllustration.Id);
    }

    [SkippableFact]
    public async Task Searchers_own_profile_is_left_out_but_their_posts_stay()
    {
        var all = await Search(new() { AuthorId = Docs.Olena.SourceId });
        var asOlena = await Search(new() { AuthorId = Docs.Olena.SourceId, ViewerId = Docs.Olena.SourceId });

        Ids(asOlena).Should().Equal(Docs.BookIllustration.Id);
        asOlena.Facets.Kinds.GetValueOrDefault("user").Should().Be(all.Facets.Kinds.GetValueOrDefault("user") - 1);
    }

    [SkippableFact]
    public async Task Filters_combine_with_the_query()
    {
        var r = await Search(new() { Query = "ілюстрація", Kind = "post", From = Docs.BookIllustration.CreatedAt });
        Ids(r).Should().Equal(Docs.BookIllustration.Id);
    }

    [SkippableFact]
    public async Task Sort_new_and_popular()
    {
        var newest = await Search(new() { Kind = "post", Sort = SearchSort.New });
        Ids(newest).Should().Equal(Docs.BrushTesting.Id, Docs.Song.Id, Docs.BookIllustration.Id, Docs.TestPost.Id);

        var popular = await Search(new() { Kind = "post", Sort = SearchSort.Popular });
        Ids(popular)[0].Should().Be(Docs.BookIllustration.Id);
    }

    [SkippableFact]
    public async Task Facets_count_each_value_while_ignoring_their_own_filter()
    {
        var r = await Search(new() { Category = "Арт" });
        r.Items.Should().OnlyContain(i => i.Categories.Contains("Арт"));
        r.Facets.Categories.Should().Contain(new FacetValue("Арт", 2)).And.Contain(new FacetValue("Музика", 1));
        r.Facets.Tags.Should().Contain(t => t.Key == "illustration" && t.Label == "illustration" && t.Count == 2);
        r.Facets.Kinds.Should().Equal(new Dictionary<string, int> { ["post"] = 2 });

        var byKind = await Search(new() { Kind = "team" });
        byKind.Facets.Kinds.Keys.Should().Contain(["post", "team", "vacancy", "user"]);
        byKind.Total.Should().Be(2);
    }

    [SkippableFact]
    public async Task Cursor_pages_through_without_repeats_even_when_documents_are_added()
    {
        var first = await Search(new() { Kind = "post", Sort = SearchSort.New, PageSize = 2 });
        Ids(first).Should().Equal(Docs.BrushTesting.Id, Docs.Song.Id);
        first.NextCursor.Should().NotBeNull();

        // A newer post appears between the pages; with an offset the next page would repeat Song.
        var writer = new SearchIndexWriter(db.Index!);
        var catalog = await writer.LoadTagCatalogAsync(default);
        var fresh = Docs.Song with { SourceId = "99", Title = "Свіжий пост", CreatedAt = Docs.BrushTesting.CreatedAt.AddDays(5) };
        var posts = new[] { Docs.TestPost, Docs.BookIllustration, Docs.Song, Docs.BrushTesting };
        await writer.SyncKindAsync("post", [.. posts.Append(fresh).Select(d => SearchIndexWriter.BuildRow(d, catalog))], default);
        try
        {
            var second = await Search(new() { Kind = "post", Sort = SearchSort.New, PageSize = 2, Cursor = first.NextCursor });
            Ids(second).Should().Equal(Docs.BookIllustration.Id, Docs.TestPost.Id);
            second.NextCursor.Should().BeNull();
        }
        finally
        {
            await writer.SyncKindAsync("post", [.. posts.Select(d => SearchIndexWriter.BuildRow(d, catalog))], default);
        }
    }

    [SkippableFact]
    public async Task Cursor_works_for_relevance_and_popular_order()
    {
        foreach (var sort in new[] { SearchSort.Relevance, SearchSort.Popular })
        {
            var all = await Search(new() { Query = "ілюстрація", Sort = sort, PageSize = 50 });
            var paged = new List<string>();
            string? cursor = null;
            do
            {
                var page = await Search(new() { Query = "ілюстрація", Sort = sort, PageSize = 1, Cursor = cursor });
                paged.AddRange(Ids(page));
                cursor = page.NextCursor;
            } while (cursor is not null);
            paged.Should().Equal(Ids(all), $"paging by {sort} must give the same order");
        }
    }

    [SkippableFact]
    public async Task Invalid_or_foreign_cursor_is_rejected()
    {
        var service = Service();
        await service.Invoking(s => s.SearchAsync(new() { Cursor = "garbage!" }, default))
            .Should().ThrowAsync<InvalidCursorException>();

        var page = await service.SearchAsync(new() { Sort = SearchSort.New, PageSize = 1 }, default);
        await service.Invoking(s => s.SearchAsync(new() { Sort = SearchSort.Popular, Cursor = page.NextCursor }, default))
            .Should().ThrowAsync<InvalidCursorException>();
    }
}
