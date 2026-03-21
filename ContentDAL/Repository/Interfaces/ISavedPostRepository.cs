using ContentDomain.Entity;

namespace ContentDAL.Repository.Interfaces;

public interface  ISavedPostRepository
{
    Task AddAsync(SavedPost entity, CancellationToken ct = default);
    Task DeleteAsync(int postId, int userId, CancellationToken ct = default);
 
    Task<bool> ExistsAsync(int postId, int userId, CancellationToken ct = default);
    Task<IEnumerable<SavedPost>> GetByUserIdAsync(int userId, CancellationToken ct = default);
}