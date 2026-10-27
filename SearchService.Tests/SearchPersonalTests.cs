using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SearchService.Indexing;
using SearchService.Personal;
using SearchService.Search;
using Docs = SearchService.Tests.SampleDocuments;

namespace SearchService.Tests;

/// <summary>Stage 5: history, saved searches and subscriptions. Own database (class fixture).</summary>
public sealed class SearchPersonalTests(SearchDatabaseFixture db) : IClassFixture<SearchDatabaseFixture>
{
    private PersonalSearchStore Store()
    {
        Skip.If(db.SkipReason is not null, db.SkipReason);
        return new PersonalSearchStore(db.Index!);
    }

    private SubscriptionMatcher Matcher() =>
        new(db.Index!, new SearchQueryService(db.Index!, new TagCatalogCache(db.Index!)), NullLogger<SubscriptionMatcher>.Instance);

    private static string NewUser() => Guid.NewGuid().ToString();

    [SkippableFact]
    public async Task Repeated_search_moves_to_the_top_instead_of_duplicating()
    {
        var store = Store();
        var user = NewUser();
        var art = SearchFilters.Of(new SearchRequest { Category = "Арт" });
        await store.RecordAsync(user, "ілюстрація", art, 2, default);
        await store.RecordAsync(user, "пісня", new SearchFilters(), 1, default);
        await store.RecordAsync(user, " Ілюстрація  ", art, 3, default);

        var history = await store.HistoryAsync(user, null, 10, default);
        history.Select(h => h.Query).Should().Equal("Ілюстрація", "пісня");
        history[0].ResultsCount.Should().Be(3);
        history[0].Filters.Category.Should().Be("Арт");

        (await store.HistoryAsync(user, "піс", 10, default)).Select(h => h.Query).Should().Equal("пісня");
        (await store.HistoryAsync(user, "%", 10, default)).Should().BeEmpty("LIKE wildcards are escaped");
        (await store.HistoryAsync(NewUser(), null, 10, default)).Should().BeEmpty("history is per user");
    }

    [SkippableFact]
    public async Task History_keeps_the_newest_entries_and_can_be_deleted()
    {
        var store = Store();
        var user = NewUser();
        for (var i = 0; i < PersonalSearchStore.HistoryLimit + 5; i++)
            await store.RecordAsync(user, $"запит {i}", new SearchFilters(), i, default);

        var history = await store.HistoryAsync(user, null, 100, default);
        history.Should().HaveCount(PersonalSearchStore.HistoryLimit);
        history[0].Query.Should().Be($"запит {PersonalSearchStore.HistoryLimit + 4}");

        (await store.DeleteHistoryAsync(NewUser(), history[0].Id, default)).Should().Be(0, "another user's entry is untouched");
        (await store.DeleteHistoryAsync(user, history[0].Id, default)).Should().Be(1);
        (await store.DeleteHistoryAsync(user, null, default)).Should().Be(PersonalSearchStore.HistoryLimit - 1);
    }

    [SkippableFact]
    public async Task Saved_searches_are_private_and_names_unique_per_user()
    {
        var store = Store();
        var user = NewUser();
        var filters = SearchFilters.Of(new SearchRequest { Kind = "vacancy", Role = "аніматор" });
        var (result, id) = await store.SaveAsync(user, null, new SavedSearchInput("Аніматори", "", filters, Notify: false), default);
        result.Should().Be(SaveResult.Saved);

        (await store.SaveAsync(user, null, new SavedSearchInput("Аніматори", "x", null, false), default)).Result
            .Should().Be(SaveResult.DuplicateName);
        (await store.SaveAsync(NewUser(), null, new SavedSearchInput("Аніматори", "", null, false), default)).Result
            .Should().Be(SaveResult.Saved, "names are unique per user only");
        (await store.SaveAsync(NewUser(), id, new SavedSearchInput("Чуже", "", null, false), default)).Result
            .Should().Be(SaveResult.NotFound);

        (await store.SaveAsync(user, id, new SavedSearchInput("Аніматори 2D", "мультфільм", filters, true), default)).Result
            .Should().Be(SaveResult.Saved);
        var saved = (await store.SavedAsync(user, default)).Should().ContainSingle().Subject;
        saved.Should().BeEquivalentTo(new { Name = "Аніматори 2D", Query = "мультфільм", Notify = true, Unread = 0 });
        saved.Filters.Should().Be(filters);

        (await store.DeleteSavedAsync(NewUser(), id, default)).Should().BeFalse();
        (await store.DeleteSavedAsync(user, id, default)).Should().BeTrue();
        (await store.SavedAsync(user, default)).Should().BeEmpty();
    }

    [SkippableFact]
    public async Task Subscription_notifies_once_about_each_new_matching_document()
    {
        var store = Store();
        var matcher = Matcher();
        var user = NewUser();
        var filters = SearchFilters.Of(new SearchRequest { Kind = "vacancy", Role = "ілюстратор" });
        var (_, id) = await store.SaveAsync(user, null, new SavedSearchInput("Ілюстратори", "", filters, Notify: true), default);
        var (_, quiet) = await store.SaveAsync(user, null, new SavedSearchInput("Без сповіщень", "", filters, Notify: false), default);

        var writer = new SearchIndexWriter(db.Index!);
        var catalog = await writer.LoadTagCatalogAsync(default);
        var vacancies = new[] { Docs.ModelerVacancy };
        var illustrator = Docs.ModelerVacancy with { SourceId = "650000000000000000000099", Title = "Illustrator", Roles = ["Illustrator"] };
        var drummer = Docs.ModelerVacancy with { SourceId = "650000000000000000000098", Title = "Барабанщик", Roles = ["Барабанщик"] };
        try
        {
            (await matcher.RunAsync(default)).Should().Be(0, "documents indexed before the subscription are not new");

            await writer.SyncKindAsync("vacancy", [.. vacancies.Append(illustrator).Append(drummer).Select(d => SearchIndexWriter.BuildRow(d, catalog))], default);
            (await matcher.RunAsync(default)).Should().Be(1);
            (await matcher.RunAsync(default)).Should().Be(0, "the same document notifies once");

            var notifications = await store.NotificationsAsync(user, unreadOnly: true, 10, default);
            notifications.Should().ContainSingle().Which.Should().BeEquivalentTo(new
            {
                SavedSearchId = id, SavedSearchName = "Ілюстратори", DocumentId = illustrator.Id, Kind = "vacancy", Title = "Illustrator", Read = false,
            });
            (await store.SavedAsync(user, default)).Single(s => s.Id == id).Unread.Should().Be(1);
            (await store.SavedAsync(user, default)).Single(s => s.Id == quiet).Unread.Should().Be(0);

            (await store.MarkReadAsync(NewUser(), [], default)).Should().Be(0);
            (await store.MarkReadAsync(user, [], default)).Should().Be(1);
            (await store.NotificationsAsync(user, unreadOnly: true, 10, default)).Should().BeEmpty();
            (await store.NotificationsAsync(user, unreadOnly: false, 10, default)).Should().ContainSingle().Which.Read.Should().BeTrue();
        }
        finally
        {
            await writer.SyncKindAsync("vacancy", [.. vacancies.Select(d => SearchIndexWriter.BuildRow(d, catalog))], default);
            await store.DeleteSavedAsync(user, id, default);
            await store.DeleteSavedAsync(user, quiet, default);
        }
    }

    [SkippableFact]
    public async Task Subscription_skips_the_users_own_documents()
    {
        var store = Store();
        var matcher = Matcher();
        var author = Docs.Olena.SourceId;
        var (_, id) = await store.SaveAsync(author, null, new SavedSearchInput("Мої пісні", "пісня", new SearchFilters(), Notify: true), default);

        var writer = new SearchIndexWriter(db.Index!);
        var catalog = await writer.LoadTagCatalogAsync(default);
        var posts = new[] { Docs.TestPost, Docs.BookIllustration, Docs.Song, Docs.BrushTesting };
        var own = Docs.Song with { SourceId = "77", OwnerId = author };
        try
        {
            await writer.SyncKindAsync("post", [.. posts.Append(own).Select(d => SearchIndexWriter.BuildRow(d, catalog))], default);
            (await matcher.RunAsync(default)).Should().Be(0);
        }
        finally
        {
            await writer.SyncKindAsync("post", [.. posts.Select(d => SearchIndexWriter.BuildRow(d, catalog))], default);
            await store.DeleteSavedAsync(author, id, default);
        }
    }
}
