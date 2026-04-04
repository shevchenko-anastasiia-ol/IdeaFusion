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
    protected readonly IDbTransaction? _transaction;

    public PostRepository(IDbConnection connection, IMinioClient minioClient, string bucketName, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
        _minioClient = minioClient;
        _bucketName = bucketName;
    }

    public Task<string> GetMediaUrlAsync(PostMedia media, CancellationToken ct = default)
    {
        var url = $"https://{media.Bucket}.s3.amazonaws.com/{media.ObjectName}";
        return Task.FromResult(url);
    }

    public async Task<PostMedia> UploadMediaAsync(int postId, IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));

        var objectName = $"{Guid.NewGuid()}_{file.FileName}";

        await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(file.OpenReadStream())
                .WithObjectSize(file.Length)
                .WithContentType(file.ContentType),
            ct);

        var media = new PostMedia
        {
            PostId = postId,
            ObjectName = objectName,
            Bucket = _bucketName,
            ContentType = file.ContentType
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

        await _connection.ExecuteAsync(sql, entity, transaction: _transaction);

        var existingMedia = (await _connection.QueryAsync<PostMedia>(
            "SELECT * FROM PostMedia WHERE PostId = @PostId;",
            new { PostId = entity.PostId },
            transaction: _transaction)).ToList();

        var mediaToDelete = existingMedia
            .Where(em => !entity.Media.Any(m => m.ObjectName == em.ObjectName))
            .ToList();

        foreach (var media in mediaToDelete)
        {
            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(media.ObjectName));

            await _connection.ExecuteAsync(
                "DELETE FROM PostMedia WHERE Id = @Id;",
                new { media.Id },
                transaction: _transaction);
        }

        var mediaToAdd = entity.Media
            .Where(m => !existingMedia.Any(em => em.ObjectName == m.ObjectName))
            .ToList();

        foreach (var media in mediaToAdd)
        {
            await _connection.ExecuteAsync(
                @"INSERT INTO PostMedia (PostId, Bucket, ObjectName, ContentType)
                  VALUES (@PostId, @Bucket, @ObjectName, @ContentType);",
                new
                {
                    PostId = entity.PostId,
                    Bucket = media.Bucket,
                    ObjectName = media.ObjectName,
                    ContentType = media.ContentType
                },
                transaction: _transaction);
        }
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
}