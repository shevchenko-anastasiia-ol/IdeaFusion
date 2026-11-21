namespace AggregatorService.DTO;

public class AggregatorSavedPostDto
{
    public int SavedPostId { get; set; }
    public int UserId { get; set; }
    public DateTime SavedAt { get; set; }
 
    public int PostId { get; set; }
    public string PostTitle { get; set; } = null!;
    /// <summary>All media of the saved post, as ContentService returns them.</summary>
    public List<string> PostMediaUrls { get; set; } = new();

    /// <summary>The cover image: the first media URL.</summary>
    public string? PostMediaUrl => PostMediaUrls.FirstOrDefault();
}