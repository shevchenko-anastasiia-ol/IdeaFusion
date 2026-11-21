using Collaboration.Application.DTOs;
using Collaboration.Application.Interfaces.Queries;
using Collaboration.Domain.Common.Helpers;

namespace Collaboration.Application.Queries.ChatMessage;

// RequesterId and UserId are gone: both named the caller. The caller is the token.

public class GetChatHistoryQuery : IQuery<PagedList<Domain.Entities.ChatMessage>>
{
    public string ChatId { get; init; } = default!;
    public string? BeforeMessageId { get; init; }
    public int PageSize { get; init; } = 50;
}

public class GetMyChatsQuery : IQuery<IEnumerable<ChatSummaryDto>>
{
}