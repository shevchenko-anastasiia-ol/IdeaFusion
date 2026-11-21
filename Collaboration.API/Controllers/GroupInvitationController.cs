using Collaboration.API.Services;
using Collaboration.Application.Commands.GroupInvitation;
using Collaboration.Application.Queries.GroupInvitation;
using Collaboration.Domain.Entities.Parameters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collaboration.API.Controllers;

[Route("api/group-invitations")]
[Authorize]
public class GroupInvitationController : BaseApiController
{
    private readonly ICollaborationAuditWriter _audit;

    public GroupInvitationController(IMediator mediator, ICollaborationAuditWriter audit) : base(mediator)
    {
        _audit = audit;
    }
 
    // GET: api/group-invitations/{id}
    // Authenticated: an invitation names the invitee and the inviter's message.
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
    // Authenticated: listing is scoped to one team and permission-checked.
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
 
    // GET: api/group-invitations/mine
    // The invitations addressed to the caller. The old /user/{userId} route is gone:
    // an invitation is personal, so it is never readable on somebody else's behalf.
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        try
        {
            var invitations = await _mediator.Send(new GetMyGroupInvitationsQuery(), cancellationToken);
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

            // Read first: the command returns Unit, so the invitee and the team have to
            // be captured before the revoke mutates them away.
            var existing = await _mediator.Send(
                new GetGroupInvitationByIdQuery { InvitationId = id }, cancellationToken);

            await _mediator.Send(command, cancellationToken);

            await _audit.RecordAsync(
                action: "RevokeGroupInvitation",
                targetType: "GroupInvitation",
                targetId: id,
                targetName: existing?.InvitedUserId,
                details: existing is null
                    ? "Group invitation revoked."
                    : $"Invitation to '{existing.InvitedUserId}' for team {existing.TeamId} revoked.",
                cancellationToken: cancellationToken);

            return NoContent();
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}