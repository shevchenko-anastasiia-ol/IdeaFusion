using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Common;

public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; protected set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; protected set; }

    [BsonElement("createdBy")]
    public string CreatedBy { get; protected set; } = string.Empty;

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? UpdatedAt { get; protected set; }

    [BsonElement("updatedBy")]
    [BsonIgnoreIfNull]
    public string? UpdatedBy { get; protected set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; protected set; }

    protected BaseEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsCreated(string userId)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedBy = userId ?? throw new ArgumentNullException(nameof(userId));
    }

    public void MarkAsUpdated(string userId)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = userId ?? throw new ArgumentNullException(nameof(userId));
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}