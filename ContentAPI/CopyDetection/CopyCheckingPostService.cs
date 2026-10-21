using ContentBLL.DTO.Post;
using ContentBLL.Services.Interfaces;

namespace ContentAPI.CopyDetection;

/// <summary>
/// Decorates <see cref="IPostService"/>: publishing a post queues its images for the copy check
/// and indexing; deleting or archiving removes them from the index. Reads pass straight through.
/// The post itself is never delayed or rejected here (requirement B5: no automatic sanctions).
/// </summary>
public sealed class CopyCheckingPostService : IPostService
{
    private readonly IPostService _inner;
    private readonly CopyCheckQueue _queue;
    private readonly ILogger<CopyCheckingPostService> _logger;

    public CopyCheckingPostService(IPostService inner, CopyCheckQueue queue, ILogger<CopyCheckingPostService> logger)
    {
        _inner = inner;
        _queue = queue;
        _logger = logger;
    }

    public Task<PostDto?> GetByIdAsync(int id, CancellationToken ct = default) => _inner.GetByIdAsync(id, ct);
    public Task<IEnumerable<PostDto>> GetAllAsync(CancellationToken ct = default) => _inner.GetAllAsync(ct);
    public Task<IEnumerable<PostDto>> GetByAuthorAsync(int postAuthorId, CancellationToken ct = default) => _inner.GetByAuthorAsync(postAuthorId, ct);
    public Task<IEnumerable<PostDto>> GetByUserIdAsync(int userId, CancellationToken ct = default) => _inner.GetByUserIdAsync(userId, ct);
    public Task<IEnumerable<PostDto>> GetByCollaborationAsync(int collaborationSnapshotId, CancellationToken ct = default) => _inner.GetByCollaborationAsync(collaborationSnapshotId, ct);
    public Task<IEnumerable<PostDto>> GetByTagAsync(int tagId, CancellationToken ct = default) => _inner.GetByTagAsync(tagId, ct);
    public Task<IEnumerable<PostDto>> SearchAsync(string query, CancellationToken ct = default) => _inner.SearchAsync(query, ct);

    public async Task<PostDto> CreateAsync(PostCreateDto dto, CancellationToken ct = default)
    {
        var post = await _inner.CreateAsync(dto, ct);
        if (dto.MediaFiles.Count > 0)
            Enqueue(new CopyJob(CopyJobKind.IndexPost, post.PostId));
        return post;
    }

    public async Task<PostDto> UpdateAsync(int id, PostUpdateDto dto, CancellationToken ct = default)
    {
        var post = await _inner.UpdateAsync(id, dto, ct);
        if (dto.NewMediaFiles is { Count: > 0 })
        {
            // New media replace the old ones: drop the old fingerprints, then check the new images.
            Enqueue(new CopyJob(CopyJobKind.RemovePost, id));
            Enqueue(new CopyJob(CopyJobKind.IndexPost, id));
        }
        return post;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _inner.DeleteAsync(id, ct);
        Enqueue(new CopyJob(CopyJobKind.RemovePost, id));
    }

    public async Task ArchiveAsync(int id, CancellationToken ct = default)
    {
        await _inner.ArchiveAsync(id, ct);
        Enqueue(new CopyJob(CopyJobKind.RemovePost, id));
    }

    public async Task ModerateDeleteAsync(int id, CancellationToken ct = default)
    {
        await _inner.ModerateDeleteAsync(id, ct);
        Enqueue(new CopyJob(CopyJobKind.RemovePost, id));
    }

    private void Enqueue(CopyJob job)
    {
        if (!_queue.TryEnqueue(job))
            _logger.LogWarning("Copy check queue rejected {Kind} for post {PostId}", job.Kind, job.PostId);
    }
}
