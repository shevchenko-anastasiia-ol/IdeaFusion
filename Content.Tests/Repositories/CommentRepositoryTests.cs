using ContentDAL.Repository;
using ContentDomain.Entity;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Content.Tests.Repositories;

public class CommentRepositoryTests : IDisposable
{
    private readonly SqliteConnection _db = DbHelper.OpenDb();

    public void Dispose() => _db.Dispose();

    private CommentRepository Repo() => new(_db);

    // seed helpers
    private async Task<int> SeedAuthorAsync(int userId = 1, string userName = "alice")
    {
        return await _db.ExecuteScalarAsync<int>(
            "INSERT INTO PostAuthors (UserId, UserName, SyncedAt) VALUES (@UserId, @UserName, @SyncedAt) RETURNING PostAuthorId",
            new { UserId = userId, UserName = userName, SyncedAt = DateTime.UtcNow.ToString("o") });
    }

    private Comment MakeComment(int postId = 1, int postAuthorId = 1) => new()
    {
        PostId       = postId,
        PostAuthorId = postAuthorId,
        Body         = "Test body",
        CreatedAt    = DateTime.UtcNow,
        IsDeleted    = false
    };

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_SetsCommentId()
    {
        var comment = MakeComment();
        await Repo().AddAsync(comment);
        comment.CommentId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_PersistsBodyInDatabase()
    {
        var comment = MakeComment();
        await Repo().AddAsync(comment);

        var body = await _db.ExecuteScalarAsync<string>(
            "SELECT Body FROM Comments WHERE CommentId = @Id", new { Id = comment.CommentId });
        body.Should().Be("Test body");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesBodyInDatabase()
    {
        var comment = MakeComment();
        await Repo().AddAsync(comment);
        comment.Body      = "Updated body";
        comment.UpdatedAt = DateTime.UtcNow;

        await Repo().UpdateAsync(comment);

        var body = await _db.ExecuteScalarAsync<string>(
            "SELECT Body FROM Comments WHERE CommentId = @Id", new { Id = comment.CommentId });
        body.Should().Be("Updated body");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletesComment()
    {
        var comment = MakeComment();
        await Repo().AddAsync(comment);

        await Repo().DeleteAsync(comment.CommentId);

        var isDeleted = await _db.ExecuteScalarAsync<int>(
            "SELECT IsDeleted FROM Comments WHERE CommentId = @Id", new { Id = comment.CommentId });
        isDeleted.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        Func<Task> act = () => Repo().DeleteAsync(9999);
        await act.Should().NotThrowAsync();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsCommentWithCorrectId()
    {
        var authorId = await SeedAuthorAsync();
        var comment  = MakeComment(postAuthorId: authorId);
        await Repo().AddAsync(comment);

        var result = await Repo().GetByIdAsync(comment.CommentId);

        result.Should().NotBeNull();
        result!.CommentId.Should().Be(comment.CommentId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var result = await Repo().GetByIdAsync(9999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenSoftDeleted_ReturnsNull()
    {
        var comment = MakeComment();
        await Repo().AddAsync(comment);
        await Repo().DeleteAsync(comment.CommentId);

        var result = await Repo().GetByIdAsync(comment.CommentId);
        result.Should().BeNull();
    }

    // ── GetByPostIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPostIdAsync_ReturnsTopLevelCommentsForPost()
    {
        var authorId = await SeedAuthorAsync();
        var c1       = MakeComment(postId: 10, postAuthorId: authorId);
        var c2       = MakeComment(postId: 10, postAuthorId: authorId);
        var other    = MakeComment(postId: 99, postAuthorId: authorId);
        await Repo().AddAsync(c1);
        await Repo().AddAsync(c2);
        await Repo().AddAsync(other);

        var result = (await Repo().GetByPostIdAsync(10)).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.PostId.Should().Be(10));
    }

    [Fact]
    public async Task GetByPostIdAsync_ExcludesSoftDeletedComments()
    {
        var authorId = await SeedAuthorAsync();
        var c        = MakeComment(postId: 5, postAuthorId: authorId);
        await Repo().AddAsync(c);
        await Repo().DeleteAsync(c.CommentId);

        var result = await Repo().GetByPostIdAsync(5);
        result.Should().BeEmpty();
    }

    // ── GetRepliesAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRepliesAsync_ReturnsChildComments()
    {
        var authorId = await SeedAuthorAsync();
        var parent   = MakeComment(postId: 1, postAuthorId: authorId);
        await Repo().AddAsync(parent);

        // Insert reply directly with ParentCommentId
        await _db.ExecuteAsync(
            "INSERT INTO Comments (PostId, PostAuthorId, ParentCommentId, Body, CreatedAt, IsDeleted) VALUES (1, @AId, @PId, 'reply', @Now, 0)",
            new { AId = authorId, PId = parent.CommentId, Now = DateTime.UtcNow.ToString("o") });

        var replies = (await Repo().GetRepliesAsync(parent.CommentId)).ToList();
        replies.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRepliesAsync_ReturnsEmpty_WhenNoReplies()
    {
        var result = await Repo().GetRepliesAsync(9999);
        result.Should().BeEmpty();
    }
}
