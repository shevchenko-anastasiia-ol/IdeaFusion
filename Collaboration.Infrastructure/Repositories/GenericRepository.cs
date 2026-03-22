using System.Linq.Expressions;
using System.Reflection;
using Collaboration.Domain.Common;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly IMongoCollection<TEntity> _collection;
    private readonly PropertyInfo? _versionProperty;
 
    public GenericRepository(IMongoDatabase database, string? collectionName = null)
    {
        if (database is null) throw new ArgumentNullException(nameof(database));
 
        var name = collectionName ?? typeof(TEntity).Name.ToLowerInvariant() + "s";
        _collection = database.GetCollection<TEntity>(name);
 
        _versionProperty = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => string.Equals(p.Name, "Version", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(p.Name, "RowVersion", StringComparison.OrdinalIgnoreCase));
    }
 
    private FilterDefinition<TEntity> BuildFilter(Expression<Func<TEntity, bool>>? filter) =>
        filter is null ? Builders<TEntity>.Filter.Empty : Builders<TEntity>.Filter.Where(filter);
 
    public async Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return await _collection.Find(Builders<TEntity>.Filter.Eq("Id", id)).FirstOrDefaultAsync(cancellationToken);
    }
 
    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _collection.Find(Builders<TEntity>.Filter.Empty).ToListAsync(cancellationToken);
 
    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }
 
    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default) =>
        await _collection.Find(BuildFilter(filter)).ToListAsync(cancellationToken);
 
    public async Task<TEntity?> FindOneAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default) =>
        await _collection.Find(BuildFilter(filter)).FirstOrDefaultAsync(cancellationToken);
 
    public async Task<long> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default) =>
        await _collection.CountDocumentsAsync(BuildFilter(filter), cancellationToken: cancellationToken);
 
    public async Task<(IEnumerable<TEntity> Items, long TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var f = BuildFilter(filter);
        var total = await _collection.CountDocumentsAsync(f, cancellationToken: cancellationToken);
 
        var find = _collection.Find(f);
 
        find = orderBy is not null
            ? find.Sort(ascending ? Builders<TEntity>.Sort.Ascending(orderBy) : Builders<TEntity>.Sort.Descending(orderBy))
            : find.Sort(ascending ? Builders<TEntity>.Sort.Ascending("_id") : Builders<TEntity>.Sort.Descending("_id"));
 
        var items = await find.Skip(Math.Max(0, (pageNumber - 1)) * Math.Max(1, pageSize)).Limit(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
 
    public async Task<IEnumerable<TEntity>> CreateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var list = (entities ?? Array.Empty<TEntity>()).ToList();
        if (!list.Any()) return list;
        await _collection.InsertManyAsync(list, cancellationToken: cancellationToken);
        return list;
    }
 
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        var result = await _collection.DeleteOneAsync(Builders<TEntity>.Filter.Eq("Id", id), cancellationToken);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
 
    public async Task<bool> DeleteManyAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteManyAsync(BuildFilter(filter), cancellationToken);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
 
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        var count = await _collection.CountDocumentsAsync(Builders<TEntity>.Filter.Eq("Id", id), cancellationToken: cancellationToken);
        return count > 0;
    }
 
    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Entity Id is required for update", nameof(entity));
 
        var result = await _collection.ReplaceOneAsync(
            Builders<TEntity>.Filter.Eq("Id", entity.Id),
            entity,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
 
        if (!result.IsAcknowledged || result.ModifiedCount == 0)
            throw new InvalidOperationException($"Failed to update document with Id = {entity.Id}");
 
        return entity;
    }
 
    public async Task<TEntity> UpdateWithConcurrencyCheckAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (_versionProperty is null) return await UpdateAsync(entity, cancellationToken);
 
        var currentVersionObj = _versionProperty.GetValue(entity)
            ?? throw new InvalidOperationException("Version property is null on entity.");
 
        if (!int.TryParse(currentVersionObj.ToString(), out var currentVersion))
            throw new InvalidOperationException("Version property is not an int.");
 
        var filter = Builders<TEntity>.Filter.And(
            Builders<TEntity>.Filter.Eq("Id", entity.Id),
            Builders<TEntity>.Filter.Eq(_versionProperty.Name, currentVersion));
 
        _versionProperty.SetValue(entity, currentVersion + 1);
 
        var result = await _collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = false }, cancellationToken);
 
        if (!result.IsAcknowledged || result.ModifiedCount == 0)
        {
            _versionProperty.SetValue(entity, currentVersion);
            throw new InvalidOperationException("Concurrency conflict: entity version mismatch.");
        }
 
        return entity;
    }
 
    public async Task<(bool Success, TEntity? Entity)> TryUpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_versionProperty is null)
            {
                await UpdateAsync(entity, cancellationToken);
                return (true, entity);
            }
 
            var currentVersionObj = _versionProperty.GetValue(entity);
            if (currentVersionObj is null || !int.TryParse(currentVersionObj.ToString(), out var currentVersion))
                return (false, null);
 
            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Eq("Id", entity.Id),
                Builders<TEntity>.Filter.Eq(_versionProperty.Name, currentVersion));
 
            _versionProperty.SetValue(entity, currentVersion + 1);
 
            var result = await _collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = false }, cancellationToken);
            if (!result.IsAcknowledged || result.ModifiedCount == 0)
            {
                _versionProperty.SetValue(entity, currentVersion);
                return (false, null);
            }
 
            return (true, entity);
        }
        catch
        {
            return (false, null);
        }
    }
}