using ContentDomain.Entity;

namespace ContentDAL.Repository.Interfaces;

public interface  ICommentRepository
{
    Task AddAsync(Comment entity, CancellationToken ct = default);
    Task UpdateAsync(Comment entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
 
    Task<Comment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Comment>> GetByPostIdAsync(int postId, CancellationToken ct = default);
    Task<IEnumerable<Comment>> GetRepliesAsync(int parentCommentId, CancellationToken ct = default);
}