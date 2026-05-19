using Microsoft.AspNetCore.Http;

namespace ContentBLL.DTO.Post;

public class PostUpdateDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ExternalLink { get; set; }

    // Нові файли для завантаження
    public List<IFormFile>? NewMediaFiles { get; set; }

    // Ідентифікатори тегів
    public List<int> TagIds { get; set; } = new();

    public string UpdatedBy { get; set; } = null!;
}