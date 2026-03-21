using ContentBLL.DTO.Tag;

namespace ContentBLL.Services.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagDto>> GetAllAsync(CancellationToken ct = default);
    Task<TagDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<TagDto>> GetByPostIdAsync(int postId, CancellationToken ct = default);
    Task<TagDto> CreateAsync(TagCreateDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}