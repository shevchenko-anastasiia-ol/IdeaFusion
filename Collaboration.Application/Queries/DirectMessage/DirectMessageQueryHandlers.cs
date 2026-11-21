using Collaboration.Application.Commands.DirectMessage;
using Collaboration.Application.Interfaces.Queries;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.DirectMessage;

public class GetMyDirectConversationsQueryHandler : IQueryHandler<GetMyDirectConversationsQuery, List<DirectConversation>>
{
    /// <summary>How many conversations with unresolved names one list request tries to fix.</summary>
    private const int MaxRepairsPerRequest = 5;

    private readonly IDirectConversationRepository _conversations;
    private readonly IUserSnapshotService _users;
    private readonly ICallerContext _caller;

    public GetMyDirectConversationsQueryHandler(
        IDirectConversationRepository conversations, IUserSnapshotService users, ICallerContext caller)
    {
        _conversations = conversations;
        _users = users;
        _caller = caller;
    }

    public async Task<List<DirectConversation>> Handle(GetMyDirectConversationsQuery request, CancellationToken cancellationToken)
    {
        var me = _caller.UserId;
        var all = await _conversations.GetForUserAsync(me, cancellationToken);
        // A block placed from a profile before anyone wrote is the blocker's business only:
        // the other person does not get an empty, blocked chat in their list.
        var visible = all.Where(c => c.LastMessageAt != null || c.BlockedBy == me).ToList();

        // Names stored as ids (Identity was unreachable at the time) are fixed as they are seen,
        // a few per request so that a directory outage cannot make the list slow.
        var broken = visible.Where(c => c.UnresolvedParticipants().Any()).Take(MaxRepairsPerRequest).ToList();
        await Task.WhenAll(broken.Select(async c =>
        {
            if (await DirectSnapshots.ResolveAsync(c, _users, cancellationToken))
                await _conversations.UpdateAsync(c, cancellationToken);
        }));

        return visible;
    }
}

public class GetDirectConversationWithUserQueryHandler : IQueryHandler<GetDirectConversationWithUserQuery, DirectConversation?>
{
    private readonly IDirectConversationRepository _conversations;
    private readonly ICallerContext _caller;

    public GetDirectConversationWithUserQueryHandler(IDirectConversationRepository conversations, ICallerContext caller)
    {
        _conversations = conversations;
        _caller = caller;
    }

    public async Task<DirectConversation?> Handle(GetDirectConversationWithUserQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || request.UserId == _caller.UserId)
            return null;
        return await _conversations.GetByPairAsync(_caller.UserId, request.UserId.Trim(), cancellationToken);
    }
}

public class GetDirectHistoryQueryHandler
    : IQueryHandler<GetDirectHistoryQuery, (DirectConversation Conversation, PagedList<Domain.Entities.ChatMessage> Messages)>
{
    private readonly IDirectConversationRepository _conversations;
    private readonly IChatMessageRepository _messages;
    private readonly IUserSnapshotService _users;
    private readonly ICallerContext _caller;

    public GetDirectHistoryQueryHandler(
        IDirectConversationRepository conversations, IChatMessageRepository messages,
        IUserSnapshotService users, ICallerContext caller)
    {
        _conversations = conversations;
        _messages = messages;
        _users = users;
        _caller = caller;
    }

    public async Task<(DirectConversation Conversation, PagedList<Domain.Entities.ChatMessage> Messages)> Handle(
        GetDirectHistoryQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetByIdAsync(request.ConversationId, cancellationToken);
        // Somebody else's conversation looks exactly like a missing one.
        if (conversation is null || conversation.IsDeleted || !conversation.IsParticipant(_caller.UserId))
            throw new NotFoundException("Conversation not found.");

        if (await DirectSnapshots.ResolveAsync(conversation, _users, cancellationToken))
            await _conversations.UpdateAsync(conversation, cancellationToken);

        var page = await _messages.GetByChatCursorAsync(
            conversation.Id, request.BeforeMessageId, Math.Clamp(request.PageSize, 1, 100), cancellationToken);
        return (conversation, page);
    }
}
