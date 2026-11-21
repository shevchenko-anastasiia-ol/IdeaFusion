using Collaboration.Domain.Entities;

namespace Collaboration.Application.DTOs;

/// <summary>
/// A direct conversation as one participant sees it. Everything the client needs to draw
/// the request banner and the composer is precomputed here, so the rules live on the server.
/// </summary>
public record DirectConversationDto(
    string Id,
    string OtherUserId,
    string OtherUsername,
    string? OtherAvatarUrl,
    string Status,
    bool RequestedByMe,
    bool BlockedByMe,
    bool CanSend,
    bool CanAccept,
    bool CanUnblock,
    int PendingMessagesLeft,
    string? LastMessage,
    string? LastSenderId,
    DateTime? LastMessageAt,
    DateTime CreatedAt)
{
    public static DirectConversationDto For(DirectConversation c, string viewerId)
    {
        var other = c.Other(viewerId);
        var pending = c.Status == DirectConversationStatus.Pending;
        return new DirectConversationDto(
            c.Id,
            other.UserId,
            other.Username,
            other.AvatarUrl,
            c.Status.ToString().ToLowerInvariant(),
            c.RequestedBy == viewerId,
            c.BlockedBy == viewerId,
            c.CanSend(viewerId),
            pending && c.RequestedBy != viewerId,
            c.Status == DirectConversationStatus.Blocked && c.BlockedBy == viewerId,
            pending && c.RequestedBy == viewerId ? DirectConversation.MaxPendingMessages - c.PendingMessageCount : 0,
            c.LastMessagePreview,
            c.LastSenderId,
            c.LastMessageAt,
            c.CreatedAt);
    }
}
