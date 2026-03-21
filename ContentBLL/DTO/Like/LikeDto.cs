namespace ContentBLL.DTO.Like;

public class LikeDto
{
    public int LikeId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}