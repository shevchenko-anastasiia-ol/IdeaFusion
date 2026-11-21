using ContentDAL.Repository;
using ContentDomain.Entity;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Minio;
using Minio.DataModel.Args;
using Moq;
using Xunit;

namespace Content.Tests.Repositories;

public class PostRepositoryTests : IDisposable
{
    private readonly SqliteConnection _db = DbHelper.OpenDb();
    private readonly Mock<IMinioClient> _minioMock = new();

    public void Dispose() => _db.Dispose();

    private PostRepository Repo() =>
        new(_db, _minioMock.Object, "test-bucket", "minio.local", null);

    private PostAuthorRepository AuthorRepo() => new(_db);

    private Post MakePost(int? postAuthorId = null) => new()
    {
        PostAuthorId = postAuthorId,
        Title        = "Sample Post",
        Status       = PostStatus.Published,
        CreatedAt    = DateTime.UtcNow,
        IsDeleted    = false
    };

    private async Task<int> SeedAuthorAsync(int userId = 1, string name = "bob")
    {
        return await _db.ExecuteScalarAsync<int>(
            "INSERT INTO PostAuthors (UserId, UserName, SyncedAt) VALUES (@UserId, @Name, @Now) RETURNING PostAuthorId",
            new { UserId = userId, Name = name, Now = DateTime.UtcNow.ToString("o") });
    }

    private async Task<Post> SeedPostAsync(int? authorId = null, string title = "Seeded", string status = "Published")
    {
        var postId = await _db.ExecuteScalarAsync<int>(
            @"INSERT INTO Posts (PostAuthorId, Title, Status, CreatedAt, IsDeleted)
              VALUES (@AuthorId, @Title, @Status, @Now, 0) RETURNING PostId",
            new { AuthorId = authorId, Title = title, Status = status, Now = DateTime.UtcNow.ToString("o") });
        return new Post { PostId = postId, PostAuthorId = authorId, Title = title };
    }

    // ── GetMediaUrlAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMediaUrlAsync_ReturnsCorrectUrl()
    {
        var media = new PostMedia { Bucket = "my-bucket", ObjectName = "photo.jpg" };

        var url = await Repo().GetMediaUrlAsync(media);

        url.Should().Be("http://minio.local/my-bucket/photo.jpg");
    }

    [Fact]
    public async Task GetMediaUrlAsync_EndpointTrailingSlashIsStripped()
    {
        var repo = new PostRepository(_db, _minioMock.Object, "bucket", "host.io/", null);
        var media = new PostMedia { Bucket = "bucket", ObjectName = "file.png" };

        var url = await repo.GetMediaUrlAsync(media);

        url.Should().Be("http://host.io/bucket/file.png");
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_SetsPostId()
    {
        var post = MakePost();
        await Repo().AddAsync(post);
        post.PostId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddAsync_PersistsTitleInDatabase()
    {
        var post = MakePost();
        await Repo().AddAsync(post);

        var title = await _db.ExecuteScalarAsync<string>(
            "SELECT Title FROM Posts WHERE PostId = @Id", new { Id = post.PostId });
        title.Should().Be("Sample Post");
    }

    [Fact]
    public async Task AddAsync_WithMedia_InsertsMediaRows()
    {
        var post = MakePost();
        post.Media = new List<PostMedia>
        {
            new() { Bucket = "b", ObjectName = "img.jpg", ContentType = "image/jpeg" }
        };

        await Repo().AddAsync(post);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PostMedia WHERE PostId = @Id", new { Id = post.PostId });
        count.Should().Be(1);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesTitleInDatabase()
    {
        var seeded = await SeedPostAsync(title: "Old Title");
        seeded.Title     = "New Title";
        seeded.Status    = PostStatus.Published;
        seeded.UpdatedAt = DateTime.UtcNow;

        await Repo().UpdateAsync(seeded);

        var title = await _db.ExecuteScalarAsync<string>(
            "SELECT Title FROM Posts WHERE PostId = @Id", new { Id = seeded.PostId });
        title.Should().Be("New Title");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletesPost()
    {
        var seeded = await SeedPostAsync();

        await Repo().DeleteAsync(seeded.PostId);

        var isDeleted = await _db.ExecuteScalarAsync<int>(
            "SELECT IsDeleted FROM Posts WHERE PostId = @Id", new { Id = seeded.PostId });
        isDeleted.Should().Be(1);
    }

    // ── ArchiveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ArchiveAsync_SetsStatusToArchived()
    {
        var seeded = await SeedPostAsync(status: "Published");

        await Repo().ArchiveAsync(seeded.PostId);

        var status = await _db.ExecuteScalarAsync<string>(
            "SELECT Status FROM Posts WHERE PostId = @Id", new { Id = seeded.PostId });
        status.Should().Be("Archived");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsNonNull()
    {
        var seeded = await SeedPostAsync();

        var result = await Repo().GetByIdAsync(seeded.PostId);

        result.Should().NotBeNull();
        result!.PostId.Should().Be(seeded.PostId);
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
        var seeded = await SeedPostAsync();
        await Repo().DeleteAsync(seeded.PostId);

        var result = await Repo().GetByIdAsync(seeded.PostId);
        result.Should().BeNull();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllActivePosts()
    {
        await SeedPostAsync(title: "Post 1");
        await SeedPostAsync(title: "Post 2");

        var result = (await Repo().GetAllAsync()).ToList();

        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedPosts()
    {
        var seeded = await SeedPostAsync();
        await Repo().DeleteAsync(seeded.PostId);

        var result = await Repo().GetAllAsync();

        result.Should().NotContain(p => p.PostId == seeded.PostId);
    }

    // ── GetByAuthorAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByAuthorAsync_ReturnsOnlyMatchingAuthorPosts()
    {
        var authorId = await SeedAuthorAsync(userId: 42, name: "carol");
        await SeedPostAsync(authorId: authorId, title: "Carol's Post 1");
        await SeedPostAsync(authorId: authorId, title: "Carol's Post 2");
        await SeedPostAsync(authorId: 999, title: "Other Author");

        var result = (await Repo().GetByAuthorAsync(authorId)).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.PostId.Should().BeGreaterThan(0));
    }

    // ── GetMediaByPostIdAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetMediaByPostIdAsync_ReturnsMediaForPost()
    {
        var seeded = await SeedPostAsync();
        await _db.ExecuteAsync(
            "INSERT INTO PostMedia (PostId, ObjectName, Bucket, ContentType) VALUES (@PostId, 'img.jpg', 'bucket', 'image/jpeg')",
            new { PostId = seeded.PostId });
        await _db.ExecuteAsync(
            "INSERT INTO PostMedia (PostId, ObjectName, Bucket, ContentType) VALUES (@PostId, 'vid.mp4', 'bucket', 'video/mp4')",
            new { PostId = seeded.PostId });

        var result = (await Repo().GetMediaByPostIdAsync(seeded.PostId)).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.PostId.Should().Be(seeded.PostId));
    }

    // ── Author lookup by username (PostAuthorRepository) ─────────────────────

    [Fact]
    public async Task GetByUserNameAsync_WhenRowExists_ReturnsIt()
    {
        await SeedAuthorAsync(77, "existing");

        var author = await AuthorRepo().GetByUserNameAsync("existing");

        author.Should().NotBeNull();
        author!.UserId.Should().Be(77);
        author.PostAuthorId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByUserNameAsync_WhenNoRow_ReturnsNullAndCreatesNothing()
    {
        var author = await AuthorRepo().GetByUserNameAsync("nobody");

        author.Should().BeNull();
        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PostAuthors WHERE UserName = 'nobody'");
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetByExternalIdAsync_WhenRowExists_ReturnsIt()
    {
        var externalId = Guid.NewGuid().ToString();
        await _db.ExecuteAsync(
            "INSERT INTO PostAuthors (UserId, ExternalId, UserName, SyncedAt) VALUES (88, @ExternalId, 'veteran', @Now)",
            new { ExternalId = externalId, Now = DateTime.UtcNow.ToString("o") });

        var author = await AuthorRepo().GetByExternalIdAsync(externalId);

        author.Should().NotBeNull();
        author!.UserId.Should().Be(88);
        author.ExternalId.Should().Be(externalId);
    }

    // ── DeleteMediaAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMediaAsync_RemovesMediaRowsFromDatabase()
    {
        var seeded = await SeedPostAsync();
        await _db.ExecuteAsync(
            "INSERT INTO PostMedia (PostId, ObjectName, Bucket, ContentType) VALUES (@PostId, 'file.jpg', 'bucket', 'image/jpeg')",
            new { PostId = seeded.PostId });

        _minioMock
            .Setup(m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Repo().DeleteMediaAsync(seeded.PostId);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PostMedia WHERE PostId = @PostId", new { PostId = seeded.PostId });
        count.Should().Be(0);
    }

    // ── GetByCollaborationAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetByCollaborationAsync_ReturnsOnlyMatchingCollaborationPosts()
    {
        var collabId = await _db.ExecuteScalarAsync<int>(
            "INSERT INTO CollaborationSnapshots (CollaborationId, Name, SyncedAt) VALUES (1, 'Team Alpha', @Now) RETURNING CollaborationSnapshotId",
            new { Now = DateTime.UtcNow.ToString("o") });
        await _db.ExecuteAsync(
            "INSERT INTO Posts (CollaborationSnapshotId, Title, Status, CreatedAt, IsDeleted) VALUES (@C, 'Collab Post 1', 'Published', @Now, 0)",
            new { C = collabId, Now = DateTime.UtcNow.ToString("o") });
        await _db.ExecuteAsync(
            "INSERT INTO Posts (CollaborationSnapshotId, Title, Status, CreatedAt, IsDeleted) VALUES (@C, 'Collab Post 2', 'Draft', @Now, 0)",
            new { C = collabId, Now = DateTime.UtcNow.ToString("o") });
        await SeedPostAsync(title: "Other Post"); // no collaboration

        var result = (await Repo().GetByCollaborationAsync(collabId)).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.PostId.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetByCollaborationAsync_WhenNoPosts_ReturnsEmpty()
    {
        var result = await Repo().GetByCollaborationAsync(9999);
        result.Should().BeEmpty();
    }

    // ── GetByStatusAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByStatusAsync_ReturnsOnlyPostsWithMatchingStatus()
    {
        await SeedPostAsync(title: "Draft 1", status: "Draft");
        await SeedPostAsync(title: "Draft 2", status: "Draft");
        await SeedPostAsync(title: "Published 1", status: "Published");

        var result = (await Repo().GetByStatusAsync(PostStatus.Draft)).ToList();

        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Should().AllSatisfy(p => p.PostId.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetByStatusAsync_WhenNoPosts_ReturnsEmpty()
    {
        var result = await Repo().GetByStatusAsync(PostStatus.Archived);
        result.Should().BeEmpty();
    }

    // ── GetByTagAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTagAsync_ReturnsOnlyPostsWithMatchingTag()
    {
        var tagId  = await _db.ExecuteScalarAsync<int>(
            "INSERT INTO Tags (Name) VALUES ('dotnet') RETURNING TagId");
        var post1  = await SeedPostAsync(title: "Tagged Post 1");
        var post2  = await SeedPostAsync(title: "Tagged Post 2");
        await SeedPostAsync(title: "Untagged Post");

        await _db.ExecuteAsync(
            "INSERT INTO PostTags (PostId, TagId) VALUES (@PostId, @TagId)",
            new { PostId = post1.PostId, TagId = tagId });
        await _db.ExecuteAsync(
            "INSERT INTO PostTags (PostId, TagId) VALUES (@PostId, @TagId)",
            new { PostId = post2.PostId, TagId = tagId });

        var result = (await Repo().GetByTagAsync(tagId)).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.PostId.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetByTagAsync_WhenNoPostsHaveTag_ReturnsEmpty()
    {
        var result = await Repo().GetByTagAsync(99999);
        result.Should().BeEmpty();
    }

    // ── GetByUserIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_ReturnsPostsForMatchingUserId()
    {
        var authorId = await SeedAuthorAsync(userId: 555, name: "user555");
        await SeedPostAsync(authorId: authorId, title: "User Post 1");
        await SeedPostAsync(authorId: authorId, title: "User Post 2");
        await SeedPostAsync(title: "No Author Post");

        var result = (await Repo().GetByUserIdAsync(555)).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNoMatch_ReturnsEmpty()
    {
        var result = await Repo().GetByUserIdAsync(99999);
        result.Should().BeEmpty();
    }

    // ── UploadMediaAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UploadMediaAsync_NullFile_ThrowsArgumentException()
    {
        var act = async () => await Repo().UploadMediaAsync(1, null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UploadMediaAsync_EmptyFile_ThrowsArgumentException()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        var act = async () => await Repo().UploadMediaAsync(1, mockFile.Object);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UploadMediaAsync_WhenBucketExists_UploadsFile()
    {
        var seeded = await SeedPostAsync();

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[100]));

        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _minioMock
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);

        var result = await Repo().UploadMediaAsync(seeded.PostId, mockFile.Object);

        result.Should().NotBeNull();
        result.PostId.Should().Be(seeded.PostId);
        result.Bucket.Should().Be("test-bucket");
        result.ContentType.Should().Be("image/jpeg");

        _minioMock.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadMediaAsync_WhenBucketNotExists_CreatesBucketAndUploads()
    {
        var seeded = await SeedPostAsync();

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(50);
        mockFile.Setup(f => f.FileName).Returns("photo.png");
        mockFile.Setup(f => f.ContentType).Returns("image/png");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[50]));

        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _minioMock
            .Setup(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _minioMock
            .Setup(m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _minioMock
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);

        var result = await Repo().UploadMediaAsync(seeded.PostId, mockFile.Object);

        result.Should().NotBeNull();
        _minioMock.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _minioMock.Verify(
            m => m.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadMediaAsync_EmptyContentType_UsesOctetStream()
    {
        var seeded = await SeedPostAsync();

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(10);
        mockFile.Setup(f => f.FileName).Returns("data.bin");
        mockFile.Setup(f => f.ContentType).Returns(string.Empty);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[10]));

        _minioMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _minioMock
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);

        var result = await Repo().UploadMediaAsync(seeded.PostId, mockFile.Object);

        result.ContentType.Should().Be("application/octet-stream");
    }

    // ── EnsureByExternalIdAsync: minting and avatar refresh ───────────────────

    [Fact]
    public async Task EnsureByExternalIdAsync_WhenNotExists_InsertsAndReportsCreated()
    {
        var externalId = Guid.NewGuid().ToString();

        var (author, created) = await AuthorRepo().EnsureByExternalIdAsync(externalId, "newuser");

        created.Should().BeTrue();
        author.PostAuthorId.Should().BeGreaterThan(0);
        author.UserId.Should().BeGreaterThan(0);
        author.ExternalId.Should().Be(externalId);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM PostAuthors WHERE ExternalId = @ExternalId",
            new { ExternalId = externalId });
        count.Should().Be(1);
    }

    [Fact]
    public async Task EnsureByExternalIdAsync_WhenExists_ReturnsExistingRowAndRefreshesIt()
    {
        var externalId = Guid.NewGuid().ToString();
        await _db.ExecuteAsync(
            "INSERT INTO PostAuthors (UserId, ExternalId, UserName, SyncedAt) VALUES (77, @ExternalId, 'existing', @Now)",
            new { ExternalId = externalId, Now = DateTime.UtcNow.ToString("o") });

        var (author, created) = await AuthorRepo().EnsureByExternalIdAsync(externalId, "existing");

        created.Should().BeFalse();
        author.UserId.Should().Be(77);
        author.PostAuthorId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EnsureByExternalIdAsync_WhenAvatarDiffers_UpdatesAvatar()
    {
        var externalId = Guid.NewGuid().ToString();
        await _db.ExecuteAsync(
            "INSERT INTO PostAuthors (UserId, ExternalId, UserName, AvatarUrl, SyncedAt) VALUES (66, @ExternalId, 'user66', 'old-url', @Now)",
            new { ExternalId = externalId, Now = DateTime.UtcNow.ToString("o") });

        var (author, _) = await AuthorRepo().EnsureByExternalIdAsync(externalId, "user66", "new-url");

        author.UserId.Should().Be(66);
        var updated = await _db.ExecuteScalarAsync<string>(
            "SELECT AvatarUrl FROM PostAuthors WHERE ExternalId = @ExternalId",
            new { ExternalId = externalId });
        updated.Should().Be("new-url");
    }

    [Fact]
    public async Task EnsureByExternalIdAsync_WhenAvatarSame_KeepsIt()
    {
        var externalId = Guid.NewGuid().ToString();
        await _db.ExecuteAsync(
            "INSERT INTO PostAuthors (UserId, ExternalId, UserName, AvatarUrl, SyncedAt) VALUES (67, @ExternalId, 'user67', 'same-url', @Now)",
            new { ExternalId = externalId, Now = DateTime.UtcNow.ToString("o") });

        var (author, _) = await AuthorRepo().EnsureByExternalIdAsync(externalId, "user67", "same-url");

        author.UserId.Should().Be(67);
        author.AvatarUrl.Should().Be("same-url");
    }

    [Fact]
    public async Task EnsureByExternalIdAsync_WhenNoAvatarPassed_KeepsTheStoredOne()
    {
        var externalId = Guid.NewGuid().ToString();
        await _db.ExecuteAsync(
            "INSERT INTO PostAuthors (UserId, ExternalId, UserName, AvatarUrl, SyncedAt) VALUES (68, @ExternalId, 'user68', 'some-url', @Now)",
            new { ExternalId = externalId, Now = DateTime.UtcNow.ToString("o") });

        await AuthorRepo().EnsureByExternalIdAsync(externalId, "user68");

        var avatar = await _db.ExecuteScalarAsync<string>(
            "SELECT AvatarUrl FROM PostAuthors WHERE ExternalId = @ExternalId",
            new { ExternalId = externalId });
        avatar.Should().Be("some-url");
    }
}
