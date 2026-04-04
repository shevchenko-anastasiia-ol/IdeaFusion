using System.Text.Json.Serialization;

namespace AggregatorService.DTO;

/// <summary>
/// Aggregated user dashboard DTO — combines data from both ContentService and CollaborationService.
/// Single request for rendering the user's personal profile/dashboard page.
/// </summary>
public class UserDashboardDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
 
    /// <summary>Posts created by this user (from ContentService).</summary>
    [JsonPropertyName("myPosts")]
    public List<AggregatorPostDto> MyPosts { get; set; } = new();
 
    /// <summary>Posts saved by this user (from ContentService).</summary>
    [JsonPropertyName("savedPosts")]
    public List<AggregatorSavedPostDto> SavedPosts { get; set; } = new();
 
    /// <summary>Teams the user is a member of (from CollaborationService).</summary>
    [JsonPropertyName("myTeams")]
    public List<AggregatorTeamSummaryDto> MyTeams { get; set; } = new();
 
    /// <summary>Pending collaboration requests sent by this user.</summary>
    [JsonPropertyName("pendingRequests")]
    public List<AggregatorCollaborationRequestDto> PendingRequests { get; set; } = new();
 
    /// <summary>Pending group invitations received by this user.</summary>
    [JsonPropertyName("pendingInvitations")]
    public List<AggregatorGroupInvitationDto> PendingInvitations { get; set; } = new();
 
    [JsonPropertyName("totalPosts")]
    public int TotalPosts => MyPosts?.Count ?? 0;
 
    [JsonPropertyName("totalSavedPosts")]
    public int TotalSavedPosts => SavedPosts?.Count ?? 0;
 
    [JsonPropertyName("totalTeams")]
    public int TotalTeams => MyTeams?.Count ?? 0;
 
    public bool IsValid() => UserId > 0;
}