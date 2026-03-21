namespace ContentDomain.Entity;

public class Comment
{
    public int CommentId { get; set; }
    public int PostId { get; set; }
    public int PostAuthorId { get; set; }
    public int? ParentCommentId { get; set; }
 
    public string Body { get; set; } = null!;
 
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public byte[] RowVer { get; set; } = null!;
 
    public Post Post { get; set; } = null!;
    public PostAuthor Author { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}