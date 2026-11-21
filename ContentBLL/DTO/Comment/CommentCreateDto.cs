namespace ContentBLL.DTO.Comment;

public class CommentCreateDto
{
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }

    public string Body { get; set; } = null!;
}
