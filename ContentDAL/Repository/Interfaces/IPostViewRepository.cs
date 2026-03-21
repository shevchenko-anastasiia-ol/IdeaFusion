using ContentDomain.Entity;

namespace ContentDAL.Repository.Interfaces;

public interface  IPostViewRepository
{
    Task AddAsync(PostView entity, CancellationToken ct = default);
 
    Task<int> CountByPostAsync(int postId, CancellationToken ct = default);
}