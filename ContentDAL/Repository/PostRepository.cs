using System.Data;
using ContentDAL.Repository.Interfaces;
using ContentDomain.Entity;
using Dapper;
using Microsoft.AspNetCore.Http;
using Minio;
using Minio.DataModel.Args;

namespace ContentDAL.Repository;

public class PostRepository : IPostRepository
{
    private readonly IDbConnection _connection;
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _endpoint;
    protected readonly IDbTransaction? _transaction;

    public PostRepository(IDbConnection connection, IMinioClient minioClient, string bucketName, string endpoint, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
        _minioClient = minioClient;
        _bucketName = bucketName;
        _endpoint = endpoint.TrimEnd('/');
    }

    public Task<string> GetMediaUrlAsync(PostMedia media, CancellationToken ct = default)
    {
        var url = $"http://{_endpoint}/{media.Bucket}/{media.ObjectName}";
        return Task.FromResult(url);
    }

    public async Task<PostMedia> UploadMediaAsync(int postId, IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));

        var bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName), ct);
        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucketName), ct);
            var policy = $$"""{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{_bucketName}}/*"]}]}""";
            await _minioClient.SetPolicyAsync(
                new SetPolicyArgs().WithBucket(_bucketName).WithPolicy(policy), ct);
        }

        var objectName = $"{Guid.NewGuid()}_{file.FileName}";
        var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;

        await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(file.OpenReadStream())
                .WithObjectSize(file.Length)
                .WithContentType(contentType),
            ct);

        var media = new PostMedia
        {
            PostId = postId,
            ObjectName = objectName,
            Bucket = _bucketName,
            ContentType = contentType
        };

        await AddPostMediaAsync(media, ct);

        return media;
    }

    private async Task AddPostMediaAsync(PostMedia media, CancellationToken ct)
    {
        var sql = @"INSERT INTO PostMedia (PostId, ObjectName, Bucket, ContentType)
                    VALUES (@PostId, @ObjectName, @Bucket, @ContentType);";

        await _connection.ExecuteAsync(sql, media, transaction: _transaction);
    }

    public async Task DeleteMediaAsync(int postId, CancellationToken ct = default)
    {
        var mediaList = await GetMediaByPostIdAsync(postId, ct);

        foreach (var media in mediaList)
        {
            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(media.Bucket)
                .WithObject(media.ObjectName));
        }

        var sql = "DELETE FROM PostMedia WHERE PostId = @PostId;";
        await _connection.ExecuteAsync(sql, new { PostId = postId }, transaction: _transaction);
    }

    public async Task<IEnumerable<PostMedia>> GetMediaByPostIdAsync(int postId, CancellationToken ct = default)
    {
        var sql = @"SELECT Id, PostId, ObjectName, Bucket, ContentType
                    FROM PostMedia
                    WHERE PostId = @PostId;";

        var mediaList = await _connection.QueryAsync<PostMedia>(sql, new { PostId = postId }, transaction: _transaction);
        return mediaList;
    }

    public async Task AddAsync(Post entity, CancellationToken ct = default)
    {
        var sql = @"INSERT INTO Posts
                        (PostAuthorId, CollaborationSnapshotId, Title, Description,
                         ExternalLink, Status, CreatedAt, CreatedBy, IsDeleted)
                    VALUES
                        (@PostAuthorId, @CollaborationSnapshotId, @Title, @Description,
                         @ExternalLink, @Status, @CreatedAt, @CreatedBy, @IsDeleted)
                    RETURNING PostId;";

        entity.PostId = await _connection.ExecuteScalarAsync<int>(sql, entity, transaction: _transaction);

        foreach (var media in entity.Media)
        {
            var sqlMedia = @"INSERT INTO PostMedia
                                (PostId, Bucket, ObjectName, ContentType)
                             VALUES
                                (@PostId, @Bucket, @ObjectName, @ContentType);";

            await _connection.ExecuteAsync(sqlMedia, new
            {
                PostId = entity.PostId,
                Bucket = media.Bucket,
                ObjectName = media.ObjectName,
                ContentType = media.ContentType
            }, transaction: _transaction);
        }
    }

    public async Task UpdateAsync(Post entity, CancellationToken ct = default)
    {
        var sql = @"UPDATE Posts
                    SET Title       = @Title,
                        Description = @Description,
                        ExternalLink= @ExternalLink,
                        Status      = @Status,
                        UpdatedAt   = @UpdatedAt,
                        UpdatedBy   = @UpdatedBy
                    WHERE PostId = @PostId AND IsDeleted = false;";

        await _connection.ExecuteAsync(sql, new
        {
            entity.Title,
            entity.Description,
            entity.ExternalLink,
            Status = entity.Status.ToString(),
            entity.UpdatedAt,
            entity.UpdatedBy,
            entity.PostId,
        }, transaction: _transaction);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var sql = @"UPDATE Posts
                    SET IsDeleted = true, UpdatedAt = @UpdatedAt
                    WHERE PostId = @Id AND IsDeleted = false;";

        await _connection.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }, transaction: _transaction);
    }

    public async Task ArchiveAsync(int id, CancellationToken ct = default)
    {
        var sql = @"UPDATE Posts
                    SET Status = @Status, UpdatedAt = @UpdatedAt
                    WHERE PostId = @Id AND IsDeleted = false;";

        await _connection.ExecuteAsync(sql,
            new { Id = id, Status = PostStatus.Archived.ToString(), UpdatedAt = DateTime.UtcNow },
            transaction: _transaction);
    }

    public async Task<Post?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var sql = @"SELECT p.*, a.*, c.*
                    FROM Posts p
                    LEFT JOIN PostAuthors a            ON p.PostAuthorId = a.PostAuthorId
                    LEFT JOIN CollaborationSnapshots c ON p.CollaborationSnapshotId = c.CollaborationSnapshotId
                    WHERE p.PostId = @Id AND p.IsDeleted = false;";

        var post = (await _connection.QueryAsync<Post, PostAuthor, CollaborationSnapshot, Post>(
            sql,
            (p, author, collab) =>
            {
                p.Author        = author;
                p.Collaboration = collab;
                return p;
            },
            new { Id = id },
            transaction: _transaction,
            splitOn: "PostAuthorId,CollaborationSnapshotId")).FirstOrDefault();

        if (post != null)
        {
            var mediaSql = @"SELECT * FROM PostMedia WHERE PostId = @PostId;";
            var media = await _connection.QueryAsync<PostMedia>(mediaSql, new { PostId = id }, transaction: _transaction);
            post.Media = media.ToList();
        }

        return post;
    }

    public async Task<IEnumerable<Post>> GetAllAsync(CancellationToken ct = default)
    {
        var sql = @"SELECT p.*, a.*, c.*
                    FROM Posts p
                    LEFT JOIN PostAuthors a            ON p.PostAuthorId = a.PostAuthorId
                    LEFT JOIN CollaborationSnapshots c ON p.CollaborationSnapshotId = c.CollaborationSnapshotId
                    WHERE p.IsDeleted = false
                    ORDER BY p.CreatedAt DESC;";

        return await _connection.QueryAsync<Post, PostAuthor, CollaborationSnapshot, Post>(
            sql,
            (post, author, collab) =>
            {
                post.Author        = author;
                post.Collaboration = collab;
                return post;
            },
            transaction: _transaction,
            splitOn: "PostAuthorId,CollaborationSnapshotId");
    }

    public async Task<IEnumerable<Post>> GetByAuthorAsync(int postAuthorId, CancellationToken ct = default)
    {
        var sql = @"SELECT p.*, a.*, c.*
                    FROM Posts p
                    LEFT JOIN PostAuthors a            ON p.PostAuthorId = a.PostAuthorId
                    LEFT JOIN CollaborationSnapshots c ON p.CollaborationSnapshotId = c.CollaborationSnapshotId
                    WHERE p.PostAuthorId = @PostAuthorId AND p.IsDeleted = false
                    ORDER BY p.CreatedAt DESC;";

        return await _connection.QueryAsync<Post, PostAuthor, CollaborationSnapshot, Post>(
            sql,
            (post, author, collab) =>
            {
                post.Author        = author;
                post.Collaboration = collab;
                return post;
            },
            new { PostAuthorId = postAuthorId },
            transaction: _transaction,
            splitOn: "PostAuthorId,CollaborationSnapshotId");
    }

    public async Task<IEnumerable<Post>> GetByCollaborationAsync(int collaborationSnapshotId, CancellationToken ct = default)
    {
        var sql = @"SELECT p.*, a.*, c.*
                    FROM Posts p
                    LEFT JOIN PostAuthors a            ON p.PostAuthorId = a.PostAuthorId
                    LEFT JOIN CollaborationSnapshots c ON p.CollaborationSnapshotId = c.CollaborationSnapshotId
                    WHERE p.CollaborationSnapshotId = @CollaborationSnapshotId AND p.IsDeleted = false
                    ORDER BY p.CreatedAt DESC;";

        return await _connection.QueryAsync<Post, PostAuthor, CollaborationSnapshot, Post>(
            sql,
            (post, author, collab) =>
            {
                post.Author        = author;
                post.Collaboration = collab;
                return post;
            },
            new { CollaborationSnapshotId = collaborationSnapshotId },
            transaction: _transaction,
            splitOn: "PostAuthorId,CollaborationSnapshotId");
    }

    public async Task<IEnumerable<Post>> GetByStatusAsync(PostStatus status, CancellationToken ct = default)
    {
        var sql = @"SELECT p.*, a.*, c.*
                    FROM Posts p
                    LEFT JOIN PostAuthors a            ON p.PostAuthorId = a.PostAuthorId
                    LEFT JOIN CollaborationSnapshots c ON p.CollaborationSnapshotId = c.CollaborationSnapshotId
                    WHERE p.Status = @Status AND p.IsDeleted = false
                    ORDER BY p.CreatedAt DESC;";

        return await _connection.QueryAsync<Post, PostAuthor, CollaborationSnapshot, Post>(
            sql,
            (post, author, collab) =>
            {
                post.Author        = author;
                post.Collaboration = collab;
                return post;
            },
            new { Status = status.ToString() },
            transaction: _transaction,
            splitOn: "PostAuthorId,CollaborationSnapshotId");
    }

    public async Task<IEnumerable<Post>> GetByTagAsync(int tagId, CancellationToken ct = default)
    {
        var sql = @"SELECT p.*, a.*, c.*
                    FROM Posts p
                    INNER JOIN PostTags pt             ON p.PostId = pt.PostId
                    LEFT JOIN PostAuthors a            ON p.PostAuthorId = a.PostAuthorId
                    LEFT JOIN CollaborationSnapshots c ON p.CollaborationSnapshotId = c.CollaborationSnapshotId
                    WHERE pt.TagId = @TagId AND p.IsDeleted = false
                    ORDER BY p.CreatedAt DESC;";

        return await _connection.QueryAsync<Post, PostAuthor, CollaborationSnapshot, Post>(
            sql,
            (post, author, collab) =>
            {
                post.Author        = author;
                post.Collaboration = collab;
                return post;
            },
            new { TagId = tagId },
            transaction: _transaction,
            splitOn: "PostAuthorId,CollaborationSnapshotId");
    }

    public async Task<IEnumerable<Post>> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        var sql = @"SELECT p.*, a.*, c.*
                    FROM Posts p
                    LEFT JOIN PostAuthors a            ON p.PostAuthorId = a.PostAuthorId
                    LEFT JOIN CollaborationSnapshots c ON p.CollaborationSnapshotId = c.CollaborationSnapshotId
                    WHERE a.UserId = @UserId AND p.IsDeleted = false
                    ORDER BY p.CreatedAt DESC;";

        return await _connection.QueryAsync<Post, PostAuthor, CollaborationSnapshot, Post>(
            sql,
            (post, author, collab) =>
            {
                post.Author        = author;
                post.Collaboration = collab;
                return post;
            },
            new { UserId = userId },
            transaction: _transaction,
            splitOn: "PostAuthorId,CollaborationSnapshotId");
    }

    public async Task<int> EnsurePostAuthorAsync(string userName, string? avatarUrl = null, CancellationToken ct = default)
    {
        var existing = await _connection.QuerySingleOrDefaultAsync<(int UserId, string? ExistingAvatar)>(
            "SELECT UserId, AvatarUrl FROM PostAuthors WHERE UserName = @UserName LIMIT 1;",
            new { UserName = userName });

        if (existing != default)
        {
            if (avatarUrl != null && existing.ExistingAvatar != avatarUrl)
            {
                await _connection.ExecuteAsync(
                    "UPDATE PostAuthors SET AvatarUrl = @AvatarUrl, SyncedAt = @SyncedAt WHERE UserName = @UserName;",
                    new { AvatarUrl = avatarUrl, SyncedAt = DateTime.UtcNow, UserName = userName });
            }
            return existing.UserId;
        }

        var newUserId = await _connection.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(UserId), 200) + 1 FROM PostAuthors;");

        await _connection.ExecuteAsync(
            "INSERT INTO PostAuthors (UserId, UserName, AvatarUrl, SyncedAt) VALUES (@UserId, @UserName, @AvatarUrl, @SyncedAt);",
            new { UserId = newUserId, UserName = userName, AvatarUrl = avatarUrl, SyncedAt = DateTime.UtcNow });

        return newUserId;
    }

    public async Task<int> EnsurePostAuthorByUserIdAsync(int userId, string userName, string? avatarUrl = null, CancellationToken ct = default)
    {
        var existing = await _connection.QuerySingleOrDefaultAsync<(int PostAuthorId, string? ExistingAvatar)>(
            "SELECT PostAuthorId, AvatarUrl FROM PostAuthors WHERE UserId = @UserId LIMIT 1;",
            new { UserId = userId });

        if (existing != default)
        {
            if (avatarUrl != null && existing.ExistingAvatar != avatarUrl)
            {
                await _connection.ExecuteAsync(
                    "UPDATE PostAuthors SET AvatarUrl = @AvatarUrl, SyncedAt = @SyncedAt WHERE UserId = @UserId;",
                    new { AvatarUrl = avatarUrl, SyncedAt = DateTime.UtcNow, UserId = userId });
            }
            return existing.PostAuthorId;
        }

        return await _connection.ExecuteScalarAsync<int>(
            "INSERT INTO PostAuthors (UserId, UserName, AvatarUrl, SyncedAt) VALUES (@UserId, @UserName, @AvatarUrl, @SyncedAt) RETURNING PostAuthorId;",
            new { UserId = userId, UserName = userName, AvatarUrl = avatarUrl, SyncedAt = DateTime.UtcNow });
    }
}