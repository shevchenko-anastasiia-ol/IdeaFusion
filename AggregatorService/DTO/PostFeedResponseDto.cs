using System.Text.Json.Serialization;

namespace AggregatorService.DTO;

/// <summary>
/// Response DTO for paginated feed of posts with engagement.
/// </summary>
public class PostFeedResponseDto
{
    [JsonPropertyName("posts")]
    public List<PostWithEngagementDto> Posts { get; set; } = new();
 
    [JsonPropertyName("totalPosts")]
    public int TotalPosts => Posts?.Count ?? 0;
 
    [JsonPropertyName("page")]
    public int Page { get; set; }
 
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
 
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}