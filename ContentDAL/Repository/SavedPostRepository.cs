using ContentDAL.Repository.Interfaces;
using System.Data;
using ContentDomain.Entity;
using Dapper;

namespace ContentDAL.Repository;

public class SavedPostRepository : ISavedPostRepository
{
    private readonly IDbConnection _connection;
    protected readonly IDbTransaction? _transaction;
 
    public SavedPostRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }
 
    public async Task AddAsync(SavedPost entity, CancellationToken ct = default)
    {
        var sql = @"INSERT INTO SavedPosts (PostId, UserId, SavedAt)
                    VALUES (@PostId, @UserId, @SavedAt)
                    RETURNING SavedPostId;";
 
        entity.SavedPostId = await _connection.ExecuteScalarAsync<int>(sql, entity, transaction: _transaction);
    }
 
    public async Task DeleteAsync(int postId, int userId, CancellationToken ct = default)
    {
        var sql = @"DELETE FROM SavedPosts
                    WHERE PostId = @PostId AND UserId = @UserId;";
 
        await _connection.ExecuteAsync(sql, new { PostId = postId, UserId = userId }, transaction: _transaction);
    }
 
    public async Task<bool> ExistsAsync(int postId, int userId, CancellationToken ct = default)
    {
        var sql = @"SELECT COUNT(1) FROM SavedPosts
                    WHERE PostId = @PostId AND UserId = @UserId;";
 
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { PostId = postId, UserId = userId }, transaction: _transaction);
        return count > 0;
    }
 
    public async Task<IEnumerable<SavedPost>> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        var sql = @"SELECT sp.*, p.*
                    FROM SavedPosts sp
                    INNER JOIN Posts p ON sp.PostId = p.PostId
                    WHERE sp.UserId = @UserId AND p.IsDeleted = false
                    ORDER BY sp.SavedAt DESC;";
 
        return await _connection.QueryAsync<SavedPost, Post, SavedPost>(
            sql,
            (saved, post) =>
            {
                saved.Post = post;
                return saved;
            },
            new { UserId = userId },
            transaction: _transaction,
            splitOn: "PostId");
    }
}