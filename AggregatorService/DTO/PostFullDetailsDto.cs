using System.Text.Json.Serialization;

namespace AggregatorService.DTO;

/// <summary>
/// Fully aggregated post DTO — combines ContentService and CollaborationService data.
/// Designed for the post detail page: post + comments tree + team info + engagement.
/// </summary>
public class PostFullDetailsDto
{
    [JsonPropertyName("post")]
    public AggregatorPostDto Post { get; set; } = null!;
 
    [JsonPropertyName("comments")]
    public List<AggregatorCommentDto> Comments { get; set; } = new();
 
    [JsonPropertyName("team")]
    public AggregatorTeamDto? Team { get; set; }
 
    [JsonPropertyName("likesCount")]
    public int LikesCount { get; set; }
 
    [JsonPropertyName("commentsCount")]
    public int CommentsCount { get; set; }
 
    [JsonPropertyName("viewsCount")]
    public int ViewsCount { get; set; }
 
    [JsonPropertyName("isLikedByCurrentUser")]
    public bool IsLikedByCurrentUser { get; set; }
 
    [JsonPropertyName("isSavedByCurrentUser")]
    public bool IsSavedByCurrentUser { get; set; }
 
    /// <summary>
    /// Validates that team data is consistent with the post's CollaborationSnapshotId.
    /// </summary>
    public bool IsValid()
    {
        if (Post == null) return false;
 
        // If post references a collaboration, team must be present
        if (Post.Collaboration != null && Team == null)
            return false;
 
        return true;
    }
}
