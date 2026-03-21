namespace ContentDomain.Entity;

public class Post
{
    public int PostId { get; set; }
    
    public int? PostAuthorId { get; set; }
    public int? CollaborationSnapshotId { get; set; }
 
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? MediaUrl { get; set; }
    public string? ExternalLink { get; set; }
 
    public PostStatus Status { get; set; } = PostStatus.Published;
 
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public byte[] RowVer { get; set; } = null!;
 
    public PostAuthor? Author { get; set; }
    public CollaborationSnapshot? Collaboration { get; set; }
    
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<PostView> Views { get; set; } = new List<PostView>();
    public ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
}

public enum PostStatus
{
    Published,
    Archived,
    Draft
}