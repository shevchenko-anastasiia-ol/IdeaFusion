namespace ContentDomain.Entity;

public class PostMedia
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public string ObjectName { get; set; } = null!; // шлях в MinIO
    public string Bucket { get; set; } = null!;
    public string ContentType { get; set; } = null!; // image/png, video/mp4
}