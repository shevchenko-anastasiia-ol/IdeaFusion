using ContentBLL.DTO.SavedPost;

namespace ContentBLL.Services.Interfaces;

public interface ISavedPostService
{
    Task<SavedPostDto> SaveAsync(SavedPostCreateDto dto, CancellationToken ct = default);
    Task UnsaveAsync(int postId, int userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int postId, int userId, CancellationToken ct = default);
    Task<IEnumerable<SavedPostDto>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<int> CountByPostAsync(int postId, CancellationToken ct = default);
}