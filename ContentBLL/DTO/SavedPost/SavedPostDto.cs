namespace ContentBLL.DTO.SavedPost;

public class SavedPostDto
{
    public int SavedPostId { get; set; }
    public int UserId { get; set; }
    public DateTime SavedAt { get; set; }
 
    public int PostId { get; set; }
    public string PostTitle { get; set; } = null!;
    public string? PostMediaUrl { get; set; }
}