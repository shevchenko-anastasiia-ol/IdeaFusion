using Microsoft.AspNetCore.Http;

namespace ContentBLL.DTO.Post;

public class PostCreateDto
{
    public int? PostAuthorId { get; set; }
    public int? CollaborationSnapshotId { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ExternalLink { get; set; }

    // Файли, які користувач передає при створенні поста
    public List<IFormFile> MediaFiles { get; set; } = new();

    // Ідентифікатори тегів
    public List<int> TagIds { get; set; } = new();

    public string CreatedBy { get; set; } = null!;
}