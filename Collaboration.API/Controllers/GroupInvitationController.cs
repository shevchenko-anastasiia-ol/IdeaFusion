using Collaboration.Application.Commands.GroupInvitation;
using Collaboration.Application.Queries.GroupInvitation;
using Collaboration.Domain.Entities.Parameters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Collaboration.API.Controllers;

[Route("api/group-invitations")]
public class GroupInvitationController : BaseApiController
{
    public GroupInvitationController(IMediator mediator) : base(mediator) { }
 
    // GET: api/group-invitations/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await _mediator.Send(new GetGroupInvitationByIdQuery { InvitationId = id }, cancellationToken);
            if (invitation is null) return NotFound();
            return Ok(invitation);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/group-invitations
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] GroupInvitationParameters parameters, CancellationToken cancellationToken)
    {
        try
        {
            var invitations = await _mediator.Send(new GetGroupInvitationsPagedQuery { Parameters = parameters }, cancellationToken);
            return Ok(invitations);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/group-invitations/team/{teamId}
    [HttpGet("team/{teamId}")]
    public async Task<IActionResult> GetByTeam(string teamId, CancellationToken cancellationToken)
    {
        try
        {
            var invitations = await _mediator.Send(new GetGroupInvitationsByTeamQuery { TeamId = teamId }, cancellationToken);
            return Ok(invitations);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/group-invitations/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var invitations = await _mediator.Send(new GetGroupInvitationsByUserQuery { UserId = userId }, cancellationToken);
            return Ok(invitations);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/group-invitations/expired
    [HttpGet("expired")]
    public async Task<IActionResult> GetExpired(CancellationToken cancellationToken)
    {
        try
        {
            var invitations = await _mediator.Send(new GetExpiredGroupInvitationsQuery(), cancellationToken);
            return Ok(invitations);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // POST: api/group-invitations
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupInvitationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = invitation.Id }, invitation);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PATCH: api/group-invitations/{id}/accept
    [HttpPatch("{id}/accept")]
    public async Task<IActionResult> Accept(string id, [FromBody] AcceptGroupInvitationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.InvitationId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var invitation = await _mediator.Send(command, cancellationToken);
            return Ok(invitation);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PATCH: api/group-invitations/{id}/decline
    [HttpPatch("{id}/decline")]
    public async Task<IActionResult> Decline(string id, [FromBody] DeclineGroupInvitationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.InvitationId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var invitation = await _mediator.Send(command, cancellationToken);
            return Ok(invitation);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PATCH: api/group-invitations/{id}/revoke
    [HttpPatch("{id}/revoke")]
    public async Task<IActionResult> Revoke(string id, [FromBody] RevokeGroupInvitationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.InvitationId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}