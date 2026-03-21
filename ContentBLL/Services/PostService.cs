using AutoMapper;
using ContentBLL.DTO.Post;
using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;

namespace ContentBLL.Services;

public class PostService : IPostService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
 
    public PostService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }
 
    public async Task<PostDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var post = await _uow.PostRepository.GetByIdAsync(id, ct);
        if (post is null) return null;
 
        var tags = await _uow.TagRepository.GetByPostIdAsync(id, ct);
        var dto = _mapper.Map<PostDto>(post);
        dto.Tags = tags.Select(t => t.Name).ToList();
 
        return dto;
    }
 
    public async Task<IEnumerable<PostDto>> GetAllAsync(CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.GetAllAsync(ct);
        return await MapPostsWithTagsAsync(posts, ct);
    }
 
    public async Task<IEnumerable<PostDto>> GetByAuthorAsync(int postAuthorId, CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.GetByAuthorAsync(postAuthorId, ct);
        return await MapPostsWithTagsAsync(posts, ct);
    }
 
    public async Task<IEnumerable<PostDto>> GetByCollaborationAsync(int collaborationSnapshotId, CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.GetByCollaborationAsync(collaborationSnapshotId, ct);
        return await MapPostsWithTagsAsync(posts, ct);
    }
 
    public async Task<IEnumerable<PostDto>> GetByTagAsync(int tagId, CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.GetByTagAsync(tagId, ct);
        return await MapPostsWithTagsAsync(posts, ct);
    }
 
    public async Task<PostDto> CreateAsync(PostCreateDto dto, CancellationToken ct = default)
    {
        var post = _mapper.Map<Post>(dto);
        post.CreatedAt = DateTime.UtcNow;
 
        await _uow.BeginTransactionAsync(ct);
        try
        {
            await _uow.PostRepository.AddAsync(post, ct);
 
            foreach (var tagId in dto.TagIds.Distinct())
                await _uow.TagRepository.GetByIdAsync(tagId, ct); // validate tag exists
 
            // PostTags вставляються через окремий SQL якщо потрібно,
            // або через пряму вставку в PostTags таблицю
            await InsertPostTagsAsync(post.PostId, dto.TagIds, ct);
 
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
 
        return (await GetByIdAsync(post.PostId, ct))!;
    }
 
    public async Task<PostDto> UpdateAsync(int id, PostUpdateDto dto, CancellationToken ct = default)
    {
        var existing = await _uow.PostRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Пост з id={id} не знайдено.");
 
        _mapper.Map(dto, existing);
        existing.UpdatedAt = DateTime.UtcNow;
 
        await _uow.BeginTransactionAsync(ct);
        try
        {
            await _uow.PostRepository.UpdateAsync(existing, ct);
            await DeletePostTagsAsync(id, ct);
            await InsertPostTagsAsync(id, dto.TagIds, ct);
 
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
 
        return (await GetByIdAsync(id, ct))!;
    }
 
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _uow.PostRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Пост з id={id} не знайдено.");
 
        await _uow.PostRepository.DeleteAsync(existing.PostId, ct);
    }
 
    public async Task ArchiveAsync(int id, CancellationToken ct = default)
    {
        var existing = await _uow.PostRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Пост з id={id} не знайдено.");
 
        await _uow.PostRepository.ArchiveAsync(existing.PostId, ct);
    }
 
    // ── helpers ──────────────────────────────────────────────────────────────
 
    private async Task<IEnumerable<PostDto>> MapPostsWithTagsAsync(IEnumerable<Post> posts, CancellationToken ct)
    {
        var result = new List<PostDto>();
        foreach (var post in posts)
        {
            var tags = await _uow.TagRepository.GetByPostIdAsync(post.PostId, ct);
            var dto = _mapper.Map<PostDto>(post);
            dto.Tags = tags.Select(t => t.Name).ToList();
            result.Add(dto);
        }
        return result;
    }
 
    private async Task InsertPostTagsAsync(int postId, IEnumerable<int> tagIds, CancellationToken ct)
    {
        foreach (var tagId in tagIds.Distinct())
        {
            var postTag = new PostTag { PostId = postId, TagId = tagId };
            // використовуємо окремий метод репозиторію через UoW connection
            await _uow.TagRepository.AddPostTagAsync(postId, tagId, ct);
        }
    }
 
    private async Task DeletePostTagsAsync(int postId, CancellationToken ct)
    {
        await _uow.TagRepository.DeletePostTagsAsync(postId, ct);
    }
}
 