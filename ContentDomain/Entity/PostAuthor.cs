namespace ContentDomain.Entity;

public class PostAuthor
{
    public int PostAuthorId { get; set; }
 
    /// <summary>Id користувача з UserService</summary>
    public int UserId { get; set; }
 
    public string UserName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
 
    /// <summary>Синхронізовано з UserService останній раз</summary>
    public DateTime SyncedAt { get; set; }
 
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}