using Collaboration.Domain.Common;
using Collaboration.Domain.Exceptions;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Entities;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Expired,
    Revoked
}
 
[BsonCollection("GroupInvitations")]
public class GroupInvitation : BaseEntity
{
    [BsonElement("teamId")]
    public string TeamId { get; private set; } = string.Empty;
 
    [BsonElement("invitedUserId")]
    public string InvitedUserId { get; private set; } = string.Empty;
 
    [BsonElement("invitedByUserId")]
    public string InvitedByUserId { get; private set; } = string.Empty;
 
    [BsonElement("role")]
    public string Role { get; private set; } = string.Empty;
 
    [BsonElement("message")]
    [BsonIgnoreIfNull]
    public string? Message { get; private set; }
 
    [BsonElement("status")]
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;
 
    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; private set; }
 
    [BsonElement("resolvedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ResolvedAt { get; private set; }
 
    [BsonConstructor]
    private GroupInvitation() { }
 
    public GroupInvitation(string teamId, string invitedUserId, string invitedByUserId, string role, string? message, int expirationDays = 7)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            throw new DomainException("TeamId cannot be empty.");
        if (string.IsNullOrWhiteSpace(invitedUserId))
            throw new DomainException("InvitedUserId cannot be empty.");
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Role cannot be empty.");
        if (expirationDays <= 0)
            throw new DomainException("Expiration days must be greater than 0.");
 
        TeamId = teamId;
        InvitedUserId = invitedUserId;
        InvitedByUserId = invitedByUserId;
        Role = role.Trim();
        Message = message?.Trim();
        ExpiresAt = DateTime.UtcNow.AddDays(expirationDays);
        MarkAsCreated(invitedByUserId);
    }
 
    public void Accept(string userId)
    {
        EnsurePending();
        EnsureNotExpired();
        Status = InvitationStatus.Accepted;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated(userId);
    }
 
    public void Decline(string userId)
    {
        EnsurePending();
        EnsureNotExpired();
        Status = InvitationStatus.Declined;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated(userId);
    }
 
    public void Revoke(string userId)
    {
        EnsurePending();
        Status = InvitationStatus.Revoked;
        ResolvedAt = DateTime.UtcNow;
        MarkAsUpdated(userId);
    }
 
    public void MarkAsExpired()
    {
        if (Status == InvitationStatus.Pending && DateTime.UtcNow > ExpiresAt)
        {
            Status = InvitationStatus.Expired;
            ResolvedAt = DateTime.UtcNow;
        }
    }
 
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
 
    private void EnsurePending()
    {
        if (Status != InvitationStatus.Pending)
            throw new DomainException($"Invitation is already {Status}.");
    }
 
    private void EnsureNotExpired()
    {
        if (IsExpired())
            throw new DomainException("Invitation has expired.");
    }
}
