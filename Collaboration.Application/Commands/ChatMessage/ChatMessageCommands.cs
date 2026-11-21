using Collaboration.Application.Interfaces.Commands;

namespace Collaboration.Application.Commands.ChatMessage;

// SenderId, SenderName and SenderAvatarUrl are gone from this command. The sender is
// whoever holds the token; naming the sender in a body is how impersonation happens.

public class SendChatMessageCommand : ICommand<Domain.Entities.ChatMessage>
{
    public string ChatId { get; init; } = default!;

    /// <summary>May be empty when the message carries attachments.</summary>
    public string? Text { get; init; }

    /// <summary>Files already uploaded through POST /api/media/upload.</summary>
    public List<ChatAttachmentInput> Attachments { get; init; } = [];
}

public class ChatAttachmentInput
{
    public string Url { get; init; } = default!;
    public string? Name { get; init; }
    public string? ContentType { get; init; }
    public long Size { get; init; }
}