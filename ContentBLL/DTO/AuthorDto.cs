namespace ContentBLL.DTO.Post;

public class AuthorDto
{
    public int PostAuthorId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}