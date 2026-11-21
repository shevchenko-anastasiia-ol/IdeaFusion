using Collaboration.Application.Commands.Team;
using Collaboration.Application.Interfaces.Services;
using Collaboration.Application.Queries.Team;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Entities.Parameters;
using Collaboration.API.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collaboration.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class TeamController : BaseApiController
{
    private readonly ICallerContext _caller;
    private readonly ICollaborationAuditWriter _audit;

    public TeamController(IMediator mediator, ICallerContext caller, ICollaborationAuditWriter audit)
        : base(mediator)
    {
        _caller = caller;
        _audit = audit;
    }
 
    // GET: api/team/{id}
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var team = await _mediator.Send(new GetTeamByIdQuery { TeamId = id }, cancellationToken);
            if (team is null) return NotFound();
 
            AddETagHeader(GenerateETag(team.UpdatedAt ?? team.CreatedAt));
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] TeamParameters parameters, CancellationToken cancellationToken)
    {
        try
        {
            var teams = await _mediator.Send(new GetTeamsPagedQuery { Parameters = parameters }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team/search
    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
    {
        try
        {
            var teams = await _mediator.Send(new SearchTeamsByNameQuery { Name = name }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team/status/{status}
    [AllowAnonymous]
    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(TeamStatus status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var teams = await _mediator.Send(new GetTeamsByStatusQuery
            {
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team/category/{category}
    [AllowAnonymous]
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var teams = await _mediator.Send(new GetTeamsByCategoryQuery
            {
                Category = category,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // GET: api/team/member/{userId}
    // Public, like the team pages that already list every member: which teams this person
    // is in. The aggregator builds a member's portfolio from it, so that work a team
    // published shows up on each member's profile and not only on the team's page.
    // Read-only; nothing here acts on the user's behalf.
    [AllowAnonymous]
    [HttpGet("member/{userId}")]
    public async Task<IActionResult> GetTeamsByMember(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var teams = await _mediator.Send(new GetTeamsByMemberQuery { UserId = userId }, cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // GET: api/team/mine
    // The caller's own memberships. Unlike member/{userId}, this one answers for the token.
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyTeams(CancellationToken cancellationToken)
    {
        try
        {
            var teams = await _mediator.Send(new GetMyTeamsQuery(), cancellationToken);
            return Ok(teams);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // POST: api/team
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var team = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PUT: api/team/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var requestETag = GetIfMatchHeader();
            var existing = await _mediator.Send(new GetTeamByIdQuery { TeamId = id }, cancellationToken);
            if (existing is null) return NotFound();
 
            var currentETag = GenerateETag(existing.UpdatedAt ?? existing.CreatedAt);
            if (!ValidateETag(requestETag, currentETag))
                return StatusCode(412, new { message = "ETag mismatch. The resource has been modified." });
 
            var updated = await _mediator.Send(command, cancellationToken);
            AddETagHeader(GenerateETag(updated.UpdatedAt ?? updated.CreatedAt));
            return Ok(updated);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // PATCH: api/team/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, [FromBody] SetTeamStatusCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var team = await _mediator.Send(command, cancellationToken);
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // DELETE: api/team/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteTeamCommand { TeamId = id }, cancellationToken);
            await _audit.RecordAsync(
                action: "DeleteTeam",
                targetType: "Team",
                targetId: id,
                details: "Team deleted.",
                cancellationToken: cancellationToken);
            return NoContent();
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // POST: api/team/{id}/members
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(string id, [FromBody] AddTeamMemberCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var team = await _mediator.Send(command, cancellationToken);
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // DELETE: api/team/{id}/members/{userId}
    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var team = await _mediator.Send(new RemoveTeamMemberCommand
            {
                TeamId = id,
                MemberUserId = userId
            }, cancellationToken);

            // Recorded after the removal has been applied, and only for a member being
            // removed by somebody else: a voluntary exit is not a moderation event.
            if (!string.Equals(userId, _caller.UserId, StringComparison.Ordinal))
            {
                await _audit.RecordAsync(
                    action: "RemoveTeamMember",
                    targetType: "Team",
                    targetId: id,
                    targetName: team.Name,
                    details: $"Removed member {userId} from team '{team.Name}'.",
                    cancellationToken: cancellationToken);
            }

            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
 
    // POST: api/team/{id}/required-roles
    [HttpPost("{id}/required-roles")]
    public async Task<IActionResult> AddRequiredRole(string id, [FromBody] AddRequiredRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });
 
            var team = await _mediator.Send(command, cancellationToken);
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // PATCH: api/team/{id}/avatar-url
    [HttpPatch("{id}/avatar-url")]
    public async Task<IActionResult> SetAvatarUrl(string id, [FromBody] SetTeamAvatarUrlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id)
                return BadRequest(new { message = "ID mismatch." });

            var team = await _mediator.Send(command, cancellationToken);
            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // PATCH: api/team/{id}/members/{memberId}/permissions
    [HttpPatch("{id}/members/{memberId}/permissions")]
    public async Task<IActionResult> SetMemberPermissions(string id, string memberId, [FromBody] UpdateTeamMemberPermissionsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id || command.TargetMemberId != memberId)
                return BadRequest(new { message = "ID mismatch." });

            var team = await _mediator.Send(command, cancellationToken);

            await _audit.RecordAsync(
                action: "ChangeTeamMemberPermissions",
                targetType: "Team",
                targetId: id,
                targetName: team.Name,
                details: $"Permissions for {memberId} set to: " +
                         (command.Permissions.Count == 0 ? "(none)" : string.Join(", ", command.Permissions)),
                cancellationToken: cancellationToken);

            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    // PATCH: api/team/{id}/members/{memberId}/role
    // Administrator only: this is the only route where a team role changes hands.
    [Authorize(Policy = "RequireAdministrator")]
    [HttpPatch("{id}/members/{memberId}/role")]
    public async Task<IActionResult> SetMemberRole(string id, string memberId, [FromBody] SetTeamMemberRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.TeamId != id || command.TargetMemberId != memberId)
                return BadRequest(new { message = "ID mismatch." });

            var team = await _mediator.Send(command, cancellationToken);

            await _audit.RecordAsync(
                action: "ChangeTeamMemberRole",
                targetType: "Team",
                targetId: id,
                targetName: team.Name,
                details: $"Member {memberId} was set to role '{command.Role}'.",
                cancellationToken: cancellationToken);

            return Ok(team);
        }
        catch (Exception ex) { return HandleException(ex); }
    }
}