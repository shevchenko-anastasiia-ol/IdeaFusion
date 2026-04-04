namespace AggregatorService.DTO;

/// <summary>
/// Represents a post linked to a team, as returned by CollaborationService.
/// Contains a denormalized author snapshot so AggregatorService doesn't need
/// to call UserService separately for basic author info.
/// </summary>
public class AggregatorTeamPostDto
{
    public string Id { get; set; } = string.Empty;

    /// <summary>The post ID from ContentService.</summary>
    public string PostId { get; set; } = string.Empty;

    public string TeamId { get; set; } = string.Empty;

    /// <summary>Denormalized author snapshot from UserService.</summary>
    public AggregatorUserSnapshotDto Author { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    // Audit fields from BaseEntity
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class AggregatorUserSnapshotDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}