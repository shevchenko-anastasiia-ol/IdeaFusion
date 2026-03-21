using ContentDomain.Entity;

namespace ContentDAL.Repository.Interfaces;

public interface  ITagRepository
{
    Task AddAsync(Tag entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
 
    Task<Tag?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Tag?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<Tag>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Tag>> GetByPostIdAsync(int postId, CancellationToken ct = default);
}