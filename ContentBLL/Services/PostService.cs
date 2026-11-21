using AutoMapper;
using ContentBLL.DTO.Post;
using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;
using ContentDomain.Exception;
using ServiceDefaults.Interfaces;

namespace ContentBLL.Services;

public class PostService : IPostService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IPostAuthorResolver _authorResolver;
    private readonly ICurrentUserAccessor _currentUser;

    public PostService(
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

    public async Task<PostDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var post = await _uow.PostRepository.GetByIdAsync(id, ct);
        if (post is null) return null;

        var tags = await _uow.TagRepository.GetByPostIdAsync(id, ct);
        var dto = _mapper.Map<PostDto>(post);
        dto.Tags = tags.Select(t => t.Name).ToList();

        var mediaUrls = new List<string>();
        foreach (var m in post.Media)
            mediaUrls.Add(await _uow.PostRepository.GetMediaUrlAsync(m));
        dto.MediaUrls = mediaUrls;
        
        return dto;
    }

    public async Task<IEnumerable<PostDto>> GetAllAsync(CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.GetAllAsync(ct);
        return await MapPostsWithTagsAndMediaAsync(posts, ct);
    }

    public async Task<IEnumerable<PostDto>> GetByAuthorAsync(int postAuthorId, CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.GetByAuthorAsync(postAuthorId, ct);
        return await MapPostsWithTagsAndMediaAsync(posts, ct);
    }

    public async Task<IEnumerable<PostDto>> GetByCollaborationAsync(int collaborationSnapshotId, CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.GetByCollaborationAsync(collaborationSnapshotId, ct);
        return await MapPostsWithTagsAndMediaAsync(posts, ct);
    }

    public async Task<IEnumerable<PostDto>> GetByTagAsync(int tagId, CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.GetByTagAsync(tagId, ct);
        return await MapPostsWithTagsAndMediaAsync(posts, ct);
    }

    public async Task<IEnumerable<PostDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.SearchAsync(query, ct);
        return await MapPostsWithTagsAndMediaAsync(posts, ct);
    }

    public async Task<PostDto> CreateAsync(PostCreateDto dto, CancellationToken ct = default)
    {
        // The author is whoever holds the token, so there is nothing to trust in the
        // body. This is also where a first-time author's PostAuthor row is created.
        var caller = await _authorResolver.ResolveForWriteAsync(dto.AuthorAvatarUrl, ct);

        var post = _mapper.Map<Post>(dto);
        post.CreatedAt = DateTime.UtcNow;
        post.CreatedBy = caller.UserName;

        // The check constraint allows exactly one owner. A collaboration post is
        // owned by its team; a personal one by the caller.
        post.PostAuthorId = dto.CollaborationSnapshotId.HasValue ? null : caller.PostAuthorId;

        await _uow.BeginTransactionAsync(ct);
        try
        {
            await _uow.PostRepository.AddAsync(post, ct);

            // Перевірка тегів
            foreach (var tagId in dto.TagIds.Distinct())
                await _uow.TagRepository.GetByIdAsync(tagId, ct);

            await InsertPostTagsAsync(post.PostId, dto.TagIds, ct);

            // Завантаження медіа через MinIO
            foreach (var mediaFile in dto.MediaFiles) // якщо DTO містить колекцію файлів
            {
                var media = await _uow.PostRepository.UploadMediaAsync(post.PostId, mediaFile, ct);
                post.Media.Add(media);
            }

            await _uow.CommitAsync(ct);
            Console.WriteLine($"Successfully created post: {post.PostId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating post: {ex.Message}");
            await _uow.RollbackAsync(ct);
            throw;
        }

        return (await GetByIdAsync(post.PostId, ct))!;
    }

    public async Task<PostDto> UpdateAsync(int id, PostUpdateDto dto, CancellationToken ct = default)
    {
        var existing = await _uow.PostRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Пост з id={id} не знайдено.");

        var caller = await _authorResolver.ResolveForWriteAsync(ct: ct);
        OwnershipGuard.EnsurePostOwnerOrModerator(existing, caller, _currentUser);

        _mapper.Map(dto, existing);
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = caller.UserName;

        await _uow.BeginTransactionAsync(ct);
        try
        {
            await _uow.PostRepository.UpdateAsync(existing, ct);

            await DeletePostTagsAsync(id, ct);
            await InsertPostTagsAsync(id, dto.TagIds, ct);

            // Оновлення медіа: видалити непотрібне, додати нове
            if (dto.NewMediaFiles != null && dto.NewMediaFiles.Count > 0)
            {
                await _uow.PostRepository.DeleteMediaAsync(id, ct);
                foreach (var mediaFile in dto.NewMediaFiles)
                {
                    var media = await _uow.PostRepository.UploadMediaAsync(id, mediaFile, ct);
                    existing.Media.Add(media);
                }
            };

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
            ?? throw new NotFoundException($"Пост з id={id} не знайдено.");

        var caller = await _authorResolver.ResolveForWriteAsync(ct: ct);
        OwnershipGuard.EnsurePostOwnerOrModerator(existing, caller, _currentUser);

        // Видалення медіа з MinIO
        await _uow.PostRepository.DeleteMediaAsync(id, ct);

        await _uow.PostRepository.DeleteAsync(existing.PostId, ct);
    }

    public async Task ArchiveAsync(int id, CancellationToken ct = default)
    {
        var existing = await _uow.PostRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Пост з id={id} не знайдено.");

        var caller = await _authorResolver.ResolveForWriteAsync(ct: ct);
        OwnershipGuard.EnsurePostOwnerOrModerator(existing, caller, _currentUser);

        await _uow.PostRepository.ArchiveAsync(existing.PostId, ct);
    }

    public async Task ModerateDeleteAsync(int id, CancellationToken ct = default)
    {
        OwnershipGuard.EnsureModerator(_currentUser, "post.moderate.delete");

        var existing = await _uow.PostRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Пост з id={id} не знайдено.");

        await _uow.PostRepository.DeleteMediaAsync(id, ct);
        await _uow.PostRepository.DeleteAsync(existing.PostId, ct);
    }

    // ── helpers ────────────────────────────────────────────────

    private async Task<IEnumerable<PostDto>> MapPostsWithTagsAndMediaAsync(IEnumerable<Post> posts, CancellationToken ct)
    {
        var result = new List<PostDto>();
        foreach (var post in posts)
        {
            var tags = await _uow.TagRepository.GetByPostIdAsync(post.PostId, ct);
            var dto = _mapper.Map<PostDto>(post);
            dto.Tags = tags.Select(t => t.Name).ToList();
            var mediaList = await _uow.PostRepository.GetMediaByPostIdAsync(post.PostId, ct);
            dto.MediaUrls = new List<string>();
            foreach (var media in mediaList)
                dto.MediaUrls.Add(await _uow.PostRepository.GetMediaUrlAsync(media, ct));
            result.Add(dto);
        }
        return result;
    }

    private async Task InsertPostTagsAsync(int postId, IEnumerable<int> tagIds, CancellationToken ct)
    {
        foreach (var tagId in tagIds.Distinct())
            await _uow.TagRepository.AddPostTagAsync(postId, tagId, ct);
    }

    public async Task<IEnumerable<PostDto>> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        var posts = await _uow.PostRepository.GetByUserIdAsync(userId, ct);
        return await MapPostsWithTagsAndMediaAsync(posts, ct);
    }

    private async Task DeletePostTagsAsync(int postId, CancellationToken ct)
    {
        await _uow.TagRepository.DeletePostTagsAsync(postId, ct);
    }
}
