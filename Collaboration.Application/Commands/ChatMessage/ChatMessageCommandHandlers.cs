using Collaboration.Application.Interfaces.Commands;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;
using Collaboration.Domain.ValueOfObjects;
using MediatR;

namespace Collaboration.Application.Commands.ChatMessage;

public class SendChatMessageCommandHandler
    : ICommandHandler<SendChatMessageCommand, Domain.Entities.ChatMessage>
{
    private readonly IChatMessageRepository _messageRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public SendChatMessageCommandHandler(
        IChatMessageRepository messageRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _messageRepository = messageRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<Domain.Entities.ChatMessage> Handle(
        SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.ChatId, cancellationToken)
            ?? throw new NotFoundException($"Team '{request.ChatId}' not found.");

        if (team.IsDeleted)
            throw new DomainException("Team is deleted.");

        var isMember = await _teamRepository.IsUserMemberAsync(
            request.ChatId, _caller.UserId, cancellationToken);
        if (!isMember)
            throw new ForbiddenException("Only team members can send messages.");

        var attachments = (request.Attachments ?? [])
            .Select(a => new ChatAttachment(a.Url, a.Name, a.ContentType, a.Size));

        var message = new Domain.Entities.ChatMessage(
            request.ChatId, _caller.ToSnapshot(), request.Text, attachments);

        await _messageRepository.CreateAsync(message, cancellationToken);
        return message;
    }
}