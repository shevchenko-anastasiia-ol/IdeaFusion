namespace ContentBLL.DTO.Post;

public class PostCreateDto
{
    public int? PostAuthorId { get; set; }
    public int? CollaborationSnapshotId { get; set; }
 
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? MediaUrl { get; set; }
    public string? ExternalLink { get; set; }
 
    public List<int> TagIds { get; set; } = new();
 
    public string CreatedBy { get; set; } = null!;
}