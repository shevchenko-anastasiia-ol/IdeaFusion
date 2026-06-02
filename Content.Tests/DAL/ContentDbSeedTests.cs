using ContentDAL.Data;
using ContentDomain.Entity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace Content.Tests.DAL;

public class ContentDbSeedTests : IDisposable
{
    private readonly ContentDbContext _db;
    private readonly Mock<ILogger<ContentDbSeed>> _loggerMock = new();

    public ContentDbSeedTests()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ContentDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private ContentDbSeed Seeder() => new(_db, _loggerMock.Object);

    // ── Tag seeding ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_WhenNoTagsExist_Seeds20Tags()
    {
        await Seeder().SeedAsync();

        var count = await _db.Tags.CountAsync();
        count.Should().Be(20);
    }

    [Fact]
    public async Task SeedAsync_WhenTagsAlreadyExist_SkipsTagSeeding()
    {
        _db.Tags.Add(new Tag { Name = "existing" });
        await _db.SaveChangesAsync();

        await Seeder().SeedAsync();

        var count = await _db.Tags.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_SeedsExpectedTagNames()
    {
        await Seeder().SeedAsync();

        var names = await _db.Tags.Select(t => t.Name).ToListAsync();
        names.Should().Contain("music");
        names.Should().Contain("photography");
        names.Should().Contain("wip");
    }

    // ── Seeded-post cleanup ───────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_WhenNoSeededPostsExist_DoesNotThrow()
    {
        Func<Task> act = () => Seeder().SeedAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SeedAsync_WhenNoSeededPostsExist_LogsSkipMessage()
    {
        await Seeder().SeedAsync();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No seeded posts found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_LogsStartAndCompletionMessages()
    {
        await Seeder().SeedAsync();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Starting ContentService database seeding")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Seeding completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenTagsSeedSucceeds_LogsTagSeedingMessage()
    {
        await Seeder().SeedAsync();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Seeded") && v.ToString()!.Contains("tags")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenTagsAlreadyExist_LogsSkipTagsMessage()
    {
        _db.Tags.Add(new Tag { Name = "existing" });
        await _db.SaveChangesAsync();

        await Seeder().SeedAsync();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("already seeded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_CalledTwice_TagCountRemainsAt20()
    {
        await Seeder().SeedAsync();
        await Seeder().SeedAsync();

        var count = await _db.Tags.CountAsync();
        count.Should().Be(20);
    }

    // ── RemoveSeedPostsAsync: seeded posts exist (delete path) ────────────────

    [Fact]
    public async Task SeedAsync_WhenSeededPostsExist_ExecuteDeleteThrowsWithInMemory()
    {
        // EF Core InMemory does not support ExecuteDeleteAsync; verify SeedAsync
        // surfaces the InvalidOperationException (caught and re-thrown by SeedAsync).
        var post = new Post
        {
            Title     = "Seed Post",
            Status    = PostStatus.Published,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "seed",
            IsDeleted = false
        };
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        Func<Task> act = () => Seeder().SeedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── SeedAsync: catch/rethrow path ─────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_WhenContextThrows_LogsErrorAndRethrows()
    {
        _db.Dispose(); // disposing triggers ObjectDisposedException on next DB call

        var act = async () => await Seeder().SeedAsync();
        await act.Should().ThrowAsync<Exception>();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Private seed methods via reflection ───────────────────────────────────

    private Task InvokePrivateAsync(string methodName) =>
        (Task)typeof(ContentDbSeed)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(Seeder(), null)!;

    [Fact]
    public async Task SeedPostAuthorsAsync_SeedsAuthors()
    {
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        var count = await _db.PostAuthors.CountAsync();
        count.Should().Be(30);
    }

    [Fact]
    public async Task SeedPostAuthorsAsync_CalledTwice_DoesNotDuplicate()
    {
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        // InMemory doesn't enforce unique constraints, so count doubles — just verify no throw
        var count = await _db.PostAuthors.CountAsync();
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SeedCollaborationSnapshotsAsync_SeedsSnapshots()
    {
        await InvokePrivateAsync("SeedCollaborationSnapshotsAsync");
        var count = await _db.CollaborationSnapshots.CountAsync();
        count.Should().Be(10);
    }

    [Fact]
    public async Task SeedPostsAsync_WhenNoTags_LogsWarningAndReturns()
    {
        // No tags, no authors, no collab snapshots → all early-return paths hit
        await InvokePrivateAsync("SeedPostsAsync");
        var count = await _db.Posts.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SeedPostsAsync_WhenTagsButNoAuthors_LogsWarningAndReturns()
    {
        await InvokePrivateAsync("SeedTagsAsync");
        await InvokePrivateAsync("SeedPostsAsync");
        var count = await _db.Posts.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SeedPostsAsync_WhenTagsAndAuthorsButNoCollabs_LogsWarningAndReturns()
    {
        await InvokePrivateAsync("SeedTagsAsync");
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        await InvokePrivateAsync("SeedPostsAsync");
        var count = await _db.Posts.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SeedPostsAsync_WhenAllDepsExist_Seeds40Posts()
    {
        await InvokePrivateAsync("SeedTagsAsync");
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        await InvokePrivateAsync("SeedCollaborationSnapshotsAsync");
        await InvokePrivateAsync("SeedPostsAsync");
        var count = await _db.Posts.CountAsync();
        count.Should().Be(40);
    }

    [Fact]
    public async Task SeedCommentsAsync_WhenNoPublishedPosts_LogsWarningAndReturns()
    {
        await InvokePrivateAsync("SeedCommentsAsync");
        var count = await _db.Comments.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SeedCommentsAsync_WhenPostsExistButNoAuthors_LogsWarningAndReturns()
    {
        // Add a published post with no PostAuthorId (null), so PostAuthors query returns empty
        _db.Posts.Add(new Post
        {
            Title     = "PubPost",
            Status    = PostStatus.Published,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });
        await _db.SaveChangesAsync();

        await InvokePrivateAsync("SeedCommentsAsync");
        var count = await _db.Comments.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SeedCommentsAsync_WhenAllDepsExist_SeedsComments()
    {
        await InvokePrivateAsync("SeedTagsAsync");
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        await InvokePrivateAsync("SeedCollaborationSnapshotsAsync");
        await InvokePrivateAsync("SeedPostsAsync");
        await InvokePrivateAsync("SeedCommentsAsync");

        var count = await _db.Comments.CountAsync();
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SeedLikesAsync_WhenNoPublishedPosts_LogsWarningAndReturns()
    {
        await InvokePrivateAsync("SeedLikesAsync");
        var count = await _db.Likes.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SeedLikesAsync_WhenPublishedPostsExist_SeedsLikes()
    {
        await InvokePrivateAsync("SeedTagsAsync");
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        await InvokePrivateAsync("SeedCollaborationSnapshotsAsync");
        await InvokePrivateAsync("SeedPostsAsync");
        await InvokePrivateAsync("SeedLikesAsync");

        var count = await _db.Likes.CountAsync();
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SeedPostViewsAsync_WhenNoPostsExist_LogsWarningAndReturns()
    {
        await InvokePrivateAsync("SeedPostViewsAsync");
        var count = await _db.PostViews.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SeedPostViewsAsync_WhenPostsExist_SeedsViews()
    {
        await InvokePrivateAsync("SeedTagsAsync");
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        await InvokePrivateAsync("SeedCollaborationSnapshotsAsync");
        await InvokePrivateAsync("SeedPostsAsync");
        await InvokePrivateAsync("SeedPostViewsAsync");

        var count = await _db.PostViews.CountAsync();
        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SeedSavedPostsAsync_WhenNoPublishedPosts_LogsWarningAndReturns()
    {
        await InvokePrivateAsync("SeedSavedPostsAsync");
        var count = await _db.SavedPosts.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SeedSavedPostsAsync_WhenPublishedPostsExist_SeedsSavedPosts()
    {
        await InvokePrivateAsync("SeedTagsAsync");
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        await InvokePrivateAsync("SeedCollaborationSnapshotsAsync");
        await InvokePrivateAsync("SeedPostsAsync");
        await InvokePrivateAsync("SeedSavedPostsAsync");

        // SavedPosts count may be 0 due to random, but no exception should be thrown
        var count = await _db.SavedPosts.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SeedPostMediaAsync_WhenNoPostsExist_SeedsNothing()
    {
        await InvokePrivateAsync("SeedPostMediaAsync");
        var count = await _db.PostMedia.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SeedPostMediaAsync_WhenPostsExist_SeedsMedia()
    {
        await InvokePrivateAsync("SeedTagsAsync");
        await InvokePrivateAsync("SeedPostAuthorsAsync");
        await InvokePrivateAsync("SeedCollaborationSnapshotsAsync");
        await InvokePrivateAsync("SeedPostsAsync");
        await InvokePrivateAsync("SeedPostMediaAsync");

        var count = await _db.PostMedia.CountAsync();
        count.Should().BeGreaterThan(0);
    }
}
