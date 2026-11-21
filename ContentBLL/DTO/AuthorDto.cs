namespace ContentBLL.DTO.Post;

public class AuthorDto
{
    public int PostAuthorId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? AvatarUrl { get; set; }

    /// <summary>The author's Identity id (Keycloak "sub"); what profile links use. Null for seeded demo authors.</summary>
    public string? ExternalId { get; set; }
}