using Collaboration.API.Hubs;
using Collaboration.Application.Commands.ChatMessage;
using Collaboration.Application.DTOs;
using Collaboration.Application.Queries.ChatMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Collaboration.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ChatController : BaseApiController
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(IMediator mediator, IHubContext<ChatHub> hubContext) : base(mediator)
    {
        _hubContext = hubContext;
    }

    // GET: api/chat/my
    // The caller's chats. There is no userId parameter: the token says whose these are.
    [HttpGet("my")]
    public async Task<IActionResult> GetMyChats(CancellationToken cancellationToken)
    {
        try
        {
            var chats = await _mediator.Send(new GetMyChatsQuery(), cancellationToken);
            return Ok(chats);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // GET: api/chat/{chatId}/messages?before=&pageSize=
    [HttpGet("{chatId}/messages")]
    public async Task<IActionResult> GetMessages(
        string chatId,
        [FromQuery] string? before,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paged = await _mediator.Send(new GetChatHistoryQuery
            {
                ChatId = chatId,
                BeforeMessageId = before,
                PageSize = pageSize
            }, cancellationToken);

            return Ok(new
            {
                items = paged.Select(ChatMessageDto.From),
                hasMore = paged.NextCursor != null,
                nextCursor = paged.NextCursor
            });
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // POST: api/chat/{chatId}/messages
    [HttpPost("{chatId}/messages")]
    public async Task<IActionResult> SendMessage(string chatId, [FromBody] SendChatMessageCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.ChatId != chatId)
                return BadRequest(new { message = "ID mismatch." });

            var message = await _mediator.Send(command, cancellationToken);

            var dto = ChatMessageDto.From(message);
            await _hubContext.Clients.Group(ChatHub.GetGroupName(chatId))
                .SendAsync("ReceiveMessage", dto, cancellationToken);

            return CreatedAtAction(nameof(GetMessages), new { chatId }, dto);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}