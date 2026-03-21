using ContentBLL.DTO.Like;

namespace ContentBLL.Services.Interfaces;

public interface ILikeService
{
    Task<LikeDto> AddAsync(LikeCreateDto dto, CancellationToken ct = default);
    Task RemoveAsync(int postId, int userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int postId, int userId, CancellationToken ct = default);
    Task<int> CountByPostAsync(int postId, CancellationToken ct = default);
}