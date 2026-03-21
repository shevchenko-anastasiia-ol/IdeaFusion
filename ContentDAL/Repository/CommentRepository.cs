using System.Data;
using ContentDAL.Repository.Interfaces;
using ContentDomain.Entity;
using Dapper;

namespace ContentDAL.Repository;

public class CommentRepository : ICommentRepository
{
    private readonly IDbConnection _connection;
    protected readonly IDbTransaction? _transaction;
 
    public CommentRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }
 
    public async Task AddAsync(Comment entity, CancellationToken ct = default)
    {
        var sql = @"INSERT INTO Comments
                        (PostId, PostAuthorId, ParentCommentId, Body, CreatedAt, CreatedBy, IsDeleted)
                    VALUES
                        (@PostId, @PostAuthorId, @ParentCommentId, @Body, @CreatedAt, @CreatedBy, @IsDeleted)
                    RETURNING CommentId;";
 
        entity.CommentId = await _connection.ExecuteScalarAsync<int>(sql, entity, transaction: _transaction);
    }
 
    public async Task UpdateAsync(Comment entity, CancellationToken ct = default)
    {
        var sql = @"UPDATE Comments
                    SET Body      = @Body,
                        UpdatedAt = @UpdatedAt,
                        UpdatedBy = @UpdatedBy
                    WHERE CommentId = @CommentId AND IsDeleted = false;";
 
        await _connection.ExecuteAsync(sql, entity, transaction: _transaction);
    }
 
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var sql = @"UPDATE Comments
                    SET IsDeleted = true, UpdatedAt = @UpdatedAt
                    WHERE CommentId = @Id AND IsDeleted = false;";
 
        await _connection.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }, transaction: _transaction);
    }
 
    public async Task<Comment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var sql = @"SELECT c.*, a.*
                    FROM Comments c
                    INNER JOIN PostAuthors a ON c.PostAuthorId = a.PostAuthorId
                    WHERE c.CommentId = @Id AND c.IsDeleted = false;";
 
        var result = await _connection.QueryAsync<Comment, PostAuthor, Comment>(
            sql,
            (comment, author) =>
            {
                comment.Author = author;
                return comment;
            },
            new { Id = id },
            transaction: _transaction,
            splitOn: "PostAuthorId");
 
        return result.FirstOrDefault();
    }
 
    public async Task<IEnumerable<Comment>> GetByPostIdAsync(int postId, CancellationToken ct = default)
    {
        var sql = @"SELECT c.*, a.*
                    FROM Comments c
                    INNER JOIN PostAuthors a ON c.PostAuthorId = a.PostAuthorId
                    WHERE c.PostId = @PostId AND c.ParentCommentId IS NULL AND c.IsDeleted = false
                    ORDER BY c.CreatedAt ASC;";
 
        return await _connection.QueryAsync<Comment, PostAuthor, Comment>(
            sql,
            (comment, author) =>
            {
                comment.Author = author;
                return comment;
            },
            new { PostId = postId },
            transaction: _transaction,
            splitOn: "PostAuthorId");
    }
 
    public async Task<IEnumerable<Comment>> GetRepliesAsync(int parentCommentId, CancellationToken ct = default)
    {
        var sql = @"SELECT c.*, a.*
                    FROM Comments c
                    INNER JOIN PostAuthors a ON c.PostAuthorId = a.PostAuthorId
                    WHERE c.ParentCommentId = @ParentCommentId AND c.IsDeleted = false
                    ORDER BY c.CreatedAt ASC;";
 
        return await _connection.QueryAsync<Comment, PostAuthor, Comment>(
            sql,
            (comment, author) =>
            {
                comment.Author = author;
                return comment;
            },
            new { ParentCommentId = parentCommentId },
            transaction: _transaction,
            splitOn: "PostAuthorId");
    }
}