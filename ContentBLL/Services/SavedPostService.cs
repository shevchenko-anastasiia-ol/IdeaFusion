using AutoMapper;
using ContentBLL.DTO.SavedPost;
using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;

namespace ContentBLL.Services;

public class SavedPostService : ISavedPostService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
 
    public SavedPostService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }
 
    public async Task<SavedPostDto> SaveAsync(SavedPostCreateDto dto, CancellationToken ct = default)
    {
        var post = await _uow.PostRepository.GetByIdAsync(dto.PostId, ct)
                   ?? throw new KeyNotFoundException($"Пост з id={dto.PostId} не знайдено.");
 
        var alreadySaved = await _uow.SavedPostRepository.ExistsAsync(dto.PostId, dto.UserId, ct);
        if (alreadySaved)
            throw new InvalidOperationException("Користувач вже зберіг цей пост.");
 
        var savedPost = _mapper.Map<SavedPost>(dto);
        savedPost.SavedAt = DateTime.UtcNow;
 
        await _uow.SavedPostRepository.AddAsync(savedPost, ct);
 
        // підтягуємо пост для маппінгу SavedPostDto
        savedPost.Post = post;
 
        return _mapper.Map<SavedPostDto>(savedPost);
    }
 
    public async Task UnsaveAsync(int postId, int userId, CancellationToken ct = default)
    {
        var exists = await _uow.SavedPostRepository.ExistsAsync(postId, userId, ct);
        if (!exists)
            throw new KeyNotFoundException($"Збережений пост від userId={userId} на постId={postId} не знайдено.");
 
        await _uow.SavedPostRepository.DeleteAsync(postId, userId, ct);
    }
 
    public async Task<bool> ExistsAsync(int postId, int userId, CancellationToken ct = default)
    {
        return await _uow.SavedPostRepository.ExistsAsync(postId, userId, ct);
    }
 
    public async Task<IEnumerable<SavedPostDto>> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        var saved = await _uow.SavedPostRepository.GetByUserIdAsync(userId, ct);
        return _mapper.Map<IEnumerable<SavedPostDto>>(saved);
    }
}