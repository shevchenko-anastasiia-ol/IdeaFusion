using Microsoft.AspNetCore.Http;

namespace ContentBLL.DTO.Post;

/// <summary>
/// The author of a new post is whoever holds the token, so there is no author id or
/// author name to accept here. Collaboration posts still name their team, and the
/// database constraint requires exactly one of the two.
/// </summary>
public class PostCreateDto
{
    public int? CollaborationSnapshotId { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ExternalLink { get; set; }

    // Файли, які користувач передає при створенні поста
    public List<IFormFile> MediaFiles { get; set; } = new();

    // Ідентифікатори тегів
    public List<int> TagIds { get; set; } = new();

    /// <summary>
    /// Avatar to cache against the author's row. Purely cosmetic and never used for
    /// identity: the token decides who the author is, and Keycloak tokens carry no
    /// avatar. Safe to drop it on a request that is not changing the avatar.
    /// </summary>
    public string? AuthorAvatarUrl { get; set; }

    // "Draft" або "Published"
    public string? Status { get; set; }
}
