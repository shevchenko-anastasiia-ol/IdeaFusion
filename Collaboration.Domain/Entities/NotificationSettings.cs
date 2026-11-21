using Collaboration.Domain.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Entities;

/// <summary>Which kinds of email a person agreed to. Everything is on until they switch it off.</summary>
public enum NotificationKind
{
    /// <summary>Team invitations, and answers to the ones you sent.</summary>
    Invitations,
    /// <summary>Join requests and vacancy applications, both ways.</summary>
    Requests,
    /// <summary>Direct message requests and new messages while you are away.</summary>
    Messages,
    /// <summary>Comments on your posts and replies to your comments (sent by ContentService).</summary>
    Comments
}

[BsonCollection("NotificationSettings")]
public class NotificationSettings : BaseEntity
{
    [BsonElement("userId")]
    public string UserId { get; private set; } = string.Empty;

    [BsonElement("emailInvitations")]
    public bool EmailInvitations { get; private set; } = true;

    [BsonElement("emailRequests")]
    public bool EmailRequests { get; private set; } = true;

    [BsonElement("emailMessages")]
    public bool EmailMessages { get; private set; } = true;

    [BsonElement("emailComments")]
    public bool EmailComments { get; private set; } = true;

    [BsonConstructor]
    private NotificationSettings() { }

    public NotificationSettings(string userId)
    {
        UserId = userId;
        MarkAsCreated(userId);
    }

    public void Update(bool invitations, bool requests, bool messages, string userId)
        => Update(invitations, requests, messages, EmailComments, userId);

    public void Update(bool invitations, bool requests, bool messages, bool comments, string userId)
    {
        EmailInvitations = invitations;
        EmailRequests = requests;
        EmailMessages = messages;
        EmailComments = comments;
        MarkAsUpdated(userId);
    }

    public bool Allows(NotificationKind kind) => kind switch
    {
        NotificationKind.Invitations => EmailInvitations,
        NotificationKind.Requests => EmailRequests,
        NotificationKind.Messages => EmailMessages,
        NotificationKind.Comments => EmailComments,
        _ => true,
    };
}
