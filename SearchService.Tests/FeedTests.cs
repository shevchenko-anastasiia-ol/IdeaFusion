using FluentAssertions;
using SearchService.Feed;
using SearchService.Indexing;
using Docs = SearchService.Tests.SampleDocuments;

namespace SearchService.Tests;

public class FeedDiversityTests
{
    [Fact]
    public void No_author_fills_more_than_two_places_in_a_row()
    {
        var ranked = new[] { "a1", "a2", "a3", "a4", "b1", "c1", "a5" };
        FeedService.Diversify(ranked, x => x[..1]).Should().Equal("a1", "a2", "b1", "a3", "a4", "c1", "a5");
    }

    [Fact]
    public void A_single_author_left_still_fills_the_rest()
    {
        FeedService.Diversify(["a1", "a2", "a3", "b1"], x => x[..1]).Should().Equal("a1", "a2", "b1", "a3");
        FeedService.Diversify(["a1", "a2", "a3"], x => x[..1]).Should().Equal("a1", "a2", "a3");
    }
}

/// <summary>Stage 6: interest profiles and the "For you" feed. Own database (class fixture).</summary>
public sealed class FeedTests(SearchDatabaseFixture db) : IClassFixture<SearchDatabaseFixture>
{
    private static readonly DateTime AsOf = Docs.BrushTesting.CreatedAt.AddDays(2);
    private const string Reader = "11111111-0000-0000-0000-000000000001";

    private FeedService Feed()
    {
        Skip.If(db.SkipReason is not null, db.SkipReason);
        return new FeedService(db.Index!, new TagCatalogCache(db.Index!));
    }

    private Task Interactions(params Interaction[] interactions) =>
        new InterestProfileWriter(db.Index!).SyncAsync(interactions, default);

    [SkippableFact]
    public async Task Liked_tags_lead_the_feed_and_seen_posts_are_left_out()
    {
        var feed = Feed();
        await Interactions(new Interaction(Reader, Docs.TestPost.Id, "like", AsOf.AddDays(-1)));

        var r = await feed.FeedAsync(Reader, 1, 10, AsOf, default);
        r.Variant.Should().Be(FeedService.ContentVariant);
        r.Items.Select(i => i.Post.Id).Should().NotContain(Docs.TestPost.Id).And.HaveCount(3);
        r.Items[0].Post.Id.Should().Be(Docs.BookIllustration.Id);
        r.Items[0].Reason.Should().BeEquivalentTo(new { Kind = "tag", Value = "illustration", Label = "illustration" });
        r.Items[0].Reason.Text.Should().Be("Бо вас цікавить #illustration");
        r.Items.Skip(1).Should().OnlyContain(i => i.Reason.Kind == "popular");
    }

    [SkippableFact]
    public async Task Own_posts_teach_the_profile_but_are_not_recommended()
    {
        var feed = Feed();
        await Interactions();
        var author = Docs.Olena.SourceId;

        var r = await feed.FeedAsync(author, 1, 10, AsOf, default);
        r.Variant.Should().Be(FeedService.ContentVariant);
        r.Items.Select(i => i.Post.Id).Should().NotContain(Docs.BookIllustration.Id);
        r.Items[0].Post.Id.Should().Be(Docs.TestPost.Id);
        r.Items[0].Reason.Kind.Should().Be("tag");
    }

    [SkippableFact]
    public async Task Liked_author_is_a_reason_too()
    {
        var feed = Feed();
        var writer = new SearchIndexWriter(db.Index!);
        var catalog = await writer.LoadTagCatalogAsync(default);
        var posts = new[] { Docs.TestPost, Docs.BookIllustration, Docs.Song, Docs.BrushTesting };
        var another = Docs.Song with { SourceId = "50", Title = "Ще одна ілюстрація", Tags = ["poetry"], OwnerId = Docs.Olena.SourceId };
        try
        {
            await writer.SyncKindAsync("post", [.. posts.Append(another).Select(d => SearchIndexWriter.BuildRow(d, catalog))], default);
            await Interactions(new Interaction(Reader, Docs.BookIllustration.Id, "save", AsOf.AddDays(-1)));

            var r = await feed.FeedAsync(Reader, 1, 10, AsOf, default);
            var item = r.Items.Single(i => i.Post.Id == another.Id);
            item.Reason.Should().BeEquivalentTo(new { Kind = "author", Value = Docs.Olena.SourceId, Label = "Олена Коваленко" });
        }
        finally
        {
            await writer.SyncKindAsync("post", [.. posts.Select(d => SearchIndexWriter.BuildRow(d, catalog))], default);
        }
    }

    [SkippableFact]
    public async Task Cold_start_shows_popular_posts_lifted_by_the_specialization()
    {
        var feed = Feed();
        await Interactions();

        var stranger = await feed.FeedAsync(Guid.NewGuid().ToString(), 1, 10, AsOf, default);
        stranger.Variant.Should().Be(FeedService.ColdStartVariant);
        stranger.Items.Should().HaveCount(4).And.OnlyContain(i => i.Reason.Kind == "popular");

        var writer = new SearchIndexWriter(db.Index!);
        var catalog = await writer.LoadTagCatalogAsync(default);
        var illustrator = Docs.Olena with { SourceId = Reader, OwnerId = Reader, Title = "Іван", Roles = ["Illustrator"] };
        try
        {
            await writer.SyncKindAsync("user", [.. new[] { Docs.Olena, illustrator }.Select(d => SearchIndexWriter.BuildRow(d, catalog))], default);
            var r = await feed.FeedAsync(Reader, 1, 10, AsOf, default);
            r.Variant.Should().Be(FeedService.ColdStartVariant);
            r.Items.Take(2).Select(i => i.Post.Id).Should().BeEquivalentTo([Docs.BookIllustration.Id, Docs.TestPost.Id]);
            r.Items.Take(2).Should().OnlyContain(i => i.Reason.Kind == "specialization");
        }
        finally
        {
            await writer.SyncKindAsync("user", [SearchIndexWriter.BuildRow(Docs.Olena, catalog)], default);
        }
    }

    [SkippableFact]
    public async Task Pages_do_not_overlap()
    {
        var feed = Feed();
        await Interactions();
        var first = await feed.FeedAsync(Reader, 1, 2, AsOf, default);
        var second = await feed.FeedAsync(Reader, 2, 2, AsOf, default);
        first.HasMore.Should().BeTrue();
        second.HasMore.Should().BeFalse();
        first.Items.Concat(second.Items).Select(i => i.Post.Id).Should().OnlyHaveUniqueItems().And.HaveCount(4);
    }

    [SkippableFact]
    public async Task Events_are_counted_per_variant()
    {
        Feed();
        var log = new FeedEventLog(db.Index!);
        var written = await log.WriteAsync(Reader, new FeedEventBatch("test-variant",
        [
            new FeedEvent(Docs.TestPost.Id, "impression", 0), new FeedEvent(Docs.Song.Id, "impression", 1),
            new FeedEvent(Docs.Song.Id, "click", 1), new FeedEvent(Docs.Song.Id, "hover", 1),
        ]), default);
        written.Should().Be(3, "unknown event types are dropped");

        var stats = (await log.StatsAsync(TimeSpan.FromDays(1), default)).Single(s => s.Variant == "test-variant");
        stats.Should().BeEquivalentTo(new { Impressions = 2, Clicks = 1, ClickRate = 0.5, ClicksInTop3 = 1.0 });
    }
}
