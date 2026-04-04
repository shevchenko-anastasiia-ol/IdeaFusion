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
 
            // Step 2: Parallel fetch — comments, likes, saved status, views
            // Team is NOT fetched here: AggregatorCollaborationDto only carries CollaborationId/CollaborationSnapshotId,
            // not a TeamId. Use GET /api/aggregator/teams/{teamId}/full to get team with posts.
            var parallelStopwatch = Stopwatch.StartNew();
 
            var commentsTask   = _contentClient.GetCommentsByPostIdAsync(postId, cancellationToken);
            var likesCountTask = _contentClient.GetLikesCountByPostIdAsync(postId, cancellationToken);
            var viewsCountTask = _contentClient.GetViewsCountByPostIdAsync(postId, cancellationToken);
 
            var isLikedTask = currentUserId.HasValue
                ? _contentClient.IsPostLikedByUserAsync(postId, currentUserId.Value, cancellationToken)
                : Task.FromResult(false);
 
            var isSavedTask = currentUserId.HasValue
                ? _contentClient.IsPostSavedByUserAsync(postId, currentUserId.Value, cancellationToken)
                : Task.FromResult(false);
 
            await Task.WhenAll(commentsTask, likesCountTask, viewsCountTask, isLikedTask, isSavedTask);
 
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
                Team                 = null, // resolve via GET /api/aggregator/teams/{teamId}/full
                LikesCount           = await likesCountTask,
                ViewsCount           = await viewsCountTask,
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
        CancellationToken cancellationToken = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        _logger.LogInformation(
            "Starting post feed aggregation. CorrelationId: {CorrelationId}", correlationId);
 
        try
        {
            // Step 1: Fetch all posts from ContentService
            var posts = await _contentClient.GetAllPostsAsync(cancellationToken);
 
            if (!posts.Any())
            {
                _logger.LogInformation(
                    "No posts found for feed. CorrelationId: {CorrelationId}", correlationId);
                return Ok(new List<PostWithEngagementDto>());
            }
 
            _logger.LogInformation(
                "Fetched {Count} posts. Starting parallel engagement fetch. CorrelationId: {CorrelationId}",
                posts.Count, correlationId);
 
            // Step 2: Fetch engagement for all posts in parallel
            var parallelStopwatch = Stopwatch.StartNew();
 
            var engagementTasks = posts.Select(post =>
                GetPostEngagementInternalAsync(post, currentUserId, cancellationToken));
 
            var engagedPosts = await Task.WhenAll(engagementTasks);
 
            parallelStopwatch.Stop();
            overallStopwatch.Stop();
 
            _logger.LogInformation(
                "Post feed aggregation completed. Count: {Count}, TotalDuration: {Duration}ms, " +
                "ParallelDuration: {ParallelDuration}ms, CorrelationId: {CorrelationId}",
                engagedPosts.Length, overallStopwatch.ElapsedMilliseconds,
                parallelStopwatch.ElapsedMilliseconds, correlationId);
 
            return Ok(engagedPosts.Where(p => p != null).ToList());
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
 
            // Step 2: Fetch team posts from CollaborationService in parallel
            var parallelStopwatch = Stopwatch.StartNew();
 
            var postsTask = _collaborationClient.GetTeamPostsByTeamAsync(teamId, cancellationToken);
            await postsTask;
 
            parallelStopwatch.Stop();
            overallStopwatch.Stop();
 
            _logger.LogInformation(
                "Team with posts aggregation completed. TeamId: {TeamId}, PostsCount: {Count}, " +
                "TotalDuration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, (await postsTask).Count, overallStopwatch.ElapsedMilliseconds, correlationId);
 
            var aggregated = new TeamWithPostsDto
            {
                Team  = team,
                Posts = await postsTask
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
 
            // ContentService: posts by postAuthorId, saved posts by userId
            var myPostsTask    = _contentClient.GetPostsByAuthorAsync(userId, cancellationToken);
            var savedPostsTask = _contentClient.GetSavedPostsByUserIdAsync(userId, cancellationToken);
 
            // CollaborationService: teams, collaboration requests, group invitations — all by userId string
            var userIdStr              = userId.ToString();
            var myTeamsTask            = _collaborationClient.GetTeamsByMemberAsync(userIdStr, cancellationToken);
            var pendingRequestsTask    = _collaborationClient.GetCollaborationRequestsByUserAsync(userIdStr, cancellationToken);
            var pendingInvitationsTask = _collaborationClient.GetGroupInvitationsByUserAsync(userIdStr, cancellationToken);
 
            await Task.WhenAll(
                myPostsTask,
                savedPostsTask,
                myTeamsTask,
                pendingRequestsTask,
                pendingInvitationsTask);
 
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
                PendingRequests    = await pendingRequestsTask,
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
            var likesCountTask = _contentClient.GetLikesCountByPostIdAsync(post.PostId, cancellationToken);
            var viewsCountTask = _contentClient.GetViewsCountByPostIdAsync(post.PostId, cancellationToken);
 
            // ContentService does not expose a dedicated comments count endpoint —
            // fetch full list and count locally to avoid a separate round-trip.
            var commentsTask = _contentClient.GetCommentsByPostIdAsync(post.PostId, cancellationToken);
 
            var isLikedTask = currentUserId.HasValue
                ? _contentClient.IsPostLikedByUserAsync(post.PostId, currentUserId.Value, cancellationToken)
                : Task.FromResult(false);
 
            var isSavedTask = currentUserId.HasValue
                ? _contentClient.IsPostSavedByUserAsync(post.PostId, currentUserId.Value, cancellationToken)
                : Task.FromResult(false);
 
            await Task.WhenAll(likesCountTask, viewsCountTask, commentsTask, isLikedTask, isSavedTask);
 
            return new PostWithEngagementDto
            {
                Post                 = post,
                LikesCount           = await likesCountTask,
                ViewsCount           = await viewsCountTask,
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