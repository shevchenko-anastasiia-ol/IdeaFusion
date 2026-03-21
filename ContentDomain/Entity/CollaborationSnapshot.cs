namespace ContentDomain.Entity;

public class CollaborationSnapshot
{
    public int CollaborationSnapshotId { get; set; }
 
    /// <summary>Id колаборації з CollaborationService</summary>
    public int CollaborationId { get; set; }
 
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
 
    /// <summary>Синхронізовано з CollaborationService останній раз</summary>
    public DateTime SyncedAt { get; set; }
 
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}