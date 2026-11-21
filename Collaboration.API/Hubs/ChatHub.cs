using Collaboration.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ServiceDefaults.Extensions;

namespace Collaboration.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ITeamRepository _teamRepository;

    public ChatHub(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public static string GetGroupName(string chatId) => $"team-{chatId}";

    /// <summary>Personal group: direct messages and request updates are pushed here.</summary>
    public static string UserGroup(string userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.TryGetUserId()?.ToString();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId), Context.ConnectionAborted);
            Presence?.Connected(userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.TryGetUserId()?.ToString();
        if (!string.IsNullOrWhiteSpace(userId))
            Presence?.Disconnected(userId);
        await base.OnDisconnectedAsync(exception);
    }

    // Resolved per call rather than injected, so the hub's constructor (and its tests) stay as they were.
    private Notifications.PresenceTracker? Presence =>
        Context.GetHttpContext()?.RequestServices.GetService<Notifications.PresenceTracker>();

    public async Task JoinChat(string chatId)
    {
        var userId = Context.User?.GetUserId().ToString();
        if (string.IsNullOrWhiteSpace(userId))
            throw new HubException("User is not authenticated.");

        var isMember = await _teamRepository.IsUserMemberAsync(chatId, userId, Context.ConnectionAborted);
        if (!isMember)
            throw new HubException("You are not a member of this team chat.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(chatId), Context.ConnectionAborted);
    }

    public async Task LeaveChat(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(chatId), Context.ConnectionAborted);
    }
}