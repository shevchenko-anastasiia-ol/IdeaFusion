using Collaboration.Domain.Entities;

namespace Collaboration.API.Notifications;

/// <summary>
/// One letter to one person, before their address is known. The address and their settings
/// are looked up when it is sent, off the request that caused it.
/// </summary>
public sealed record EmailNotification(
    string RecipientUserId,
    NotificationKind Kind,
    string Subject,
    string Heading,
    string Text,
    string ActionLabel,
    string ActionPath,
    string? Quote = null)
{
    /// <summary>Resolved while the request (and its token) was live; the sender has no token to ask Identity with.</summary>
    public string? RecipientEmail { get; init; }
    public string? RecipientName { get; init; }
}
