using ContentBLL.DTO.Post;

namespace ContentBLL.DTO.Comment;

public class CommentDto
{
    public int CommentId { get; set; }
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
 
    public string Body { get; set; } = null!;
 
    public AuthorDto Author { get; set; } = null!;
 
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}