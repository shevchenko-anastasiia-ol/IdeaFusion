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
 
    public LikeService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }
 
    public async Task<LikeDto> AddAsync(LikeCreateDto dto, CancellationToken ct = default)
    {
        var post = await _uow.PostRepository.GetByIdAsync(dto.PostId, ct)
                   ?? throw new NotFoundException($"Пост з id={dto.PostId} не знайдено.");
 
        var alreadyLiked = await _uow.LikeRepository.ExistsAsync(dto.PostId, dto.UserId, ct);
        if (alreadyLiked)
            throw new BusinessConflictException("Користувач вже лайкнув цей пост.");
 
        var like = _mapper.Map<Like>(dto);
        like.CreatedAt = DateTime.UtcNow;
 
        await _uow.LikeRepository.AddAsync(like, ct);
 
        return _mapper.Map<LikeDto>(like);
    }
 
    public async Task RemoveAsync(int postId, int userId, CancellationToken ct = default)
    {
        var exists = await _uow.LikeRepository.ExistsAsync(postId, userId, ct);
        if (!exists)
            throw new NotFoundException($"Лайк від userId={userId} на постId={postId} не знайдено.");
 
        await _uow.LikeRepository.DeleteAsync(postId, userId, ct);
    }
 
    public async Task<bool> ExistsAsync(int postId, int userId, CancellationToken ct = default)
    {
        return await _uow.LikeRepository.ExistsAsync(postId, userId, ct);
    }
 
    public async Task<int> CountByPostAsync(int postId, CancellationToken ct = default)
    {
        return await _uow.LikeRepository.CountByPostAsync(postId, ct);
    }
}