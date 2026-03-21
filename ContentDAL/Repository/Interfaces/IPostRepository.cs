using ContentDomain.Entity;

namespace ContentDAL.Repository.Interfaces;

public interface IPostRepository
{
    Task AddAsync(Post entity, CancellationToken ct = default);
    Task UpdateAsync(Post entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task ArchiveAsync(int id, CancellationToken ct = default);
 
    Task<Post?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Post>> GetByAuthorAsync(int postAuthorId, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetByCollaborationAsync(int collaborationSnapshotId, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetByStatusAsync(PostStatus status, CancellationToken ct = default);
    Task<IEnumerable<Post>> GetByTagAsync(int tagId, CancellationToken ct = default);
}