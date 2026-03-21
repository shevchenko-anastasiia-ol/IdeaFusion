namespace ContentBLL.DTO.Post;

public class PostUpdateDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? MediaUrl { get; set; }
    public string? ExternalLink { get; set; }
 
    public List<int> TagIds { get; set; } = new();
 
    public string UpdatedBy { get; set; } = null!;
}