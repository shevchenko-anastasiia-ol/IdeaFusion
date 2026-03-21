using ContentDomain.Entity;

namespace ContentDAL.Repository.Interfaces;

public interface  ILikeRepository
{
    Task AddAsync(Like entity, CancellationToken ct = default);
    Task DeleteAsync(int postId, int userId, CancellationToken ct = default);
 
    Task<bool> ExistsAsync(int postId, int userId, CancellationToken ct = default);
    Task<int> CountByPostAsync(int postId, CancellationToken ct = default);
    Task<IEnumerable<Like>> GetByPostIdAsync(int postId, CancellationToken ct = default);
}