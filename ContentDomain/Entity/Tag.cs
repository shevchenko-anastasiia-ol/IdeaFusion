namespace ContentDomain.Entity;

public class Tag
{
    public int TagId { get; set; }
    public string Name { get; set; } = null!;
 
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}

public class PostTag
{
    public int PostId { get; set; }
    public int TagId { get; set; }
 
    public Post Post { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}