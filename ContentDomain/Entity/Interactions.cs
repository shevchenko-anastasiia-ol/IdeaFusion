namespace ContentDomain.Entity;

public class Like
{
    public int LikeId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
 
    public DateTime CreatedAt { get; set; }
 
    public Post Post { get; set; } = null!;
}
 
public class PostView
{
    public int PostViewId { get; set; }
    public int PostId { get; set; }
    public int? UserId { get; set; }
 
    public DateTime ViewedAt { get; set; }
 
    public Post Post { get; set; } = null!;
}
 
public class SavedPost
{
    public int SavedPostId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
 
    public DateTime SavedAt { get; set; }
 
    public Post Post { get; set; } = null!;
}