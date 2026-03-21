using ContentDAL.Repository.Interfaces;
using System.Data;
using ContentDomain.Entity;
using Dapper;

namespace ContentDAL.Repository;

public class TagRepository : ITagRepository
{
    private readonly IDbConnection _connection;
    protected readonly IDbTransaction? _transaction;
 
    public TagRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }
 
    public async Task AddAsync(Tag entity, CancellationToken ct = default)
    {
        var sql = @"INSERT INTO Tags (Name)
                    VALUES (@Name)
                    RETURNING TagId;";
 
        entity.TagId = await _connection.ExecuteScalarAsync<int>(sql, entity, transaction: _transaction);
    }
 
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var sql = "DELETE FROM Tags WHERE TagId = @Id;";
        await _connection.ExecuteAsync(sql, new { Id = id }, transaction: _transaction);
    }
 
    public async Task<Tag?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var sql = "SELECT * FROM Tags WHERE TagId = @Id;";
        return await _connection.QuerySingleOrDefaultAsync<Tag>(sql, new { Id = id }, transaction: _transaction);
    }
 
    public async Task<Tag?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var sql = "SELECT * FROM Tags WHERE Name = @Name;";
        return await _connection.QuerySingleOrDefaultAsync<Tag>(sql, new { Name = name }, transaction: _transaction);
    }
 
    public async Task<IEnumerable<Tag>> GetAllAsync(CancellationToken ct = default)
    {
        var sql = "SELECT * FROM Tags ORDER BY Name ASC;";
        return await _connection.QueryAsync<Tag>(sql, transaction: _transaction);
    }
 
    public async Task<IEnumerable<Tag>> GetByPostIdAsync(int postId, CancellationToken ct = default)
    {
        var sql = @"SELECT t.*
                    FROM Tags t
                    INNER JOIN PostTags pt ON t.TagId = pt.TagId
                    WHERE pt.PostId = @PostId;";
 
        return await _connection.QueryAsync<Tag>(sql, new { PostId = postId }, transaction: _transaction);
    }
}