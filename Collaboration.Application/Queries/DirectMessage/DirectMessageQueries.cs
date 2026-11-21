using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;

namespace Collaboration.Application.Queries.DirectMessage;

public class GetMyDirectConversationsQuery : IQuery<List<DirectConversation>>
{
}

/// <summary>The caller's conversation with one person, or null when they have never written.</summary>
public class GetDirectConversationWithUserQuery : IQuery<DirectConversation?>
{
    public string UserId { get; init; } = default!;
}

public class GetDirectHistoryQuery : IQuery<(DirectConversation Conversation, PagedList<Domain.Entities.ChatMessage> Messages)>
{
    public string ConversationId { get; init; } = default!;
    public string? BeforeMessageId { get; init; }
    public int PageSize { get; init; } = 50;
}
