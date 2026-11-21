using Collaboration.API.Hubs;
using Collaboration.Application.Commands.ChatMessage;
using Collaboration.Application.Commands.DirectMessage;
using Collaboration.Application.DTOs;
using Collaboration.Application.Queries.DirectMessage;
using Collaboration.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ServiceDefaults.Extensions;

namespace Collaboration.API.Controllers;

/// <summary>
/// One-to-one chats. Lives under /api/chat/direct so the gateway's existing chat routes
/// cover it. Every response describes the conversation as the caller sees it.
/// </summary>
[Route("api/chat/direct")]
[Authorize]
public class DirectChatController : BaseApiController
{
    private readonly IHubContext<ChatHub> _hub;

    public DirectChatController(IMediator mediator, IHubContext<ChatHub> hub) : base(mediator)
    {
        _hub = hub;
    }

    private string Me => User.GetUserId().ToString();

    public sealed class SendDirectBody
    {
        public string? Text { get; init; }
        public List<ChatAttachmentInput> Attachments { get; init; } = [];
    }

    public sealed class StartDirectBody
    {
        public string RecipientId { get; init; } = default!;
        public string? Text { get; init; }
        public List<ChatAttachmentInput> Attachments { get; init; } = [];
    }

    // GET: api/chat/direct
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        try
        {
            var list = await _mediator.Send(new GetMyDirectConversationsQuery(), cancellationToken);
            var me = Me;
            return Ok(list.Select(c => DirectConversationDto.For(c, me)));
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // GET: api/chat/direct/with/{userId} — 204 when the two have never written to each other.
    [HttpGet("with/{userId}")]
    public async Task<IActionResult> GetWithUser(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var c = await _mediator.Send(new GetDirectConversationWithUserQuery { UserId = userId }, cancellationToken);
            return c is null ? NoContent() : Ok(DirectConversationDto.For(c, Me));
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // GET: api/chat/direct/{id}/messages?before=&pageSize=
    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(string id, [FromQuery] string? before, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var (conversation, page) = await _mediator.Send(new GetDirectHistoryQuery
            {
                ConversationId = id,
                BeforeMessageId = before,
                PageSize = pageSize
            }, cancellationToken);

            return Ok(new
            {
                conversation = DirectConversationDto.For(conversation, Me),
                items = page.Select(ChatMessageDto.From),
                hasMore = page.NextCursor != null,
                nextCursor = page.NextCursor
            });
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // POST: api/chat/direct/start — the first message to somebody (or the next one, if a chat exists).
    [HttpPost("start")]
    public Task<IActionResult> Start([FromBody] StartDirectBody body, CancellationToken cancellationToken) =>
        Send(new SendDirectMessageCommand
        {
            RecipientId = body.RecipientId,
            Text = body.Text,
            Attachments = body.Attachments ?? []
        }, cancellationToken);

    // POST: api/chat/direct/{id}/messages
    [HttpPost("{id}/messages")]
    public Task<IActionResult> SendMessage(string id, [FromBody] SendDirectBody body, CancellationToken cancellationToken) =>
        Send(new SendDirectMessageCommand
        {
            ConversationId = id,
            Text = body.Text,
            Attachments = body.Attachments ?? []
        }, cancellationToken);

    // POST: api/chat/direct/{id}/accept | decline | block | unblock
    [HttpPost("{id}/accept")]
    public Task<IActionResult> Accept(string id, CancellationToken ct) => Change(id, DirectConversationAction.Accept, ct);

    [HttpPost("{id}/decline")]
    public Task<IActionResult> Decline(string id, CancellationToken ct) => Change(id, DirectConversationAction.Decline, ct);

    [HttpPost("{id}/block")]
    public Task<IActionResult> Block(string id, CancellationToken ct) => Change(id, DirectConversationAction.Block, ct);

    [HttpPost("{id}/unblock")]
    public Task<IActionResult> Unblock(string id, CancellationToken ct) => Change(id, DirectConversationAction.Unblock, ct);

    // POST: api/chat/direct/users/{userId}/block — from a profile, with or without an existing chat.
    [HttpPost("users/{userId}/block")]
    public async Task<IActionResult> BlockUser(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var c = await _mediator.Send(new BlockDirectUserCommand { UserId = userId }, cancellationToken);
            await NotifyAsync(c, null, cancellationToken);
            return Ok(DirectConversationDto.For(c, Me));
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    private async Task<IActionResult> Send(SendDirectMessageCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var sent = await _mediator.Send(command, cancellationToken);
            var message = ChatMessageDto.From(sent.Message);
            await NotifyAsync(sent.Conversation, message, cancellationToken);
            return Ok(new { conversation = DirectConversationDto.For(sent.Conversation, Me), message });
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    private async Task<IActionResult> Change(string id, DirectConversationAction action, CancellationToken cancellationToken)
    {
        try
        {
            var c = await _mediator.Send(new ChangeDirectConversationCommand { ConversationId = id, Action = action }, cancellationToken);
            await NotifyAsync(c, null, cancellationToken);
            return Ok(DirectConversationDto.For(c, Me));
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    /// <summary>
    /// Each participant gets the conversation described from their own side. A block placed
    /// before anyone wrote is not announced to the other person.
    /// </summary>
    private async Task NotifyAsync(DirectConversation c, ChatMessageDto? message, CancellationToken cancellationToken)
    {
        foreach (var userId in c.ParticipantIds)
        {
            if (message is null && c.LastMessageAt is null && userId != Me)
                continue;
            await _hub.Clients.Group(ChatHub.UserGroup(userId)).SendAsync(
                "DirectUpdate",
                new { conversation = DirectConversationDto.For(c, userId), message },
                cancellationToken);
        }
    }
}
