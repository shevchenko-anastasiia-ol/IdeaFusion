using ContentBLL.DTO.Like;

namespace ContentBLL.Services.Interfaces;

public interface ILikeService
{
    Task<LikeDto> AddAsync(LikeCreateDto dto, CancellationToken ct = default);

    /// <summary>Removes the caller's own like. The user is taken from the token.</summary>
    Task RemoveAsync(int postId, CancellationToken ct = default);

    /// <summary>Whether the caller has liked the post.</summary>
    Task<bool> ExistsAsync(int postId, CancellationToken ct = default);

    Task<int> CountByPostAsync(int postId, CancellationToken ct = default);
}
