using AutoMapper;
using ContentBLL.DTO.Comment;
using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using ContentDomain.Exception;
using ServiceDefaults.Interfaces;

namespace ContentBLL.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IPostAuthorResolver _authorResolver;
    private readonly ICurrentUserAccessor _currentUser;

    public CommentService(
        IUnitOfWork uow,
        IMapper mapper,
        IPostAuthorResolver authorResolver,
        ICurrentUserAccessor currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _authorResolver = authorResolver;
        _currentUser = currentUser;
    }

    public async Task<CommentDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var comment = await _uow.CommentRepository.GetByIdAsync(id, ct);
        return comment is null ? null : _mapper.Map<CommentDto>(comment);
    }

    public async Task<IEnumerable<CommentDto>> GetByPostIdAsync(int postId, CancellationToken ct = default)
    {
        var comments = await _uow.CommentRepository.GetByPostIdAsync(postId, ct);
        return _mapper.Map<IEnumerable<CommentDto>>(comments);
    }

    public async Task<IEnumerable<CommentDto>> GetRepliesAsync(int parentCommentId, CancellationToken ct = default)
    {
        var replies = await _uow.CommentRepository.GetRepliesAsync(parentCommentId, ct);
        return _mapper.Map<IEnumerable<CommentDto>>(replies);
    }

    public async Task<CommentDto> CreateAsync(CommentCreateDto dto, CancellationToken ct = default)
    {
        // перевіряємо що пост існує
        var post = await _uow.PostRepository.GetByIdAsync(dto.PostId, ct)
            ?? throw new NotFoundException($"Пост з id={dto.PostId} не знайдено.");

        // якщо це reply — перевіряємо що батьківський коментар існує і належить тому ж посту
        if (dto.ParentCommentId.HasValue)
        {
            var parent = await _uow.CommentRepository.GetByIdAsync(dto.ParentCommentId.Value, ct)
                ?? throw new NotFoundException($"Батьківський коментар з id={dto.ParentCommentId} не знайдено.");

            if (parent.PostId != dto.PostId)
                throw new BusinessConflictException("Батьківський коментар належить іншому посту.");
        }

        // The author comes from the token, so a caller cannot post as somebody else.
        var caller = await _authorResolver.ResolveForWriteAsync(ct: ct);

        var comment = _mapper.Map<Comment>(dto);
        comment.PostAuthorId = caller.PostAuthorId;
        comment.CreatedAt = DateTime.UtcNow;
        comment.CreatedBy = caller.UserName;

        await _uow.CommentRepository.AddAsync(comment, ct);

        return (await GetByIdAsync(comment.CommentId, ct))!;
    }

    public async Task<CommentDto> UpdateAsync(int id, CommentUpdateDto dto, CancellationToken ct = default)
    {
        var existing = await _uow.CommentRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Коментар з id={id} не знайдено.");

        var caller = await _authorResolver.ResolveForWriteAsync(ct: ct);
        OwnershipGuard.EnsureCommentOwnerOrModerator(existing, caller, _currentUser);

        _mapper.Map(dto, existing);
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = caller.UserName;

        await _uow.CommentRepository.UpdateAsync(existing, ct);

        return (await GetByIdAsync(id, ct))!;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _uow.CommentRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Коментар з id={id} не знайдено.");

        var caller = await _authorResolver.ResolveForWriteAsync(ct: ct);
        OwnershipGuard.EnsureCommentOwnerOrModerator(existing, caller, _currentUser);

        await _uow.CommentRepository.DeleteAsync(existing.CommentId, ct);
    }

    public async Task ModerateDeleteAsync(int id, CancellationToken ct = default)
    {
        OwnershipGuard.EnsureModerator(_currentUser, "comment.moderate.delete");

        var existing = await _uow.CommentRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Коментар з id={id} не знайдено.");

        await _uow.CommentRepository.DeleteAsync(existing.CommentId, ct);
    }
}
