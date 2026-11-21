namespace ContentBLL.DTO.Post;

/// <summary>
/// The caller's own content-side identity. Returned by the endpoints that answer
/// "which author am I", never taken from a request body.
/// </summary>
public class PostAuthorDto
{
    public int PostAuthorId { get; set; }

    public int UserId { get; set; }

    /// <summary>The Keycloak subject. Null for the demo rows seeded before the realm.</summary>
    public Guid? ExternalId { get; set; }

    public string UserName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    /// <summary>
    /// True when this call created the row. Lets a client tell "I am a brand new
    /// author" from "I already had a row that has just been refreshed".
    /// </summary>
    public bool Created { get; set; }
}
