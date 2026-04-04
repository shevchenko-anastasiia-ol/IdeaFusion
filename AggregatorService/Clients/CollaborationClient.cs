using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using AggregatorService.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AggregatorService.Clients;

/// <summary>
/// Typed HTTP client for CollaborationService microservice.
/// Encapsulates all interactions with the collaboration service using service discovery.
/// CorrelationId is automatically propagated via CorrelationIdDelegatingHandler from ServiceDefaults.
/// </summary>
public class CollaborationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CollaborationClient> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;
 
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
 
    public CollaborationClient(
        HttpClient httpClient,
        ILogger<CollaborationClient> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
 
    // -------------------------------------------------------------------------
    // TEAMS  →  TeamController: api/team
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// GET api/team/{id}
    /// </summary>
    public async Task<AggregatorTeamDto?> GetTeamByIdAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling CollaborationService for team {TeamId}. CorrelationId: {CorrelationId}",
                teamId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/team/{teamId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CollaborationService returned error. TeamId: {TeamId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    teamId, response.StatusCode, correlationId);
                return null;
            }
 
            var team = await response.Content.ReadFromJsonAsync<AggregatorTeamDto>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "CollaborationService responded successfully. TeamId: {TeamId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, stopwatch.ElapsedMilliseconds, correlationId);
 
            return team;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling CollaborationService for team. TeamId: {TeamId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, stopwatch.ElapsedMilliseconds, correlationId);
            return null;
        }
    }
 
    /// <summary>
    /// GET api/team/member/{userId}
    /// </summary>
    public async Task<List<AggregatorTeamSummaryDto>> GetTeamsByMemberAsync(string userId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling CollaborationService for teams by member. UserId: {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/team/member/{userId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CollaborationService returned error for member teams. UserId: {UserId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    userId, response.StatusCode, correlationId);
                return new List<AggregatorTeamSummaryDto>();
            }
 
            var teams = await response.Content
                .ReadFromJsonAsync<List<AggregatorTeamSummaryDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "CollaborationService responded with member teams. UserId: {UserId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, teams?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return teams ?? new List<AggregatorTeamSummaryDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling CollaborationService for member teams. UserId: {UserId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorTeamSummaryDto>();
        }
    }
 
    // -------------------------------------------------------------------------
    // TEAM POSTS  →  TeamPostController: api/team-posts
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// GET api/team-posts/team/{teamId}
    /// </summary>
    public async Task<List<AggregatorTeamPostDto>> GetTeamPostsByTeamAsync(string teamId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling CollaborationService for team posts. TeamId: {TeamId}, CorrelationId: {CorrelationId}",
                teamId, correlationId);
 
            var response = await _httpClient.GetAsync($"/api/team-posts/team/{teamId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CollaborationService returned error for team posts. TeamId: {TeamId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    teamId, response.StatusCode, correlationId);
                return new List<AggregatorTeamPostDto>();
            }
 
            var posts = await response.Content
                .ReadFromJsonAsync<List<AggregatorTeamPostDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "CollaborationService responded with team posts. TeamId: {TeamId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, posts?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return posts ?? new List<AggregatorTeamPostDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling CollaborationService for team posts. TeamId: {TeamId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorTeamPostDto>();
        }
    }
 
    // -------------------------------------------------------------------------
    // COLLABORATION REQUESTS  →  CollaborationRequestController: api/collaboration-requests
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// GET api/collaboration-requests/user/{userId}
    /// </summary>
    public async Task<List<AggregatorCollaborationRequestDto>> GetCollaborationRequestsByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling CollaborationService for collaboration requests by user. UserId: {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);
 
            var response = await _httpClient.GetAsync(
                $"/api/collaboration-requests/user/{userId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CollaborationService returned error for user requests. UserId: {UserId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    userId, response.StatusCode, correlationId);
                return new List<AggregatorCollaborationRequestDto>();
            }
 
            var requests = await response.Content
                .ReadFromJsonAsync<List<AggregatorCollaborationRequestDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "CollaborationService responded with user requests. UserId: {UserId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, requests?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return requests ?? new List<AggregatorCollaborationRequestDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling CollaborationService for user requests. UserId: {UserId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorCollaborationRequestDto>();
        }
    }
 
    /// <summary>
    /// GET api/collaboration-requests/team/{teamId}
    /// </summary>
    public async Task<List<AggregatorCollaborationRequestDto>> GetCollaborationRequestsByTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling CollaborationService for collaboration requests by team. TeamId: {TeamId}, CorrelationId: {CorrelationId}",
                teamId, correlationId);
 
            var response = await _httpClient.GetAsync(
                $"/api/collaboration-requests/team/{teamId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CollaborationService returned error for team requests. TeamId: {TeamId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    teamId, response.StatusCode, correlationId);
                return new List<AggregatorCollaborationRequestDto>();
            }
 
            var requests = await response.Content
                .ReadFromJsonAsync<List<AggregatorCollaborationRequestDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "CollaborationService responded with team requests. TeamId: {TeamId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, requests?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return requests ?? new List<AggregatorCollaborationRequestDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling CollaborationService for team requests. TeamId: {TeamId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorCollaborationRequestDto>();
        }
    }
 
    // -------------------------------------------------------------------------
    // GROUP INVITATIONS  →  GroupInvitationController: api/group-invitations
    // -------------------------------------------------------------------------
 
    /// <summary>
    /// GET api/group-invitations/user/{userId}
    /// </summary>
    public async Task<List<AggregatorGroupInvitationDto>> GetGroupInvitationsByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling CollaborationService for group invitations by user. UserId: {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);
 
            var response = await _httpClient.GetAsync(
                $"/api/group-invitations/user/{userId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CollaborationService returned error for user invitations. UserId: {UserId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    userId, response.StatusCode, correlationId);
                return new List<AggregatorGroupInvitationDto>();
            }
 
            var invitations = await response.Content
                .ReadFromJsonAsync<List<AggregatorGroupInvitationDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "CollaborationService responded with user invitations. UserId: {UserId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, invitations?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return invitations ?? new List<AggregatorGroupInvitationDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling CollaborationService for user invitations. UserId: {UserId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorGroupInvitationDto>();
        }
    }
 
    /// <summary>
    /// GET api/group-invitations/team/{teamId}
    /// </summary>
    public async Task<List<AggregatorGroupInvitationDto>> GetGroupInvitationsByTeamAsync(
        string teamId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId();
 
        try
        {
            _logger.LogInformation(
                "Calling CollaborationService for group invitations by team. TeamId: {TeamId}, CorrelationId: {CorrelationId}",
                teamId, correlationId);
 
            var response = await _httpClient.GetAsync(
                $"/api/group-invitations/team/{teamId}", cancellationToken);
 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CollaborationService returned error for team invitations. TeamId: {TeamId}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    teamId, response.StatusCode, correlationId);
                return new List<AggregatorGroupInvitationDto>();
            }
 
            var invitations = await response.Content
                .ReadFromJsonAsync<List<AggregatorGroupInvitationDto>>(JsonOptions, cancellationToken);
            stopwatch.Stop();
 
            _logger.LogInformation(
                "CollaborationService responded with team invitations. TeamId: {TeamId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, invitations?.Count ?? 0, stopwatch.ElapsedMilliseconds, correlationId);
 
            return invitations ?? new List<AggregatorGroupInvitationDto>();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error calling CollaborationService for team invitations. TeamId: {TeamId}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                teamId, stopwatch.ElapsedMilliseconds, correlationId);
            return new List<AggregatorGroupInvitationDto>();
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