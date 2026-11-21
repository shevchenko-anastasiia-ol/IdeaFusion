using Collaboration.Application.DTOs;
using Collaboration.Application.Interfaces.Queries;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Exceptions;
using Collaboration.Domain.Interfaces;

namespace Collaboration.Application.Queries.ChatMessage;

public class GetChatHistoryQueryHandler
    : IQueryHandler<GetChatHistoryQuery, PagedList<Domain.Entities.ChatMessage>>
{
    private readonly IChatMessageRepository _messageRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ICallerContext _caller;

    public GetChatHistoryQueryHandler(
        IChatMessageRepository messageRepository,
        ITeamRepository teamRepository,
        ICallerContext caller)
    {
        _messageRepository = messageRepository;
        _teamRepository = teamRepository;
        _caller = caller;
    }

    public async Task<PagedList<Domain.Entities.ChatMessage>> Handle(
        GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.ChatId, cancellationToken)
            ?? throw new NotFoundException($"Team '{request.ChatId}' not found.");

        if (team.IsDeleted)
            throw new NotFoundException($"Team '{request.ChatId}' not found.");

        // Membership is checked against the token, not against a parameter, so history
        // of somebody else's chat cannot be read by naming their id.
        var isMember = await _teamRepository.IsUserMemberAsync(
            request.ChatId, _caller.UserId, cancellationToken);
        if (!isMember)
            throw new ForbiddenException("Only team members can view the chat.");

        return await _messageRepository.GetByChatCursorAsync(
            request.ChatId, request.BeforeMessageId, request.PageSize, cancellationToken);
    }
}

public class GetMyChatsQueryHandler
    : IQueryHandler<GetMyChatsQuery, IEnumerable<ChatSummaryDto>>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ICallerContext _caller;

    public GetMyChatsQueryHandler(
        ITeamRepository teamRepository,
        IChatMessageRepository messageRepository,
        ICallerContext caller)
    {
        _teamRepository = teamRepository;
        _messageRepository = messageRepository;
        _caller = caller;
    }

    public async Task<IEnumerable<ChatSummaryDto>> Handle(
        GetMyChatsQuery request, CancellationToken cancellationToken)
    {
        var teams = (await _teamRepository.GetByMemberIdAsync(_caller.UserId, cancellationToken))
            .Where(t => !t.IsDeleted)
            .ToList();

        if (teams.Count == 0)
            return [];

        var latest = await _messageRepository.GetLatestPerChatAsync(
            teams.Select(t => t.Id), cancellationToken);

        var latestByChat = latest.ToDictionary(m => m.ChatId);

        return teams
            .Select(team =>
            {
                latestByChat.TryGetValue(team.Id, out var last);
                return new ChatSummaryDto(
                    ChatId: team.Id,
                    TeamId: team.Id,
                    TeamName: team.Name,
                    TeamAvatarUrl: team.AvatarUrl,
                    MemberCount: team.Members?.Count ?? 0,
                    LastMessage: last?.Preview(),
                    LastSenderName: last?.Sender.Username,
                    LastMessageAt: last?.CreatedAt,
                    CreatedAt: team.CreatedAt);
            })
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToList();
    }
}