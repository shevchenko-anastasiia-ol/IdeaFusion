using AutoMapper;
using ContentBLL.DTO.Like;
using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using ContentDomain.Exception;

namespace ContentBLL.Services;

public class LikeService : ILikeService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IPostAuthorResolver _authorResolver;

    public LikeService(IUnitOfWork uow, IMapper mapper, IPostAuthorResolver authorResolver)
    {
        _uow = uow;
        _mapper = mapper;
        _authorResolver = authorResolver;
    }

    public async Task<LikeDto> AddAsync(LikeCreateDto dto, CancellationToken ct = default)
    {
        var post = await _uow.PostRepository.GetByIdAsync(dto.PostId, ct)
                   ?? throw new NotFoundException($"Пост з id={dto.PostId} не знайдено.");

        // A like is the caller's own, always. Naming somebody else's userId in the
        // body is no longer possible.
        var caller = await _authorResolver.ResolveForWriteAsync(ct: ct);

        var alreadyLiked = await _uow.LikeRepository.ExistsAsync(dto.PostId, caller.UserId, ct);
        if (alreadyLiked)
            throw new BusinessConflictException("Користувач вже лайкнув цей пост.");

        var like = _mapper.Map<Like>(dto);
        like.UserId = caller.UserId;
        like.CreatedAt = DateTime.UtcNow;

        await _uow.LikeRepository.AddAsync(like, ct);

        return _mapper.Map<LikeDto>(like);
    }

    public async Task RemoveAsync(int postId, CancellationToken ct = default)
    {
        var caller = await _authorResolver.ResolveAsync(ct);

        var exists = await _uow.LikeRepository.ExistsAsync(postId, caller.UserId, ct);
        if (!exists)
            throw new NotFoundException($"Лайк на пост з id={postId} не знайдено.");

        await _uow.LikeRepository.DeleteAsync(postId, caller.UserId, ct);
    }

    public async Task<bool> ExistsAsync(int postId, CancellationToken ct = default)
    {
        // No author row means the caller has never liked anything, which is an
        // answer rather than a 404.
        var caller = await _authorResolver.FindAsync(ct);
        if (caller is null)
        {
            return false;
        }

        return await _uow.LikeRepository.ExistsAsync(postId, caller.UserId, ct);
    }

    public async Task<int> CountByPostAsync(int postId, CancellationToken ct = default)
    {
        return await _uow.LikeRepository.CountByPostAsync(postId, ct);
    }
}
