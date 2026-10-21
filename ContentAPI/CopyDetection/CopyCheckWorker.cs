using ContentDAL.UOW;
using ContentDomain.Entity;

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
    private readonly IImageCopyClient _client;
    private readonly IAuthorshipFilter _authorship;
    private readonly ILogger<CopyJobProcessor> _logger;

    public CopyJobProcessor(IUnitOfWork uow, IImageCopyClient client, IAuthorshipFilter authorship, ILogger<CopyJobProcessor> logger)
    {
        _uow = uow;
        _client = client;
        _authorship = authorship;
        _logger = logger;
    }

    public async Task ProcessAsync(CopyJob job, CancellationToken ct)
    {
        if (job.Kind == CopyJobKind.RemovePost)
        {
            var removed = await _client.DeletePostAsync(job.PostId, ct);
            _logger.LogInformation("Removed {Count} image(s) of post {PostId} from the copy index", removed, job.PostId);
            return;
        }

        var post = await _uow.PostRepository.GetByIdAsync(job.PostId, ct);
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
