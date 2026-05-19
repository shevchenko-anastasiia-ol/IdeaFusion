using ContentBLL.DTO.Post;

namespace ContentBLL.Services.Interfaces;

public interface IPostService
{
    Task<PostDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<PostDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<PostDto>> GetByAuthorAsync(int postAuthorId, CancellationToken ct = default);
    Task<IEnumerable<PostDto>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<IEnumerable<PostDto>> GetByCollaborationAsync(int collaborationSnapshotId, CancellationToken ct = default);
    Task<IEnumerable<PostDto>> GetByTagAsync(int tagId, CancellationToken ct = default);
 
    Task<PostDto> CreateAsync(PostCreateDto dto, CancellationToken ct = default);
    Task<PostDto> UpdateAsync(int id, PostUpdateDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task ArchiveAsync(int id, CancellationToken ct = default);
    Task<int> EnsurePostAuthorAsync(string userName, string? avatarUrl = null, CancellationToken ct = default);
}