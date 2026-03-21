using System.Data;
using ContentDAL.Repository.Interfaces;
using ContentDomain.Entity;
using Dapper;

namespace ContentDAL.Repository;

public class PostRepository : IPostRepository
{
    private readonly IDbConnection _connection;
    protected readonly IDbTransaction? _transaction;
 
    public PostRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }
 
    public async Task AddAsync(Post entity, CancellationToken ct = default)
    {
        var sql = @"INSERT INTO Posts
                        (PostAuthorId, CollaborationSnapshotId, Title, Description,
                         MediaUrl, ExternalLink, Status, CreatedAt, CreatedBy, IsDeleted)
                    VALUES
                        (@PostAuthorId, @CollaborationSnapshotId, @Title, @Description,
                         @MediaUrl, @ExternalLink, @Status, @CreatedAt, @CreatedBy, @IsDeleted)
                    RETURNING PostId;";
 
        entity.PostId = await _connection.ExecuteScalarAsync<int>(sql, entity, transaction: _transaction);
    }
 
    public async Task UpdateAsync(Post entity, CancellationToken ct = default)
    {
        var sql = @"UPDATE Posts
                    SET Title                  = @Title,
                        Description            = @Description,
                        MediaUrl               = @MediaUrl,
                        ExternalLink           = @ExternalLink,
                        Status                 = @Status,
                        UpdatedAt              = @UpdatedAt,
                        UpdatedBy              = @UpdatedBy
                    WHERE PostId = @PostId AND IsDeleted = false;";
 
        await _connection.ExecuteAsync(sql, entity, transaction: _transaction);
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
 
        var result = await _connection.QueryAsync<Post, PostAuthor, CollaborationSnapshot, Post>(
            sql,
            (post, author, collab) =>
            {
                post.Author        = author;
                post.Collaboration = collab;
                return post;
            },
            new { Id = id },
            transaction: _transaction,
            splitOn: "PostAuthorId,CollaborationSnapshotId");
 
        return result.FirstOrDefault();
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