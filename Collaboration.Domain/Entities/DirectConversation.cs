using Collaboration.Domain.Common;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.ValueOfObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace Collaboration.Domain.Entities;

public enum DirectConversationStatus
{
    /// <summary>Somebody wrote first; the other side has not answered the request yet.</summary>
    Pending,
    Accepted,
    /// <summary>Nobody can write. Only <see cref="DirectConversation.BlockedBy"/> can lift it.</summary>
    Blocked
}

/// <summary>
/// A one-to-one chat between two people. It starts as a request: the person who writes
/// first can send a few messages, the other one accepts (and the chat opens for both) or
/// declines (and the chat is blocked for both). Either side can block an open chat later;
/// only whoever blocked it can open it again. There is one conversation per pair, so a
/// declined request cannot be sent again as a new one.
/// </summary>
[BsonCollection("DirectConversations")]
public class DirectConversation : BaseEntity
{
    /// <summary>How many messages the requester may send before the request is answered.</summary>
    public const int MaxPendingMessages = 3;

    /// <summary>"smallerId|largerId": one document per pair, enforced by a unique index.</summary>
    [BsonElement("pairKey")]
    public string PairKey { get; private set; } = string.Empty;

    [BsonElement("participantIds")]
    public List<string> ParticipantIds { get; private set; } = [];

    [BsonElement("participants")]
    public List<UserSnapshot> Participants { get; private set; } = [];

    [BsonElement("requestedBy")]
    public string RequestedBy { get; private set; } = string.Empty;

    [BsonElement("status")]
    public DirectConversationStatus Status { get; private set; } = DirectConversationStatus.Pending;

    [BsonElement("blockedBy")]
    [BsonIgnoreIfNull]
    public string? BlockedBy { get; private set; }

    [BsonElement("pendingMessageCount")]
    public int PendingMessageCount { get; private set; }

    [BsonElement("lastMessageAt")]
    [BsonIgnoreIfNull]
    public DateTime? LastMessageAt { get; private set; }

    [BsonElement("lastMessagePreview")]
    [BsonIgnoreIfNull]
    public string? LastMessagePreview { get; private set; }

    [BsonElement("lastSenderId")]
    [BsonIgnoreIfNull]
    public string? LastSenderId { get; private set; }

    [BsonConstructor]
    private DirectConversation() { }

    public DirectConversation(UserSnapshot requester, UserSnapshot recipient)
    {
        if (requester is null || recipient is null)
            throw new DomainException("Both participants are required.");
        if (requester.UserId == recipient.UserId)
            throw new DomainException("You cannot write to yourself.");

        PairKey = KeyFor(requester.UserId, recipient.UserId);
        ParticipantIds = [requester.UserId, recipient.UserId];
        Participants = [requester, recipient];
        RequestedBy = requester.UserId;
        Status = DirectConversationStatus.Pending;
        MarkAsCreated(requester.UserId);
    }

    public static string KeyFor(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";

    public bool IsParticipant(string userId) => ParticipantIds.Contains(userId);

    public string OtherId(string userId) => ParticipantIds.First(id => id != userId);

    public UserSnapshot Other(string userId) =>
        Participants.FirstOrDefault(p => p.UserId != userId) ?? new UserSnapshot(OtherId(userId), OtherId(userId));

    /// <summary>
    /// Participants whose name was never resolved (the directory was unreachable when the
    /// chat started, so the id was stored in its place).
    /// </summary>
    public IEnumerable<string> UnresolvedParticipants() =>
        Participants.Where(p => p.Username == p.UserId).Select(p => p.UserId);

    /// <summary>Replaces a participant's stored name and avatar. Returns false when nothing changed.</summary>
    public bool RefreshParticipant(UserSnapshot snapshot)
    {
        var i = Participants.FindIndex(p => p.UserId == snapshot.UserId);
        if (i < 0 || Participants[i].Equals(snapshot))
            return false;
        Participants[i] = snapshot;
        return true;
    }

    /// <summary>The recipient of the request: the one who accepts or declines it.</summary>
    public string Recipient => ParticipantIds.First(id => id != RequestedBy);

    public bool CanSend(string userId) => Status switch
    {
        DirectConversationStatus.Accepted => true,
        DirectConversationStatus.Pending => userId == RequestedBy && PendingMessageCount < MaxPendingMessages,
        _ => false,
    };

    public void EnsureParticipant(string userId)
    {
        if (!IsParticipant(userId))
            throw new ForbiddenException("You are not part of this conversation.");
    }

    /// <summary>Checks the sender may write and keeps the list preview up to date.</summary>
    public void RecordMessage(ChatMessage message)
    {
        var sender = message.Sender.UserId;
        EnsureParticipant(sender);

        switch (Status)
        {
            case DirectConversationStatus.Blocked:
                throw new ForbiddenException("Чат заблоковано, писати в нього не можна.");
            case DirectConversationStatus.Pending when sender != RequestedBy:
                throw new DomainException("Спершу прийміть запит на листування.");
            case DirectConversationStatus.Pending when PendingMessageCount >= MaxPendingMessages:
                throw new DomainException($"Поки запит не прийнято, можна надіслати не більше {MaxPendingMessages} повідомлень.");
            case DirectConversationStatus.Pending:
                PendingMessageCount++;
                break;
        }

        LastMessageAt = message.CreatedAt;
        LastMessagePreview = message.Preview();
        LastSenderId = sender;
        MarkAsUpdated(sender);
    }

    public void Accept(string userId)
    {
        EnsureParticipant(userId);
        if (Status != DirectConversationStatus.Pending)
            throw new DomainException("Запит уже розглянуто.");
        if (userId == RequestedBy)
            throw new ForbiddenException("Прийняти запит може лише той, кому написали.");

        Status = DirectConversationStatus.Accepted;
        MarkAsUpdated(userId);
    }

    /// <summary>Declining is blocking: the chat closes for both, and only the one who declined can reopen it.</summary>
    public void Decline(string userId)
    {
        EnsureParticipant(userId);
        if (Status != DirectConversationStatus.Pending)
            throw new DomainException("Запит уже розглянуто.");
        if (userId == RequestedBy)
            throw new ForbiddenException("Відхилити запит може лише той, кому написали.");

        Status = DirectConversationStatus.Blocked;
        BlockedBy = userId;
        MarkAsUpdated(userId);
    }

    public void Block(string userId)
    {
        EnsureParticipant(userId);
        if (Status == DirectConversationStatus.Blocked)
            throw new DomainException("Чат уже заблоковано.");

        Status = DirectConversationStatus.Blocked;
        BlockedBy = userId;
        MarkAsUpdated(userId);
    }

    public void Unblock(string userId)
    {
        EnsureParticipant(userId);
        if (Status != DirectConversationStatus.Blocked)
            throw new DomainException("Чат не заблоковано.");
        if (BlockedBy != userId)
            throw new ForbiddenException("Розблокувати чат може лише той, хто його заблокував.");

        Status = DirectConversationStatus.Accepted;
        BlockedBy = null;
        MarkAsUpdated(userId);
    }
}
