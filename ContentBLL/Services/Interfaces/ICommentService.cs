using ContentBLL.DTO.Comment;

namespace ContentBLL.Services.Interfaces;

public interface ICommentService
{
    Task<CommentDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<CommentDto>> GetByPostIdAsync(int postId, CancellationToken ct = default);
    Task<IEnumerable<CommentDto>> GetRepliesAsync(int parentCommentId, CancellationToken ct = default);
 
    Task<CommentDto> CreateAsync(CommentCreateDto dto, CancellationToken ct = default);
    Task<CommentDto> UpdateAsync(int id, CommentUpdateDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}