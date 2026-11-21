namespace ContentDomain.Entity;

public class PostAuthor
{
    public int PostAuthorId { get; set; }
 
    /// <summary>Id користувача з UserService</summary>
    public int UserId { get; set; }

    /// <summary>
    /// Keycloak subject ("sub") of the author, as a string. Nullable and unique
    /// where present: the rows seeded for demo content predate the realm and store
    /// faker usernames instead, so they keep a null ExternalId and stay unowned.
    /// </summary>
    public string? ExternalId { get; set; }
 
    public string UserName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
 
    /// <summary>Синхронізовано з UserService останній раз</summary>
    public DateTime SyncedAt { get; set; }
 
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}