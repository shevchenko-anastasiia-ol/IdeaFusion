namespace ContentBLL.DTO.Post;

public class CollaborationDto
{
    public int CollaborationSnapshotId { get; set; }
    public int CollaborationId { get; set; }
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? ExternalId { get; set; }
}