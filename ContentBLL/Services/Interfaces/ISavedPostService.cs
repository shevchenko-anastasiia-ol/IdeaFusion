using ContentBLL.DTO.SavedPost;

namespace ContentBLL.Services.Interfaces;

public interface ISavedPostService
{
    Task<SavedPostDto> SaveAsync(SavedPostCreateDto dto, CancellationToken ct = default);

    /// <summary>Removes the caller's own saved post. The user is taken from the token.</summary>
    Task UnsaveAsync(int postId, CancellationToken ct = default);

    /// <summary>Whether the caller has saved the post.</summary>
    Task<bool> ExistsAsync(int postId, CancellationToken ct = default);

    /// <summary>The caller's own saved posts.</summary>
    Task<IEnumerable<SavedPostDto>> GetMineAsync(CancellationToken ct = default);

    Task<int> CountByPostAsync(int postId, CancellationToken ct = default);
}
