namespace AggregatorService.DTO;

public class AggregatorCommentDto
{
    public int CommentId { get; set; }
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
 
    public string Body { get; set; } = null!;
 
    public AggregatorAuthorDto Author { get; set; } = null!;
 
    public List<AggregatorCommentDto> Replies { get; set; } = new();
 
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}