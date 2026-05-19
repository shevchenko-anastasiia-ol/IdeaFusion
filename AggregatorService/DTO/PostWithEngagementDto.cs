using System.Text.Json.Serialization;

namespace AggregatorService.DTO;

/// <summary>
/// Aggregated DTO combining post data from ContentService with engagement metrics (likes, comments, views).
/// Designed for feed rendering — counts only, no full comments list to keep payload light.
/// </summary>
public class PostWithEngagementDto
{
    [JsonPropertyName("post")]
    public AggregatorPostDto Post { get; set; } = null!;
 
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

    [JsonPropertyName("savedCount")]
    public int SavedCount { get; set; }

    public bool IsValid() => Post != null;
}
