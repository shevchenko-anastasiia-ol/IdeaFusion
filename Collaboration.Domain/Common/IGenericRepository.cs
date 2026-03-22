using System.Linq.Expressions;

namespace Collaboration.Domain.Common;

public interface IGenericRepository <TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    // Pagination Support
    Task<(IEnumerable<TEntity> Items, long TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    // Batch Operations
    Task<IEnumerable<TEntity>> CreateManyAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteManyAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);


    Task<TEntity> UpdateWithConcurrencyCheckAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
    
    Task<(bool Success, TEntity? Entity)> TryUpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}