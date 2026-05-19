using System.Diagnostics;
using AggregatorService.Clients;
using AggregatorService.DTO;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorService.Controllers;

/// <summary>
/// Controller for aggregating data from ContentService and CollaborationService.
/// Implements the Aggregator Pattern to combine posts, comments, likes, saved posts, teams, and collaboration requests.
/// </summary>
[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public class ContentCollaborationAggregationController : ControllerBase
{
    private readonly ContentClient _contentClient;
    private readonly CollaborationClient _collaborationClient;
    private readonly ILogger<ContentCollaborationAggregationController> _logger;
 
    public ContentCollaborationAggregationController(
        ContentClient contentClient,
        CollaborationClient collaborationClient,
        ILogger<ContentCollaborationAggregationController> logger)
    {
        _contentClient = contentClient;
        _collaborationClient = collaborationClient;
        _logger = logger;
    }
 
    // -------------------------------------------------------------------------
    // POST ENDPOINTS
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// Gets full post details by aggregating data from ContentService and CollaborationService in parallel.
    /// Endpoint: GET /api/aggregator/posts/{postId}/full
    /// </summary>
    [HttpGet("posts/{postId:int}/full")]
    [ProducesResponseType(typeof(PostFullDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PostFullDetailsDto>> GetPostFullDetailsAsync(
        int postId,
        [FromQuery] int? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        _logger.LogInformation(
            "Starting post full details aggregation. PostId: {PostId}, CorrelationId: {CorrelationId}",
            postId, correlationId);
 
        try
        {
            // Step 1: Fetch post (required — cannot proceed without it)
            var post = await _contentClient.GetPostByIdAsync(postId, cancellationToken);
 
            if (post is null)
            {
                _logger.LogWarning(
                    "Post not found. PostId: {PostId}, CorrelationId: {CorrelationId}",
                    postId, correlationId);
                return NotFound(new { message = $"Post {postId} not found", postId });
            }
 
            // Step 1b: Enrich with team info via MongoDB if CollaborationSnapshotId was missing in SQL
            // (handles posts created before the externalid column was added)
            if (post.Collaboration == null)
            {
                post.Collaboration = await _collaborationClient.GetTeamCollaborationByPostIdAsync(
                    postId.ToString(), cancellationToken);
            }

            // Step 2: Parallel fetch — comments, likes, saved status, views
            var parallelStopwatch = Stopwatch.StartNew();
 
            var commentsTask    = _contentClient.GetCommentsByPostIdAsync(postId, cancellationToken);
            var likesCountTask  = _contentClient.GetLikesCountByPostIdAsync(postId, cancellationToken);
            var viewsCountTask  = _contentClient.GetViewsCountByPostIdAsync(postId, cancellationToken);
            var savedCountTask  = _contentClient.GetSavedCountByPostIdAsync(postId, cancellationToken);

            var isLikedTask = currentUserId.HasValue
                ? _contentClient.IsPostLikedByUserAsync(postId, currentUserId.Value, cancellationToken)
                : Task.FromResult(false);

            var isSavedTask = currentUserId.HasValue
                ? _contentClient.IsPostSavedByUserAsync(postId, currentUserId.Value, cancellationToken)
                : Task.FromResult(false);

            await Task.WhenAll(commentsTask, likesCountTask, viewsCountTask, savedCountTask, isLikedTask, isSavedTask);
 
            parallelStopwatch.Stop();
 
            _logger.LogInformation(
                "Parallel data fetching completed. PostId: {PostId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, parallelStopwatch.ElapsedMilliseconds, correlationId);
 
            // Step 3: Aggregate
            var comments = await commentsTask;
 
            var aggregated = new PostFullDetailsDto
            {
                Post                 = post,
                Comments             = comments,
                Team                 = null,
                LikesCount           = await likesCountTask,
                ViewsCount           = await viewsCountTask,
                SavedCount           = await savedCountTask,
                CommentsCount        = comments.Count,
                IsLikedByCurrentUser = await isLikedTask,
                IsSavedByCurrentUser = await isSavedTask
            };
 
            if (!aggregated.IsValid())
            {
                _logger.LogWarning(
                    "Data consistency validation failed. PostId: {PostId}, CorrelationId: {CorrelationId}",
                    postId, correlationId);
                // Continue — log warning but return data
            }
 
            overallStopwatch.Stop();
 
            _logger.LogInformation(
                "Post full details aggregation completed. PostId: {PostId}, TotalDuration: {Duration}ms, " +
                "ParallelDuration: {ParallelDuration}ms, CorrelationId: {CorrelationId}",
                postId, overallStopwatch.ElapsedMilliseconds, parallelStopwatch.ElapsedMilliseconds, correlationId);
 
            return Ok(aggregated);
        }
        catch (OperationCanceledException)
        {
            overallStopwatch.Stop();
            _logger.LogWarning(
                "Post full details aggregation cancelled. PostId: {PostId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, overallStopwatch.ElapsedMilliseconds, correlationId);
            return StatusCode(StatusCodes.Status499ClientClosedRequest, new { message = "Request was cancelled" });
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex,
                "Error during post full details aggregation. PostId: {PostId}, Duration: {Duration}ms, " +
                "CorrelationId: {CorrelationId}, ErrorType: {ErrorType}, ErrorMessage: {ErrorMessage}",
                postId, overallStopwatch.ElapsedMilliseconds, correlationId, ex.GetType().Name, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while aggregating post data", postId });
        }
    }
 
    /// <summary>
    /// Gets all posts with engagement metrics (likes, comments, views).
    /// Endpoint: GET /api/aggregator/posts/feed
    /// </summary>
    [HttpGet("posts/feed")]
    [ProducesResponseType(typeof(List<PostWithEngagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PostWithEngagementDto>>> GetPostFeedAsync(
        [FromQuery] int? currentUserId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();

        take = Math.Clamp(take, 1, 200);
        skip = Math.Max(skip, 0);

        _logger.LogInformation(
            "Starting post feed aggregation. Skip: {Skip}, Take: {Take}, SortBy: {SortBy}, CorrelationId: {CorrelationId}",
            skip, take, sortBy, correlationId);

        try
        {
            // Step 1: Fetch all posts from ContentService (already ordered by CreatedAt DESC)
            var allPosts = await _contentClient.GetAllPostsAsync(cancellationToken);

            if (!allPosts.Any())
            {
                _logger.LogInformation(
                    "No posts found for feed. CorrelationId: {CorrelationId}", correlationId);
                return Ok(new List<PostWithEngagementDto>());
            }

            List<PostWithEngagementDto> result;

            if (sortBy == "popular")
            {
                // Fetch engagement for ALL posts, sort globally by likes, then paginate
                var parallelStopwatch = Stopwatch.StartNew();
                var allEngaged = await Task.WhenAll(
                    allPosts.Select(post => GetPostEngagementInternalAsync(post, currentUserId, cancellationToken)));
                parallelStopwatch.Stop();

                result = allEngaged
                    .Where(p => p != null)
                    .OrderByDescending(p => p.LikesCount)
                    .Skip(skip)
                    .Take(take)
                    .ToList();

                overallStopwatch.Stop();
                _logger.LogInformation(
                    "Post feed (popular) completed. Total: {Total}, Served: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                    allPosts.Count, result.Count, overallStopwatch.ElapsedMilliseconds, correlationId);
            }
            else
            {
                // Time sort (default): posts already ordered by CreatedAt DESC, paginate first
                var page = allPosts.Skip(skip).Take(take).ToList();

                if (!page.Any())
                    return Ok(new List<PostWithEngagementDto>());

                var parallelStopwatch = Stopwatch.StartNew();
                var engagedPosts = await Task.WhenAll(
                    page.Select(post => GetPostEngagementInternalAsync(post, currentUserId, cancellationToken)));
                parallelStopwatch.Stop();

                result = engagedPosts.Where(p => p != null).ToList();

                overallStopwatch.Stop();
                _logger.LogInformation(
                    "Post feed (time) completed. Count: {Count}, TotalDuration: {Duration}ms, " +
                    "ParallelDuration: {ParallelDuration}ms, CorrelationId: {CorrelationId}",
                    result.Count, overallStopwatch.ElapsedMilliseconds,
                    parallelStopwatch.ElapsedMilliseconds, correlationId);
            }

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            overallStopwatch.Stop();
            _logger.LogWarning(
                "Post feed aggregation cancelled. Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                overallStopwatch.ElapsedMilliseconds, correlationId);
            return StatusCode(StatusCodes.Status499ClientClosedRequest, new { message = "Request was cancelled" });
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex,
                "Error during post feed aggregation. Duration: {Duration}ms, " +
                "CorrelationId: {CorrelationId}, ErrorType: {ErrorType}, ErrorMessage: {ErrorMessage}",
                overallStopwatch.ElapsedMilliseconds, correlationId, ex.GetType().Name, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while aggregating the post feed" });
        }
    }
 
    /// <summary>
    /// Gets the full portfolio for a user: personal posts (by PostAuthors.UserId) + all team posts
    /// from every team the user is a member of.
    /// Endpoint: GET /api/aggregator/posts/portfolio
    /// </summary>
    [HttpGet("posts/portfolio")]
    [ProducesResponseType(typeof(List<PostWithEngagementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PostWithEngagementDto>>> GetUserPortfolioAsync(
        [FromQuery] int contentUserId,
        [FromQuery] string? identityUserId = null,
        [FromQuery] int? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        // Personal posts — filter by PostAuthors.UserId (the external identity integer reference)
        var personalPostsTask = _contentClient.GetPostsByUserIdAsync(contentUserId, cancellationToken);

        // Teams the user belongs to (needed for team posts)
        var teamsTask = identityUserId != null
            ? _collaborationClient.GetTeamsByMemberAsync(identityUserId, cancellationToken)
            : Task.FromResult(new List<AggregatorTeamSummaryDto>());

        await Task.WhenAll(personalPostsTask, teamsTask);

        var personalPosts = await personalPostsTask;
        var teams         = await teamsTask;

        // Collect team post IDs, excluding any already in personal posts
        var seen       = new HashSet<int>(personalPosts.Select(p => p.PostId));
        var teamPostIds = new List<int>();

        if (teams.Count > 0)
        {
            var teamPostBatches = await Task.WhenAll(
                teams.Select(t => _collaborationClient.GetTeamPostsByTeamAsync(t.TeamId, cancellationToken)));

            foreach (var batch in teamPostBatches)
                foreach (var tp in batch)
                    if (int.TryParse(tp.PostId, out var pid) && seen.Add(pid))
                        teamPostIds.Add(pid);
        }

        // Fetch full content data for team-only post IDs
        var teamContentPosts = (await Task.WhenAll(
            teamPostIds.Select(pid => _contentClient.GetPostByIdAsync(pid, cancellationToken))))
            .Where(p => p != null)
            .ToList();

        var allPosts = personalPosts.Concat(teamContentPosts).ToList();

        if (!allPosts.Any())
            return Ok(new List<PostWithEngagementDto>());

        var engaged = await Task.WhenAll(
            allPosts.Select(p => GetPostEngagementInternalAsync(p, currentUserId, cancellationToken)));

        var result = engaged
            .Where(p => p != null)
            .OrderByDescending(p => p!.Post.CreatedAt)
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Gets all posts by a specific author (by postAuthorId from ContentService) with engagement metrics.
    /// Endpoint: GET /api/aggregator/posts/by-author/{postAuthorId}
    /// </summary>
    [HttpGet("posts/by-author/{postAuthorId:int}")]
    [ProducesResponseType(typeof(List<PostWithEngagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PostWithEngagementDto>>> GetPostsByAuthorAsync(
        int postAuthorId,
        [FromQuery] int? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        _logger.LogInformation(
            "Starting posts by author aggregation. PostAuthorId: {PostAuthorId}, CorrelationId: {CorrelationId}",
            postAuthorId, correlationId);
 
        try
        {
            var posts = await _contentClient.GetPostsByAuthorAsync(postAuthorId, cancellationToken);
 
            if (!posts.Any())
            {
                _logger.LogInformation(
                    "No posts found for author. PostAuthorId: {PostAuthorId}, CorrelationId: {CorrelationId}",
                    postAuthorId, correlationId);
                return Ok(new List<PostWithEngagementDto>());
            }
 
            var parallelStopwatch = Stopwatch.StartNew();
 
            var engagementTasks = posts.Select(post =>
                GetPostEngagementInternalAsync(post, currentUserId, cancellationToken));
 
            var engagedPosts = await Task.WhenAll(engagementTasks);
 
            parallelStopwatch.Stop();
            overallStopwatch.Stop();
 
            _logger.LogInformation(
                "Posts by author aggregation completed. PostAuthorId: {PostAuthorId}, Count: {Count}, " +
                "TotalDuration: {Duration}ms, CorrelationId: {CorrelationId}",
                postAuthorId, engagedPosts.Length, overallStopwatch.ElapsedMilliseconds, correlationId);
 
            return Ok(engagedPosts.Where(p => p != null).ToList());
        }
        catch (OperationCanceledException)
        {
            overallStopwatch.Stop();
            _logger.LogWarning(
                "Posts by author aggregation cancelled. PostAuthorId: {PostAuthorId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postAuthorId, overallStopwatch.ElapsedMilliseconds, correlationId);
            return StatusCode(StatusCodes.Status499ClientClosedRequest, new { message = "Request was cancelled" });
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex,
                "Error during posts by author aggregation. PostAuthorId: {PostAuthorId}, Duration: {Duration}ms, " +
                "CorrelationId: {CorrelationId}, ErrorType: {ErrorType}, ErrorMessage: {ErrorMessage}",
                postAuthorId, overallStopwatch.ElapsedMilliseconds, correlationId, ex.GetType().Name, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while aggregating posts by author", postAuthorId });
        }
    }
 
    // -------------------------------------------------------------------------
    // TEAM ENDPOINTS
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// Gets a team with all its team posts from CollaborationService.
    /// Endpoint: GET /api/aggregator/teams/{teamId}/full
    /// </summary>
    [HttpGet("teams/{teamId}/full")]
    [ProducesResponseType(typeof(TeamWithPostsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TeamWithPostsDto>> GetTeamWithPostsAsync(
        string teamId,
        CancellationToken cancellationToken = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        _logger.LogInformation(
            "Starting team with posts aggregation. TeamId: {TeamId}, CorrelationId: {CorrelationId}",
            teamId, correlationId);
 
        try
        {
            // Step 1: Fetch team (required)
            var team = await _collaborationClient.GetTeamByIdAsync(teamId, cancellationToken);
 
            if (team is null)
            {
                _logger.LogWarning(
                    "Team not found. TeamId: {TeamId}, CorrelationId: {CorrelationId}",
                    teamId, correlationId);
                return NotFound(new { message = $"Team {teamId} not found", teamId });
            }
 
            // Step 2: Fetch team posts and collaboration requests in parallel
            var teamPostsTask = _collaborationClient.GetTeamPostsByTeamAsync(teamId, cancellationToken);
            var requestsTask  = _collaborationClient.GetCollaborationRequestsByTeamAsync(teamId, cancellationToken);
            await Task.WhenAll(teamPostsTask, requestsTask);

            var teamPosts = await teamPostsTask;
            var collaborationRequests = await requestsTask;

            // Step 3: Enrich each team post with mediaUrls and tags from ContentAPI (parallel).
            // Returns null if the content post was deleted — those entries are excluded below.
            var enrichTasks = teamPosts.Select(async tp =>
            {
                if (!int.TryParse(tp.PostId, out var contentPostId))
                    return null;

                var fullPost = await _contentClient.GetPostByIdAsync(contentPostId, cancellationToken);
                if (fullPost == null)
                    return null;

                tp.MediaUrls = fullPost.MediaUrls;
                tp.Tags      = fullPost.Tags;
                return tp;
            });

            var enrichedPosts = (await Task.WhenAll(enrichTasks))
                .Where(tp => tp != null)
                .ToList();

            overallStopwatch.Stop();
            _logger.LogInformation(
                "Team with posts aggregation completed. TeamId: {TeamId}, PostsCount: {Count}, " +
                "TotalDuration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, enrichedPosts.Count, overallStopwatch.ElapsedMilliseconds, correlationId);

            var aggregated = new TeamWithPostsDto
            {
                Team                 = team,
                Posts                = enrichedPosts,
                CollaborationRequests = collaborationRequests
            };
 
            return Ok(aggregated);
        }
        catch (OperationCanceledException)
        {
            overallStopwatch.Stop();
            _logger.LogWarning(
                "Team with posts aggregation cancelled. TeamId: {TeamId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, overallStopwatch.ElapsedMilliseconds, correlationId);
            return StatusCode(StatusCodes.Status499ClientClosedRequest, new { message = "Request was cancelled" });
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex,
                "Error during team with posts aggregation. TeamId: {TeamId}, Duration: {Duration}ms, " +
                "CorrelationId: {CorrelationId}, ErrorType: {ErrorType}, ErrorMessage: {ErrorMessage}",
                teamId, overallStopwatch.ElapsedMilliseconds, correlationId, ex.GetType().Name, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while aggregating team data", teamId });
        }
    }
 
    // -------------------------------------------------------------------------
    // USER DASHBOARD ENDPOINT
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// Gets the full user dashboard by aggregating data from both services in parallel.
    /// Endpoint: GET /api/aggregator/users/{userId}/dashboard
    /// </summary>
    [HttpGet("users/{userId:int}/dashboard")]
    [ProducesResponseType(typeof(UserDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDashboardDto>> GetUserDashboardAsync(
        int userId,
        [FromQuery] string? identityUserId = null,
        CancellationToken cancellationToken = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        _logger.LogInformation(
            "Starting user dashboard aggregation. UserId: {UserId}, CorrelationId: {CorrelationId}",
            userId, correlationId);
 
        try
        {
            // All requests are independent — fire all in parallel
            var parallelStopwatch = Stopwatch.StartNew();
 
            // ContentService: posts by UserId (external identity ref), saved posts by userId
            var myPostsTask    = _contentClient.GetPostsByUserIdAsync(userId, cancellationToken);
            var savedPostsTask = _contentClient.GetSavedPostsByUserIdAsync(userId, cancellationToken);
 
            // CollaborationService: teams, collaboration requests, group invitations — all by userId string
            // Use the Identity GUID (identityUserId) for team membership lookup since teams store members by Identity userId.
            // Fall back to stringified content userId only if identityUserId is not provided.
            var userIdStr              = userId.ToString();
            var memberLookupId         = !string.IsNullOrEmpty(identityUserId) ? identityUserId : userIdStr;
            var myTeamsTask            = _collaborationClient.GetTeamsByMemberAsync(memberLookupId, cancellationToken);
            var pendingRequestsTask    = _collaborationClient.GetCollaborationRequestsByUserAsync(memberLookupId, cancellationToken);
            var pendingInvitationsTask = _collaborationClient.GetGroupInvitationsByUserAsync(memberLookupId, cancellationToken);
 
            await Task.WhenAll(
                myPostsTask,
                savedPostsTask,
                myTeamsTask,
                pendingRequestsTask,
                pendingInvitationsTask);

            // Enrich pending requests with team names in parallel
            var pendingRequests = await pendingRequestsTask;
            var uniqueTeamIds = pendingRequests
                .Select(r => r.TeamId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            if (uniqueTeamIds.Count > 0)
            {
                var teamNameTasks = uniqueTeamIds.ToDictionary(
                    id => id,
                    id => _collaborationClient.GetTeamByIdAsync(id, cancellationToken));
                await Task.WhenAll(teamNameTasks.Values);

                foreach (var req in pendingRequests)
                    if (!string.IsNullOrEmpty(req.TeamId) && teamNameTasks.TryGetValue(req.TeamId, out var t))
                        req.TeamName = t.Result?.Name;
            }

            parallelStopwatch.Stop();
            overallStopwatch.Stop();

            _logger.LogInformation(
                "User dashboard aggregation completed. UserId: {UserId}, TotalDuration: {Duration}ms, " +
                "ParallelDuration: {ParallelDuration}ms, CorrelationId: {CorrelationId}",
                userId, overallStopwatch.ElapsedMilliseconds, parallelStopwatch.ElapsedMilliseconds, correlationId);

            var dashboard = new UserDashboardDto
            {
                UserId             = userId,
                MyPosts            = await myPostsTask,
                SavedPosts         = await savedPostsTask,
                MyTeams            = await myTeamsTask,
                PendingRequests    = pendingRequests,
                PendingInvitations = await pendingInvitationsTask
            };
 
            if (!dashboard.IsValid())
            {
                _logger.LogWarning(
                    "Dashboard validation failed. UserId: {UserId}, CorrelationId: {CorrelationId}",
                    userId, correlationId);
            }
 
            return Ok(dashboard);
        }
        catch (OperationCanceledException)
        {
            overallStopwatch.Stop();
            _logger.LogWarning(
                "User dashboard aggregation cancelled. UserId: {UserId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, overallStopwatch.ElapsedMilliseconds, correlationId);
            return StatusCode(StatusCodes.Status499ClientClosedRequest, new { message = "Request was cancelled" });
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex,
                "Error during user dashboard aggregation. UserId: {UserId}, Duration: {Duration}ms, " +
                "CorrelationId: {CorrelationId}, ErrorType: {ErrorType}, ErrorMessage: {ErrorMessage}",
                userId, overallStopwatch.ElapsedMilliseconds, correlationId, ex.GetType().Name, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while aggregating user dashboard", userId });
        }
    }
 
    // -------------------------------------------------------------------------
    // INTERNAL HELPERS
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// Internal helper — fetches engagement data for a single post.
    /// Reused per-post inside the feed aggregation.
    /// </summary>
    private async Task<PostWithEngagementDto?> GetPostEngagementInternalAsync(
        AggregatorPostDto post,
        int? currentUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Enrich with team info via MongoDB if CollaborationSnapshotId was missing in SQL
            if (post.Collaboration == null)
            {
                post.Collaboration = await _collaborationClient.GetTeamCollaborationByPostIdAsync(
                    post.PostId.ToString(), cancellationToken);
            }

            var likesCountTask = _contentClient.GetLikesCountByPostIdAsync(post.PostId, cancellationToken);
            var viewsCountTask = _contentClient.GetViewsCountByPostIdAsync(post.PostId, cancellationToken);
            var savedCountTask = _contentClient.GetSavedCountByPostIdAsync(post.PostId, cancellationToken);

            // ContentService does not expose a dedicated comments count endpoint —
            // fetch full list and count locally to avoid a separate round-trip.
            var commentsTask = _contentClient.GetCommentsByPostIdAsync(post.PostId, cancellationToken);

            var isLikedTask = currentUserId.HasValue
                ? _contentClient.IsPostLikedByUserAsync(post.PostId, currentUserId.Value, cancellationToken)
                : Task.FromResult(false);

            var isSavedTask = currentUserId.HasValue
                ? _contentClient.IsPostSavedByUserAsync(post.PostId, currentUserId.Value, cancellationToken)
                : Task.FromResult(false);

            await Task.WhenAll(likesCountTask, viewsCountTask, savedCountTask, commentsTask, isLikedTask, isSavedTask);

            return new PostWithEngagementDto
            {
                Post                 = post,
                LikesCount           = await likesCountTask,
                ViewsCount           = await viewsCountTask,
                SavedCount           = await savedCountTask,
                CommentsCount        = (await commentsTask).Count,
                IsLikedByCurrentUser = await isLikedTask,
                IsSavedByCurrentUser = await isSavedTask
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching engagement for post internally. PostId: {PostId}", post.PostId);
            return null;
        }
    }
 
    private string GetCorrelationId() =>
        HttpContext.Items["X-Correlation-Id"]?.ToString()
        ?? HttpContext.Items["CorrelationId"]?.ToString()
        ?? "unknown";
}