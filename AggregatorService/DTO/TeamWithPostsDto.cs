using System.Text.Json.Serialization;

namespace AggregatorService.DTO;

/// <summary>
/// Aggregated DTO combining team data from CollaborationService with its team posts.
/// Used on the team profile page.
/// </summary>
public class TeamWithPostsDto
{
    [JsonPropertyName("team")]
    public AggregatorTeamDto Team { get; set; } = null!;
 
    [JsonPropertyName("posts")]
    public List<AggregatorTeamPostDto> Posts { get; set; } = new();
 
    [JsonPropertyName("totalPosts")]
    public int TotalPosts => Posts?.Count ?? 0;
 
    [JsonPropertyName("openRolesCount")]
    public int OpenRolesCount => Team?.RequiredRoles?.Count ?? 0;
 
    public bool IsValid() => Team != null;
}
