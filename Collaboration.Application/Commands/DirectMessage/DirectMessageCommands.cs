using Collaboration.Application.Commands.ChatMessage;
using Collaboration.Application.Interfaces.Commands;
using Collaboration.Domain.Entities;

namespace Collaboration.Application.Commands.DirectMessage;

/// <summary>What a sent direct message changed: the message itself and the conversation it went to.</summary>
public sealed record DirectMessageSent(DirectConversation Conversation, Domain.Entities.ChatMessage Message);

/// <summary>
/// Sends a direct message. Either <see cref="RecipientId"/> (the first message to somebody
/// opens a request) or <see cref="ConversationId"/> (an existing chat) is given. The sender
/// is always the token.
/// </summary>
public class SendDirectMessageCommand : ICommand<DirectMessageSent>
{
    public string? RecipientId { get; init; }
    public string? ConversationId { get; init; }
    public string? Text { get; init; }
    public List<ChatAttachmentInput> Attachments { get; init; } = [];
}

public enum DirectConversationAction
{
    Accept,
    Decline,
    Block,
    Unblock
}

public class ChangeDirectConversationCommand : ICommand<DirectConversation>
{
    public string ConversationId { get; init; } = default!;
    public DirectConversationAction Action { get; init; }
}

/// <summary>Blocks somebody straight from their profile, even if you have never written to each other.</summary>
public class BlockDirectUserCommand : ICommand<DirectConversation>
{
    public string UserId { get; init; } = default!;
}
