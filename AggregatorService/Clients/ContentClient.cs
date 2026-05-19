using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using AggregatorService.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AggregatorService.Clients;

/// <summary>
/// Typed HTTP client for ContentService microservice.
/// Encapsulates all interactions with the content service using service discovery.
/// CorrelationId is automatically propagated via CorrelationIdDelegatingHandler from ServiceDefaults.
/// </summary>
public class ContentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentClient> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;
 
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
 
    public ContentClient(
        HttpClient httpClient,
        ILogger<ContentClient> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
 
    // -------------------------------------------------------------------------
    // POSTS  →  PostController: api/post
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// GET api/post/{id}
    /// </summary>
    public async Task<AggregatorPostDto?> GetPostByIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService for post {PostId}. CorrelationId: {CorrelationId}",
                postId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/post/{postId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error. PostId: {PostId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    postId, response.StatusCode, correlationId);
                return null;
            }
 
            var post = await response.Content.ReadFromJsonAsync<AggregatorPostDto>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded successfully. PostId: {PostId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, stopwatch.ElapsedMilliseconds, correlationId);
 
            return post;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for post. PostId: {PostId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, stopwatch.ElapsedMilliseconds, correlationId);
            return null;
        }
    }
 
    /// <summary>
    /// GET api/post  →  повертає всі пости
    /// </summary>
    public async Task<List<AggregatorPostDto>> GetAllPostsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService for all posts. CorrelationId: {CorrelationId}", correlationId);
 
            var response = await _httpClient.GetAsync("/api/post", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for all posts. StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    response.StatusCode, correlationId);
                return new List<AggregatorPostDto>();
            }
 
            var posts = await response.Content.ReadFromJsonAsync<List<AggregatorPostDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with all posts. Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                posts?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return posts ?? new List<AggregatorPostDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for all posts. Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorPostDto>();
        }
    }
 
    /// <summary>
    /// GET api/post/by-author/{postAuthorId}
    /// </summary>
    public async Task<List<AggregatorPostDto>> GetPostsByAuthorAsync(int postAuthorId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService for posts by author. PostAuthorId: {PostAuthorId}, CorrelationId: {CorrelationId}",
                postAuthorId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/post/by-author/{postAuthorId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for author posts. PostAuthorId: {PostAuthorId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    postAuthorId, response.StatusCode, correlationId);
                return new List<AggregatorPostDto>();
            }
 
            var posts = await response.Content.ReadFromJsonAsync<List<AggregatorPostDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with author posts. PostAuthorId: {PostAuthorId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postAuthorId, posts?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return posts ?? new List<AggregatorPostDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for author posts. PostAuthorId: {PostAuthorId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postAuthorId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorPostDto>();
        }
    }
 
    /// <summary>
    /// GET api/post/by-userid/{userId}  — filters by PostAuthor.UserId (external identity reference)
    /// </summary>
    public async Task<List<AggregatorPostDto>> GetPostsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/post/by-userid/{userId}", cancellationToken);
            if (!response.IsSuccessStatusCode) return new List<AggregatorPostDto>();
            return await response.Content.ReadFromJsonAsync<List<AggregatorPostDto>>(JsonOptions, cancellationToken)
                   ?? new List<AggregatorPostDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ContentService for posts by userId. UserId: {UserId}", userId);
            return new List<AggregatorPostDto>();
        }
    }

    /// <summary>
    /// GET api/post/by-collaboration/{collaborationSnapshotId}
    /// </summary>
    public async Task<List<AggregatorPostDto>> GetPostsByCollaborationSnapshotAsync(int collaborationSnapshotId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService for posts by collaboration snapshot. CollaborationSnapshotId: {Id}, CorrelationId: {CorrelationId}",
                collaborationSnapshotId, correlationId);
 
            var response = await _httpClient.GetAsync(
                $"/api/post/by-collaboration/{collaborationSnapshotId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for collaboration posts. CollaborationSnapshotId: {Id}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    collaborationSnapshotId, response.StatusCode, correlationId);
                return new List<AggregatorPostDto>();
            }
 
            var posts = await response.Content.ReadFromJsonAsync<List<AggregatorPostDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with collaboration posts. CollaborationSnapshotId: {Id}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                collaborationSnapshotId, posts?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return posts ?? new List<AggregatorPostDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for collaboration posts. CollaborationSnapshotId: {Id}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                collaborationSnapshotId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorPostDto>();
        }
    }
 
    // -------------------------------------------------------------------------
    // COMMENTS  →  CommentController: api/comment
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// GET api/comment/by-post/{postId}
    /// </summary>
    public async Task<List<AggregatorCommentDto>> GetCommentsByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService for comments. PostId: {PostId}, CorrelationId: {CorrelationId}",
                postId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/comment/by-post/{postId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for comments. PostId: {PostId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    postId, response.StatusCode, correlationId);
                return new List<AggregatorCommentDto>();
            }
 
            var comments = await response.Content.ReadFromJsonAsync<List<AggregatorCommentDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with comments. PostId: {PostId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, comments?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return comments ?? new List<AggregatorCommentDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for comments. PostId: {PostId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorCommentDto>();
        }
    }
 
    /// <summary>
    /// GET api/comment/{parentCommentId}/replies
    /// </summary>
    public async Task<List<AggregatorCommentDto>> GetRepliesAsync(int parentCommentId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService for replies. ParentCommentId: {ParentCommentId}, CorrelationId: {CorrelationId}",
                parentCommentId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/comment/{parentCommentId}/replies", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for replies. ParentCommentId: {ParentCommentId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    parentCommentId, response.StatusCode, correlationId);
                return new List<AggregatorCommentDto>();
            }
 
            var replies = await response.Content.ReadFromJsonAsync<List<AggregatorCommentDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with replies. ParentCommentId: {ParentCommentId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                parentCommentId, replies?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return replies ?? new List<AggregatorCommentDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for replies. ParentCommentId: {ParentCommentId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                parentCommentId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorCommentDto>();
        }
    }
 
    // -------------------------------------------------------------------------
    // LIKES  →  LikeController: api/like
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// GET api/like/count?postId={postId}
    /// </summary>
    public async Task<int> GetLikesCountByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService for likes count. PostId: {PostId}, CorrelationId: {CorrelationId}",
                postId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/like/count?postId={postId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for likes count. PostId: {PostId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    postId, response.StatusCode, correlationId);
                return 0;
            }
 
            var count = await response.Content.ReadFromJsonAsync<int>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with likes count. PostId: {PostId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, count, stopwatch.ElapsedMilliseconds, correlationId);
 
            return count;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for likes count. PostId: {PostId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, stopwatch.ElapsedMilliseconds, correlationId);
            return 0;
        }
    }
 
    /// <summary>
    /// GET api/like/exists?postId={postId}&userId={userId}
    /// </summary>
    public async Task<bool> IsPostLikedByUserAsync(int postId, int userId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService to check like status. PostId: {PostId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                postId, userId, correlationId);
 
            var response = await _httpClient.GetAsync(
                $"/api/like/exists?postId={postId}&userId={userId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for like check. PostId: {PostId}, UserId: {UserId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    postId, userId, response.StatusCode, correlationId);
                return false;
            }
 
            var exists = await response.Content.ReadFromJsonAsync<bool>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with like status. PostId: {PostId}, UserId: {UserId}, IsLiked: {IsLiked}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, userId, exists, stopwatch.ElapsedMilliseconds, correlationId);
 
            return exists;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for like check. PostId: {PostId}, UserId: {UserId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, userId, stopwatch.ElapsedMilliseconds, correlationId);
            return false;
        }
    }
 
    // -------------------------------------------------------------------------
    // SAVED POSTS  →  SavedPostController: api/savedpost
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// GET api/savedpost/by-user/{userId}
    /// </summary>
    public async Task<List<AggregatorSavedPostDto>> GetSavedPostsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService for saved posts. UserId: {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/savedpost/by-user/{userId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for saved posts. UserId: {UserId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    userId, response.StatusCode, correlationId);
                return new List<AggregatorSavedPostDto>();
            }
 
            var savedPosts = await response.Content
                .ReadFromJsonAsync<List<AggregatorSavedPostDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with saved posts. UserId: {UserId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, savedPosts?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return savedPosts ?? new List<AggregatorSavedPostDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for saved posts. UserId: {UserId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorSavedPostDto>();
        }
    }
 
    /// <summary>
    /// GET api/savedpost/count?postId={postId}
    /// </summary>
    public async Task<int> GetSavedCountByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/savedpost/count?postId={postId}", cancellationToken);
            if (!response.IsSuccessStatusCode) return 0;
            return await response.Content.ReadFromJsonAsync<int>(JsonOptions, cancellationToken);
        }
        catch { return 0; }
    }

    /// <summary>
    /// GET api/savedpost/exists?postId={postId}&userId={userId}
    /// </summary>
    public async Task<bool> IsPostSavedByUserAsync(int postId, int userId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService to check saved status. PostId: {PostId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                postId, userId, correlationId);
 
            var response = await _httpClient.GetAsync(
                $"/api/savedpost/exists?postId={postId}&userId={userId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for saved check. PostId: {PostId}, UserId: {UserId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    postId, userId, response.StatusCode, correlationId);
                return false;
            }
 
            var exists = await response.Content.ReadFromJsonAsync<bool>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with saved status. PostId: {PostId}, UserId: {UserId}, IsSaved: {IsSaved}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, userId, exists, stopwatch.ElapsedMilliseconds, correlationId);
 
            return exists;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for saved check. PostId: {PostId}, UserId: {UserId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, userId, stopwatch.ElapsedMilliseconds, correlationId);
            return false;
        }
    }
 
    // -------------------------------------------------------------------------
    // POST VIEWS  →  PostViewController: api/postview
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// GET api/postview/count?postId={postId}
    /// </summary>
    public async Task<int> GetViewsCountByPostIdAsync(int postId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling ContentService for views count. PostId: {PostId}, CorrelationId: {CorrelationId}",
                postId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/postview/count?postId={postId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ContentService returned error for views count. PostId: {PostId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    postId, response.StatusCode, correlationId);
                return 0;
            }
 
            var count = await response.Content.ReadFromJsonAsync<int>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "ContentService responded with views count. PostId: {PostId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, count, stopwatch.ElapsedMilliseconds, correlationId);
 
            return count;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling ContentService for views count. PostId: {PostId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                postId, stopwatch.ElapsedMilliseconds, correlationId);
            return 0;
        }
    }
 
    // -------------------------------------------------------------------------
    // HELPERS
    // -------------------------------------------------------------------------
 
    private string GetCorrelationId()
    {
        if (_httpContextAccessor?.HttpContext != null)
        {
            return _httpContextAccessor.HttpContext.Items["X-Correlation-Id"]?.ToString()
                ?? _httpContextAccessor.HttpContext.Items["CorrelationId"]?.ToString()
                ?? "unknown";
        }
        return "unknown";
    }
}