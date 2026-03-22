using Collaboration.Domain.Common;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Entities;

[BsonCollection("TeamPosts")]
public class TeamPost : BaseEntity
{
    // ID поста з ContentService
    [BsonElement("postId")]
    public string PostId { get; private set; } = string.Empty;
 
    [BsonElement("teamId")]
    public string TeamId { get; private set; } = string.Empty;
 
    // Денормалізований snapshot автора з UserService
    [BsonElement("author")]
    public UserSnapshot Author { get; private set; } = null!;
 
    // Денормалізована назва поста щоб не ходити в ContentService
    [BsonElement("title")]
    public string Title { get; private set; } = string.Empty;
 
    [BsonElement("publishedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime PublishedAt { get; private set; }
 
    private TeamPost() { }
 
    public TeamPost(string postId, string teamId, UserSnapshot author, string title)
    {
        if (string.IsNullOrWhiteSpace(postId))
            throw new DomainException("PostId cannot be empty.");
        if (string.IsNullOrWhiteSpace(teamId))
            throw new DomainException("TeamId cannot be empty.");
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty.");
        if (title.Length > 200)
            throw new DomainException("Title cannot exceed 200 characters.");
 
        PostId = postId;
        TeamId = teamId;
        Author = author ?? throw new DomainException("Author cannot be null.");
        Title = title.Trim();
        PublishedAt = DateTime.UtcNow;
        MarkAsCreated(author.UserId);
    }
 
    public void UpdateTitle(string title, string userId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty.");
        if (title.Length > 200)
            throw new DomainException("Title cannot exceed 200 characters.");
        Title = title.Trim();
        MarkAsUpdated(userId);
    }
 
    public void UpdateAuthorSnapshot(UserSnapshot updatedAuthor)
    {
        Author = updatedAuthor ?? throw new DomainException("Author cannot be null.");
        MarkAsUpdated(updatedAuthor.UserId);
    }
}