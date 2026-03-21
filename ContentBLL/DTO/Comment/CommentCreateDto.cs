namespace ContentBLL.DTO.Comment;

public class CommentCreateDto
{
    public int PostId { get; set; }
    public int PostAuthorId { get; set; }
    public int? ParentCommentId { get; set; }
 
    public string Body { get; set; } = null!;
 
    public string CreatedBy { get; set; } = null!;
}