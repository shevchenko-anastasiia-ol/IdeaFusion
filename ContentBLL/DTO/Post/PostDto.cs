namespace ContentBLL.DTO.Post;

public class PostDto
{
    public int PostId { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ExternalLink { get; set; }
    public string Status { get; set; } = null!;

    public AuthorDto? Author { get; set; }
    public CollaborationDto? Collaboration { get; set; }

    public List<string> Tags { get; set; } = new();

    // Колекція URL медіа
    public List<string> MediaUrls { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}