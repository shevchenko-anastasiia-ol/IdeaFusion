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
 
            var raw = await response.Content.ReadFromJsonAsync<TeamDetailRaw>(JsonOptions, cancellationToken);
            stopwatch.Stop();

            if (raw is null) return null;

            var team = new AggregatorTeamDto
            {
                TeamId       = raw.Id ?? string.Empty,
                Name         = raw.Name ?? string.Empty,
                Description  = raw.Description ?? string.Empty,
                Category     = raw.Category ?? string.Empty,
                Status       = raw.Status ?? string.Empty,
                AvatarUrl    = raw.AvatarUrl,
                Tags         = raw.Tags ?? new List<string>(),
                Members      = raw.Members?.Select(m => new AggregatorTeamMemberDto
                {
                    UserId   = m.User?.UserId   ?? string.Empty,
                    Username = m.User?.Username ?? string.Empty,
                    AvatarUrl = m.User?.AvatarUrl,
                    Role     = m.Role ?? string.Empty,
                }).ToList() ?? new List<AggregatorTeamMemberDto>(),
                RequiredRoles = raw.RequiredRoles?.Select(r => new AggregatorRequiredRoleDto
                {
                    Role        = r.Role ?? string.Empty,
                    Description = r.Description,
                }).ToList() ?? new List<AggregatorRequiredRoleDto>(),
            };

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

            // Collaboration API returns full Team entities (array).
            // Deserialize via the intermediate type that matches the actual JSON shape.
            var raw = await response.Content
                .ReadFromJsonAsync<List<CollaborationTeamRaw>>(JsonOptions, cancellationToken);
            stopwatch.Stop();

            var teams = raw?.Select(t => new AggregatorTeamSummaryDto
            {
                TeamId       = t.Id ?? string.Empty,
                Name         = t.Name ?? string.Empty,
                Category     = t.Category ?? string.Empty,
                Status       = t.Status ?? string.Empty,
                MembersCount = t.Members?.Count ?? 0,
            }).ToList() ?? new List<AggregatorTeamSummaryDto>();

            _logger.LogInformation(
                "CollaborationService responded with member teams. UserId: {UserId}, Count: {Count}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                userId, teams.Count, stopwatch.ElapsedMilliseconds, correlationId);

            return teams;
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

    // Intermediate types matching the actual JSON shape returned by Collaboration API's team endpoints.
    // The API serializes domain Team entities directly, so property names are camelCase C# identifiers.
    private sealed class CollaborationTeamRaw
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public List<object>? Members { get; set; }
    }

    private sealed class TeamDetailRaw
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public List<string>? Tags { get; set; }
        public List<TeamMemberRaw>? Members { get; set; }
        public List<RequiredRoleRaw>? RequiredRoles { get; set; }
        public string? AvatarUrl { get; set; }
    }

    private sealed class TeamMemberRaw
    {
        public UserSnapshotRaw? User { get; set; }
        public string? Role { get; set; }
    }

    private sealed class UserSnapshotRaw
    {
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? AvatarUrl { get; set; }
    }

    private sealed class RequiredRoleRaw
    {
        public string? Role { get; set; }
        public string? Description { get; set; }
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
 
    /// <summary>
    /// Looks up MongoDB TeamPost by content postId, then fetches the team to build an AggregatorCollaborationDto.
    /// Returns null for posts that are not team posts or when any lookup fails.
    /// Used to enrich posts created before the CollaborationSnapshotId column existed.
    /// </summary>
    public async Task<AggregatorCollaborationDto?> GetTeamCollaborationByPostIdAsync(string postId, CancellationToken cancellationToken = default)
    {
        try
        {
            var teamPostResponse = await _httpClient.GetAsync($"/api/team-posts/{postId}", cancellationToken);
            if (!teamPostResponse.IsSuccessStatusCode) return null;

            var teamPost = await teamPostResponse.Content.ReadFromJsonAsync<TeamPostRaw>(JsonOptions, cancellationToken);
            if (teamPost?.TeamId is null) return null;

            var team = await GetTeamByIdAsync(teamPost.TeamId, cancellationToken);
            if (team is null) return null;

            return new AggregatorCollaborationDto
            {
                Name       = team.Name,
                AvatarUrl  = team.AvatarUrl,
                ExternalId = teamPost.TeamId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch team collaboration for postId {PostId}", postId);
            return null;
        }
    }

    private sealed class TeamPostRaw
    {
        public string? PostId { get; set; }
        public string? TeamId { get; set; }
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