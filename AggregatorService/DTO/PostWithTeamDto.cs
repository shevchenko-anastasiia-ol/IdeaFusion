using System.Text.Json.Serialization;

namespace AggregatorService.DTO;

/// <summary>
/// Aggregated DTO combining post data from ContentService with full team details from CollaborationService.
/// Used on the post detail page when the post is linked to a collaboration.
/// </summary>
public class PostWithTeamDto
{
    [JsonPropertyName("post")]
    public AggregatorPostDto Post { get; set; } = null!;
 
    /// <summary>
    /// Full team data — only present if post has a CollaborationSnapshotId.
    /// </summary>
    [JsonPropertyName("team")]
    public AggregatorTeamDto? Team { get; set; }
 
    [JsonPropertyName("isTeamPost")]
    public bool IsTeamPost => Team != null;
 
    public bool IsValid() => Post != null;
}