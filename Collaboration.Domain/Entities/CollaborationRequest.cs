using Collaboration.Domain.Common;
using Collaboration.Domain.Exceptions;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Entities;

public enum CollaborationRequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled
}
 
[BsonCollection("CollaborationRequests")]
public class CollaborationRequest : BaseEntity
{
    [BsonElement("teamId")]
    public string TeamId { get; private set; } = string.Empty;
 
    [BsonElement("fromUserId")]
    public string FromUserId { get; private set; } = string.Empty;

    [BsonElement("fromUsername")]
    [BsonIgnoreIfNull]
    public string? FromUsername { get; private set; }

    [BsonElement("fromAvatarUrl")]
    [BsonIgnoreIfNull]
    public string? FromAvatarUrl { get; private set; }

    [BsonElement("toUserId")]
    [BsonIgnoreIfNull]
    public string? ToUserId { get; private set; }
 
    [BsonElement("role")]
    public string Role { get; private set; } = string.Empty;
 
    [BsonElement("message")]
    [BsonIgnoreIfNull]
    public string? Message { get; private set; }
 
    [BsonElement("status")]
    public CollaborationRequestStatus Status { get; private set; } = CollaborationRequestStatus.Pending;
 
    [BsonElement("resolvedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ResolvedAt { get; private set; }
 
    [BsonConstructor]
    private CollaborationRequest() { }
 
    public CollaborationRequest(string teamId, string fromUserId, string role, string? message,
        string? toUserId = null, string? fromUsername = null, string? fromAvatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            throw new DomainException("TeamId cannot be empty.");
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Role cannot be empty.");

        TeamId = teamId;
        FromUserId = fromUserId;
        FromUsername = string.IsNullOrWhiteSpace(fromUsername) ? null : fromUsername.Trim();
        FromAvatarUrl = string.IsNullOrWhiteSpace(fromAvatarUrl) ? null : fromAvatarUrl.Trim();
        ToUserId = toUserId;
        Role = role.Trim();
        Message = message?.Trim();
        MarkAsCreated(fromUserId);
    }
 
    public void Accept(string userId)
    {
        EnsurePending();
        Status = CollaborationRequestStatus.Accepted;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated(userId);
    }
 
    public void Reject(string userId)
    {
        EnsurePending();
        Status = CollaborationRequestStatus.Rejected;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated(userId);
    }
 
    public void Cancel(string userId)
    {
        EnsurePending();
        Status = CollaborationRequestStatus.Cancelled;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated(userId);
    }
 
    private void EnsurePending()
    {
        if (Status != CollaborationRequestStatus.Pending)
            throw new DomainException($"Request is already {Status}.");
    }
}