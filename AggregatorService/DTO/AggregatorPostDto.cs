namespace AggregatorService.DTO;

public class AggregatorPostDto
{
    public int PostId { get; set; }
 
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ExternalLink { get; set; }
    public string Status { get; set; } = null!;
 
    public AggregatorAuthorDto? Author { get; set; }
    public AggregatorCollaborationDto? Collaboration { get; set; }
 
    public List<string> Tags { get; set; } = new();
    public List<string> MediaUrls { get; set; } = new();
 
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsSavedByCurrentUser { get; set; }
 
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}