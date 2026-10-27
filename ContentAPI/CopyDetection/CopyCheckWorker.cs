using ContentDAL.Data;
using ContentDAL.UOW;
using ContentDomain.Entity;
using Microsoft.EntityFrameworkCore;

namespace ContentAPI.CopyDetection;

/// <summary>
/// Processes <see cref="CopyJob"/>s: every image of a published post is checked against the
/// works already on the platform (excluding the author's and their teams' own works), a
/// non-green result goes to the moderation queue, and the image is then added to the index.
/// Failures are logged and never affect the post itself.
/// </summary>
public sealed class CopyCheckWorker : BackgroundService
{
    private readonly CopyCheckQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<CopyCheckWorker> _logger;

    public CopyCheckWorker(CopyCheckQueue queue, IServiceScopeFactory scopes, ILogger<CopyCheckWorker> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<CopyJobProcessor>();
                await processor.ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Copy check job {Kind} for post {PostId} failed", job.Kind, job.PostId);
            }
        }
    }
}

/// <summary>The work of one job, separated from the hosting loop so it can be unit-tested.</summary>
public sealed class CopyJobProcessor
{
    private readonly IUnitOfWork _uow;
    private readonly ContentDbContext _db;
    private readonly IImageCopyClient _client;
    private readonly IAuthorshipFilter _authorship;
    private readonly ILogger<CopyJobProcessor> _logger;

    public CopyJobProcessor(IUnitOfWork uow, ContentDbContext db, IImageCopyClient client, IAuthorshipFilter authorship,
        ILogger<CopyJobProcessor> logger)
    {
        _uow = uow;
        _db = db;
        _client = client;
        _authorship = authorship;
        _logger = logger;
    }

    public async Task ProcessAsync(CopyJob job, CancellationToken ct)
    {
        switch (job.Kind)
        {
            case CopyJobKind.RemovePost:
                var removed = await _client.DeletePostAsync(job.PostId, ct);
                _logger.LogInformation("Removed {Count} image(s) of post {PostId} from the copy index", removed, job.PostId);
                break;
            case CopyJobKind.ReindexAll:
                await ReindexAllAsync(ct);
                break;
            default:
                await IndexPostAsync(job.PostId, ct);
                break;
        }
    }

    /// <summary>
    /// Rebuilds the index from scratch, oldest post first: each upload is compared only with
    /// the works published before it, so it is the copy, not the original, that lands in the
    /// moderation queue (on a populated index the original would match its own later copy).
    /// One failing post does not stop the run; re-running it is safe (the ML service does not
    /// duplicate or reopen checks of the same pair).
    /// </summary>
    private async Task ReindexAllAsync(CancellationToken ct)
    {
        // Ids only: the status filter happens per post in IndexPostAsync, which reads it the
        // same way the rest of the service does.
        var posts = await _db.Posts.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.PostId)
            .Select(p => p.PostId)
            .ToListAsync(ct);
        var cleared = await _client.ClearIndexAsync(ct);
        _logger.LogInformation("Copy index rebuild: {Count} post(s), {Cleared} old index entries removed", posts.Count, cleared);

        var failed = 0;
        foreach (var postId in posts)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await IndexPostAsync(postId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _logger.LogError(ex, "Copy index rebuild: post {PostId} failed", postId);
            }
        }
        _logger.LogInformation("Copy index rebuild finished: {Done} post(s), {Failed} failed", posts.Count - failed, failed);
    }

    private async Task IndexPostAsync(int postId, CancellationToken ct)
    {
        var post = await _uow.PostRepository.GetByIdAsync(postId, ct);
        if (post is null || post.Status != PostStatus.Published)
            return;

        var owner = OwnerId(post);
        var team = TeamId(post);
        var excludeTeams = team is null ? Array.Empty<string>() : new[] { team };

        foreach (var media in post.Media.Where(m => m.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
        {
            var result = await _client.CheckObjectAsync(media.Bucket, media.ObjectName, new[] { owner }, excludeTeams, ct);
            result = await _authorship.ExcludeOwnWorksAsync(result, owner, ct);

            if (result.Zone != CopyZones.Green)
            {
                var checkId = await _client.RecordCheckAsync(new CopyCheckRecordRequest
                {
                    MediaId = media.Id,
                    PostId = post.PostId,
                    OwnerUserId = owner,
                    TeamId = team,
                    Bucket = media.Bucket,
                    ObjectName = media.ObjectName,
                    Result = result,
                }, ct);
                _logger.LogWarning(
                    "Post {PostId} media {MediaId}: possible copy of post {MatchedPostId} (P={Score:F2}, zone {Zone}, type {Type}), check {CheckId}",
                    post.PostId, media.Id, result.Best?.PostId, result.Score, result.Zone, result.MatchType, checkId);
            }

            await _client.IndexAsync(new CopyIndexRequest
            {
                MediaId = media.Id,
                PostId = post.PostId,
                OwnerUserId = owner,
                TeamId = team,
                Bucket = media.Bucket,
                ObjectName = media.ObjectName,
            }, ct);
        }
    }

    /// <summary>
    /// Keycloak subject of the author. Team posts have no PostAuthor row, and demo rows have no
    /// subject; the creator's user name stands in so the uploader still never matches themself.
    /// </summary>
    public static string OwnerId(Post post) =>
        post.Author?.ExternalId is { Length: > 0 } sub
            ? sub.ToLowerInvariant()
            : $"username:{post.Author?.UserName ?? post.CreatedBy ?? "unknown"}";

    /// <summary>One index entry per team post, keyed by the team, whichever portfolios show it.</summary>
    public static string? TeamId(Post post) =>
        post.Collaboration is null
            ? null
            : post.Collaboration.ExternalId ?? $"collaboration:{post.Collaboration.CollaborationId}";
}
