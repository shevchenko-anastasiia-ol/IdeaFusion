using ContentDAL.Repository.Interfaces;
using System.Data;
using ContentDomain.Entity;
using Dapper;


namespace ContentDAL.Repository;

public class PostViewRepository : IPostViewRepository
{
    private readonly IDbConnection _connection;
    protected readonly IDbTransaction? _transaction;
 
    public PostViewRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }
 
    public async Task AddAsync(PostView entity, CancellationToken ct = default)
    {
        var sql = @"INSERT INTO PostViews (PostId, UserId, ViewedAt)
                    VALUES (@PostId, @UserId, @ViewedAt)
                    RETURNING PostViewId;";
 
        entity.PostViewId = await _connection.ExecuteScalarAsync<int>(sql, entity, transaction: _transaction);
    }
 
    public async Task<int> CountByPostAsync(int postId, CancellationToken ct = default)
    {
        var sql = "SELECT COUNT(1) FROM PostViews WHERE PostId = @PostId;";
        return await _connection.ExecuteScalarAsync<int>(sql, new { PostId = postId }, transaction: _transaction);
    }
}