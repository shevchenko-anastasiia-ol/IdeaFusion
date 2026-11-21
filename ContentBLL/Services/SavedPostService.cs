using AutoMapper;
using ContentBLL.DTO.SavedPost;
using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using ContentDomain.Exception;

namespace ContentBLL.Services;

public class SavedPostService : ISavedPostService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IPostAuthorResolver _authorResolver;

    public SavedPostService(IUnitOfWork uow, IMapper mapper, IPostAuthorResolver authorResolver)
    {
        _uow = uow;
        _mapper = mapper;
        _authorResolver = authorResolver;
    }

    public async Task<SavedPostDto> SaveAsync(SavedPostCreateDto dto, CancellationToken ct = default)
    {
        var post = await _uow.PostRepository.GetByIdAsync(dto.PostId, ct)
                   ?? throw new NotFoundException($"Пост з id={dto.PostId} не знайдено.");

        // A saved post belongs to the caller, always.
        var caller = await _authorResolver.ResolveForWriteAsync(ct: ct);

        var alreadySaved = await _uow.SavedPostRepository.ExistsAsync(dto.PostId, caller.UserId, ct);
        if (alreadySaved)
            throw new BusinessConflictException("Користувач вже зберіг цей пост.");

        var savedPost = _mapper.Map<SavedPost>(dto);
        savedPost.UserId = caller.UserId;
        savedPost.SavedAt = DateTime.UtcNow;

        await _uow.SavedPostRepository.AddAsync(savedPost, ct);

        // підтягуємо пост для маппінгу SavedPostDto
        savedPost.Post = post;

        return _mapper.Map<SavedPostDto>(savedPost);
    }

    public async Task UnsaveAsync(int postId, CancellationToken ct = default)
    {
        var caller = await _authorResolver.ResolveAsync(ct);

        var exists = await _uow.SavedPostRepository.ExistsAsync(postId, caller.UserId, ct);
        if (!exists)
            throw new NotFoundException($"Збережений пост на postId={postId} не знайдено.");

        await _uow.SavedPostRepository.DeleteAsync(postId, caller.UserId, ct);
    }

    public async Task<bool> ExistsAsync(int postId, CancellationToken ct = default)
    {
        // No author row means the caller has never saved anything, which is an
        // answer rather than a 404.
        var caller = await _authorResolver.FindAsync(ct);
        if (caller is null)
        {
            return false;
        }

        return await _uow.SavedPostRepository.ExistsAsync(postId, caller.UserId, ct);
    }

    /// <summary>The caller's own saved posts. There is no by-user read any more.</summary>
    public async Task<IEnumerable<SavedPostDto>> GetMineAsync(CancellationToken ct = default)
    {
        // A caller who has never saved anything gets an empty list, not a 404.
        var caller = await _authorResolver.FindAsync(ct);
        if (caller is null)
        {
            return Array.Empty<SavedPostDto>();
        }

        var saved = await _uow.SavedPostRepository.GetByUserIdAsync(caller.UserId, ct);
        var result = new List<SavedPostDto>();
        foreach (var s in saved)
        {
            var dto = new SavedPostDto
            {
                SavedPostId = s.SavedPostId,
                UserId = s.UserId,
                SavedAt = s.SavedAt,
                PostId = s.PostId,
                PostTitle = s.Post?.Title ?? string.Empty,
            };
            var mediaList = await _uow.PostRepository.GetMediaByPostIdAsync(s.PostId, ct);
            foreach (var media in mediaList)
                dto.PostMediaUrls.Add(await _uow.PostRepository.GetMediaUrlAsync(media, ct));
            result.Add(dto);
        }
        return result;
    }

    public Task<int> CountByPostAsync(int postId, CancellationToken ct = default)
        => _uow.SavedPostRepository.CountByPostAsync(postId, ct);
}
