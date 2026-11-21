using Collaboration.Application.Interfaces.Commands;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;

namespace Collaboration.Application.Commands.DirectMessage;

public class SendDirectMessageCommandHandler : ICommandHandler<SendDirectMessageCommand, DirectMessageSent>
{
    private readonly IDirectConversationRepository _conversations;
    private readonly IChatMessageRepository _messages;
    private readonly IUserSnapshotService _users;
    private readonly ICallerContext _caller;

    public SendDirectMessageCommandHandler(
        IDirectConversationRepository conversations,
        IChatMessageRepository messages,
        IUserSnapshotService users,
        ICallerContext caller)
    {
        _conversations = conversations;
        _messages = messages;
        _users = users;
        _caller = caller;
    }

    public async Task<DirectMessageSent> Handle(SendDirectMessageCommand request, CancellationToken cancellationToken)
    {
        var me = _caller.UserId;
        DirectConversation? conversation;
        var isNew = false;

        if (!string.IsNullOrWhiteSpace(request.ConversationId))
        {
            conversation = await _conversations.GetByIdAsync(request.ConversationId, cancellationToken);
            if (conversation is null || conversation.IsDeleted || !conversation.IsParticipant(me))
                throw new NotFoundException("Conversation not found.");
        }
        else
        {
            var recipientId = request.RecipientId?.Trim();
            if (string.IsNullOrEmpty(recipientId) || !Guid.TryParse(recipientId, out _))
                throw new DomainException("Recipient is required.");
            if (recipientId == me)
                throw new DomainException("You cannot write to yourself.");

            conversation = await _conversations.GetByPairAsync(me, recipientId, cancellationToken);
            if (conversation is null)
            {
                // The recipient's name and avatar come from Identity, never from the body.
                var (username, avatar) = await _users.GetUserSnapshotDataAsync(recipientId, cancellationToken);
                conversation = new DirectConversation(_caller.ToSnapshot(), new UserSnapshot(recipientId, username, avatar));
                isNew = true;
            }
        }

        var attachments = (request.Attachments ?? [])
            .Select(a => new ChatAttachment(a.Url, a.Name, a.ContentType, a.Size));
        var message = new Domain.Entities.ChatMessage(conversation.Id, _caller.ToSnapshot(), request.Text, attachments);

        // Throws when the chat is blocked or the request is still waiting for an answer.
        conversation.RecordMessage(message);
        if (!isNew)
            await DirectSnapshots.ResolveAsync(conversation, _users, cancellationToken);

        if (isNew)
            await _conversations.CreateAsync(conversation, cancellationToken);
        else
            await _conversations.UpdateAsync(conversation, cancellationToken);
        await _messages.CreateAsync(message, cancellationToken);

        return new DirectMessageSent(conversation, message);
    }
}

public class ChangeDirectConversationCommandHandler : ICommandHandler<ChangeDirectConversationCommand, DirectConversation>
{
    private readonly IDirectConversationRepository _conversations;
    private readonly ICallerContext _caller;

    public ChangeDirectConversationCommandHandler(IDirectConversationRepository conversations, ICallerContext caller)
    {
        _conversations = conversations;
        _caller = caller;
    }

    public async Task<DirectConversation> Handle(ChangeDirectConversationCommand request, CancellationToken cancellationToken)
    {
        var me = _caller.UserId;
        var conversation = await _conversations.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null || conversation.IsDeleted || !conversation.IsParticipant(me))
            throw new NotFoundException("Conversation not found.");

        switch (request.Action)
        {
            case DirectConversationAction.Accept: conversation.Accept(me); break;
            case DirectConversationAction.Decline: conversation.Decline(me); break;
            case DirectConversationAction.Block: conversation.Block(me); break;
            case DirectConversationAction.Unblock: conversation.Unblock(me); break;
            default: throw new DomainException("Unknown action.");
        }

        await _conversations.UpdateAsync(conversation, cancellationToken);
        return conversation;
    }
}

public class BlockDirectUserCommandHandler : ICommandHandler<BlockDirectUserCommand, DirectConversation>
{
    private readonly IDirectConversationRepository _conversations;
    private readonly IUserSnapshotService _users;
    private readonly ICallerContext _caller;

    public BlockDirectUserCommandHandler(
        IDirectConversationRepository conversations, IUserSnapshotService users, ICallerContext caller)
    {
        _conversations = conversations;
        _users = users;
        _caller = caller;
    }

    public async Task<DirectConversation> Handle(BlockDirectUserCommand request, CancellationToken cancellationToken)
    {
        var me = _caller.UserId;
        var other = request.UserId?.Trim();
        if (string.IsNullOrEmpty(other) || !Guid.TryParse(other, out _) || other == me)
            throw new DomainException("A valid user is required.");

        var conversation = await _conversations.GetByPairAsync(me, other, cancellationToken);
        if (conversation is null)
        {
            var (username, avatar) = await _users.GetUserSnapshotDataAsync(other, cancellationToken);
            conversation = new DirectConversation(_caller.ToSnapshot(), new UserSnapshot(other, username, avatar));
            conversation.Block(me);
            await _conversations.CreateAsync(conversation, cancellationToken);
            return conversation;
        }

        conversation.Block(me);
        await _conversations.UpdateAsync(conversation, cancellationToken);
        return conversation;
    }
}
