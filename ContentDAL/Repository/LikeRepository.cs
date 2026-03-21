using ContentDAL.Repository.Interfaces;
using System.Data;
using ContentDomain.Entity;
using Dapper;

namespace ContentDAL.Repository;

public class LikeRepository : ILikeRepository
{
    private readonly IDbConnection _connection;
    protected readonly IDbTransaction? _transaction;
 
    public LikeRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }
 
    public async Task AddAsync(Like entity, CancellationToken ct = default)
    {
        var sql = @"INSERT INTO Likes (PostId, UserId, CreatedAt)
                    VALUES (@PostId, @UserId, @CreatedAt)
                    RETURNING LikeId;";
 
        entity.LikeId = await _connection.ExecuteScalarAsync<int>(sql, entity, transaction: _transaction);
    }
 
    public async Task DeleteAsync(int postId, int userId, CancellationToken ct = default)
    {
        var sql = @"DELETE FROM Likes
                    WHERE PostId = @PostId AND UserId = @UserId;";
 
        await _connection.ExecuteAsync(sql, new { PostId = postId, UserId = userId }, transaction: _transaction);
    }
 
    public async Task<bool> ExistsAsync(int postId, int userId, CancellationToken ct = default)
    {
        var sql = @"SELECT COUNT(1) FROM Likes
                    WHERE PostId = @PostId AND UserId = @UserId;";
 
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { PostId = postId, UserId = userId }, transaction: _transaction);
        return count > 0;
    }
 
    public async Task<int> CountByPostAsync(int postId, CancellationToken ct = default)
    {
        var sql = "SELECT COUNT(1) FROM Likes WHERE PostId = @PostId;";
        return await _connection.ExecuteScalarAsync<int>(sql, new { PostId = postId }, transaction: _transaction);
    }
 
    public async Task<IEnumerable<Like>> GetByPostIdAsync(int postId, CancellationToken ct = default)
    {
        var sql = "SELECT * FROM Likes WHERE PostId = @PostId;";
        return await _connection.QueryAsync<Like>(sql, new { PostId = postId }, transaction: _transaction);
    }
}