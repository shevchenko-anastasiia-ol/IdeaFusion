namespace AggregatorService.DTO;

public class AggregatorCollaborationDto
{
    public int CollaborationSnapshotId { get; set; }
    public int CollaborationId { get; set; }
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    /// <summary>MongoDB team ID from CollaborationService</summary>
    public string? ExternalId { get; set; }
}